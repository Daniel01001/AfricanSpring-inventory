using System.Security.Claims;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    public LoginModel(AppDbContext db) => _db = db;

    [BindProperty] public int UserId { get; set; }
    [BindProperty] public string Pin { get; set; } = "";

    public List<SelectListItem> People { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync() => await LoadPeopleAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadPeopleAsync();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId && u.IsActive);
        if (user is null || string.IsNullOrEmpty(Pin) || !PinHasher.Verify(Pin, user.PinHash))
        {
            Error = "That PIN doesn't match. Try again.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        // IsPersistent gives the cookie a real expiry (ExpireTimeSpan) so friends
        // stay signed in on their phones instead of losing a session cookie.
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        // Owner lands on the dashboard; friends land on the action home.
        return user.Role == UserRole.Owner ? RedirectToPage("/Dashboard") : RedirectToPage("/Index");
    }

    private async Task LoadPeopleAsync()
    {
        People = await _db.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new SelectListItem(u.Name, u.Id.ToString()))
            .ToListAsync();
    }
}
