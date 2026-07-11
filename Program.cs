using System.Security.Claims;
using System.Threading.RateLimiting;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Pin a fixed culture so money/number formatting and form parsing are identical
// everywhere (local and Render), and match HTML number inputs (dot-decimals).
var appCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = appCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = appCulture;

// Render provides the port to bind on via $PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(DbConnection.Resolve(builder.Configuration)));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        // A signed-in Friend hitting an Owner-only page goes to their home,
        // not the login screen.
        options.AccessDeniedPath = "/";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OwnerOnly", p => p.RequireRole(nameof(UserRole.Owner)));
    // Everything requires a signed-in user unless it opts out with [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRazorPages();

// Sends Web Push notifications to subscribed app devices when orders arrive.
builder.Services.AddScoped<PushNotifier>();

// Public API consumed by the marketing site (different origin): the product feed
// (GET) and order submissions (POST). No credentials, so any origin is safe.
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicSite", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .WithMethods("GET", "POST"));
});

// Throttle public order submissions to 5 per minute per client IP to blunt spam.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("orders", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Trust Render's proxy headers so the app knows it's really on HTTPS.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(appCulture),
    SupportedCultures = new[] { appCulture },
    SupportedUICultures = new[] { appCulture },
});

app.UseStaticFiles();
app.UseRouting();

app.UseCors("PublicSite");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Public product feed for the marketing site. Anonymous + CORS, active products only.
app.MapGet("/api/products", async (AppDbContext db, HttpContext http) =>
{
    http.Response.Headers.CacheControl = "public, max-age=60";
    return await db.Products
        .Where(p => p.IsActive)
        .OrderBy(p => p.Name)
        .Select(p => new
        {
            id = p.Id,
            name = p.Name,
            unitType = p.UnitType,
            unitPrice = p.UnitPrice
        })
        .ToListAsync();
})
    .AllowAnonymous()
    .RequireCors("PublicSite");

// Lightweight liveness endpoint for uptime pings (keeps the free instance warm).
app.MapGet("/health", () => Results.Ok("healthy")).AllowAnonymous();

// Public order intake from the marketing site. Anonymous + CORS + rate limited,
// with a honeypot field to catch bots.
app.MapPost("/api/orders", async (OrderDto dto, AppDbContext db, PushNotifier push) =>
{
    // Bots fill hidden fields a human never sees. Pretend success so we don't tip them off.
    if (!string.IsNullOrWhiteSpace(dto.Website))
        return Results.Ok(new { ok = true });

    var name = dto.Name?.Trim() ?? "";
    var phone = dto.Phone?.Trim() ?? "";
    if (name.Length == 0 || phone.Length == 0)
        return Results.BadRequest(new { error = "Name and phone are required." });

    static string Cap(string s, int max) => s.Length > max ? s[..max] : s;

    var product = (dto.Product ?? "").Trim();
    // Match the order to a catalog product by name when we can.
    int? productId = product.Length == 0 ? null
        : await db.Products.Where(p => p.Name == product).Select(p => (int?)p.Id).FirstOrDefaultAsync();

    db.Orders.Add(new Order
    {
        CustomerName = Cap(name, 80),
        Phone = Cap(phone, 40),
        ProductName = Cap(product, 120),
        ProductId = productId,
        Details = string.IsNullOrWhiteSpace(dto.Details) ? null : Cap(dto.Details.Trim(), 1000),
        Source = "website"
    });
    await db.SaveChangesAsync();

    // Ping the app users' devices. Never let a notification failure fail the order.
    try
    {
        await push.NotifyAllAsync(
            "New website order",
            product.Length > 0 ? $"{Cap(name, 80)} · {product}" : Cap(name, 80),
            "/Orders/Index");
    }
    catch { /* best-effort */ }

    return Results.Ok(new { ok = true });
})
    .AllowAnonymous()
    .RequireCors("PublicSite")
    .RequireRateLimiting("orders");

// --- Web Push subscription management (signed-in app users only) ---

// Public VAPID key the browser needs to subscribe.
app.MapGet("/push/publickey", (IConfiguration cfg) =>
    Results.Ok(new { publicKey = cfg["WebPush:PublicKey"] ?? "" }));

app.MapPost("/push/subscribe", async (PushSubDto dto, AppDbContext db, HttpContext ctx) =>
{
    var idClaim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(idClaim, out var userId)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(dto.Endpoint) || string.IsNullOrWhiteSpace(dto.P256dh) || string.IsNullOrWhiteSpace(dto.Auth))
        return Results.BadRequest();

    var existing = await db.PushDevices.FirstOrDefaultAsync(d => d.Endpoint == dto.Endpoint);
    if (existing is null)
    {
        db.PushDevices.Add(new PushDevice
        {
            UserId = userId,
            Endpoint = dto.Endpoint!,
            P256dh = dto.P256dh!,
            Auth = dto.Auth!
        });
    }
    else
    {
        existing.UserId = userId;
        existing.P256dh = dto.P256dh!;
        existing.Auth = dto.Auth!;
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { ok = true });
});

app.MapPost("/push/unsubscribe", async (PushUnsubDto dto, AppDbContext db) =>
{
    if (!string.IsNullOrWhiteSpace(dto.Endpoint))
    {
        var d = await db.PushDevices.FirstOrDefaultAsync(x => x.Endpoint == dto.Endpoint);
        if (d is not null)
        {
            db.PushDevices.Remove(d);
            await db.SaveChangesAsync();
        }
    }
    return Results.Ok(new { ok = true });
});

// Apply migrations and seed on startup so a fresh Render DB is ready to go.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

app.Run();

// Shape of the JSON body posted by the marketing site's order form.
// `Website` is the honeypot — real users leave it blank.
record OrderDto(string? Name, string? Phone, string? Product, string? Details, string? Website);

// Push subscription payloads from the browser.
record PushSubDto(string? Endpoint, string? P256dh, string? Auth);
record PushUnsubDto(string? Endpoint);
