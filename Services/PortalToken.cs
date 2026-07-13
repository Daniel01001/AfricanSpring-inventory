using System.Security.Cryptography;
using System.Text;

namespace AfricanSpringInventory.Services;

// Signed bearer tokens for the customer portal (separate origin, so tokens beat
// cross-site cookies). HMAC-SHA256 over "accountId.expiry"; secret from config
// key Portal:TokenSecret (env Portal__TokenSecret). No token if unconfigured.
public class PortalToken
{
    private readonly byte[]? _key;

    public PortalToken(IConfiguration config)
    {
        var secret = config["Portal:TokenSecret"];
        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 16)
            _key = Encoding.UTF8.GetBytes(secret);
    }

    public bool Configured => _key is not null;

    public string? Create(int accountId, TimeSpan life)
    {
        if (_key is null) return null;
        var exp = DateTimeOffset.UtcNow.Add(life).ToUnixTimeSeconds();
        var payload = $"{accountId}.{exp}";
        return $"{B64(Encoding.UTF8.GetBytes(payload))}.{Sign(payload)}";
    }

    public int? Validate(string? token)
    {
        if (_key is null || string.IsNullOrWhiteSpace(token)) return null;
        var dot = token.LastIndexOf('.');
        if (dot <= 0) return null;

        string payload;
        try { payload = Encoding.UTF8.GetString(FromB64(token[..dot])); }
        catch { return null; }

        var expected = Sign(payload);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(token[(dot + 1)..]), Encoding.UTF8.GetBytes(expected)))
            return null;

        var parts = payload.Split('.');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var id) || !long.TryParse(parts[1], out var exp))
            return null;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return null;
        return id;
    }

    private string Sign(string payload)
    {
        using var h = new HMACSHA256(_key!);
        return B64(h.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static string B64(byte[] b) =>
        Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromB64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(s);
    }
}
