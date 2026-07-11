using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
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

// Read-only product feed consumed by the public marketing site (different origin).
// GET-only, no credentials, so allowing any origin is safe.
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicSite", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .WithMethods("GET"));
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

// Apply migrations and seed on startup so a fresh Render DB is ready to go.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

app.Run();
