using Npgsql;

namespace AfricanSpringInventory.Data;

public static class DbConnection
{
    // Render/Heroku hand the DB over as a single DATABASE_URL. Locally we use
    // the ConnectionStrings:DefaultConnection value from appsettings.
    public static string Resolve(IConfiguration config)
    {
        var url = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(url))
            return FromUrl(url);

        return config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No database connection: set DATABASE_URL or ConnectionStrings:DefaultConnection.");
    }

    private static string FromUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require,
        };
        return builder.ConnectionString;
    }
}
