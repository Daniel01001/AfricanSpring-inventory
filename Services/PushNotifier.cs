using System.Text.Json;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace AfricanSpringInventory.Services;

// Sends Web Push notifications to every subscribed app device. Configured via
// WebPush:PublicKey / PrivateKey / Subject (set as Render env vars). If keys are
// missing it quietly no-ops so the rest of the app keeps working.
public class PushNotifier
{
    private static readonly WebPushClient Client = new();

    private readonly AppDbContext _db;
    private readonly ILogger<PushNotifier> _log;
    private readonly VapidDetails? _vapid;

    public PushNotifier(AppDbContext db, IConfiguration config, ILogger<PushNotifier> log)
    {
        _db = db;
        _log = log;

        var pub = config["WebPush:PublicKey"];
        var priv = config["WebPush:PrivateKey"];
        var subject = config["WebPush:Subject"];
        if (string.IsNullOrWhiteSpace(subject)) subject = "mailto:orders@africanspring.co.za";

        if (!string.IsNullOrWhiteSpace(pub) && !string.IsNullOrWhiteSpace(priv))
            _vapid = new VapidDetails(subject, pub, priv);
    }

    public bool Configured => _vapid is not null;

    public async Task NotifyAllAsync(string title, string body, string url)
    {
        if (_vapid is null) return;

        var devices = await _db.PushDevices.ToListAsync();
        if (devices.Count == 0) return;

        var payload = JsonSerializer.Serialize(new { title, body, url });
        var stale = new List<PushDevice>();

        foreach (var d in devices)
        {
            try
            {
                var sub = new WebPush.PushSubscription(d.Endpoint, d.P256dh, d.Auth);
                await Client.SendNotificationAsync(sub, payload, _vapid);
            }
            catch (WebPushException ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
                ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                stale.Add(d); // subscription expired or was removed on the device
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Push send failed for device {Id}", d.Id);
            }
        }

        if (stale.Count > 0)
        {
            _db.PushDevices.RemoveRange(stale);
            await _db.SaveChangesAsync();
        }
    }
}
