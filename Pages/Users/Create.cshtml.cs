using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Users;

[Authorize(Policy = "OwnerOnly")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string Pin { get; set; } = "";
    [BindProperty] public UserRole Role { get; set; } = UserRole.Friend;

    public string? Error { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        Name = Name.Trim();
        if (Name.Length == 0) { Error = "Enter a name."; return Page(); }
        if (Pin.Length < 4 || Pin.Length > 8 || !Pin.All(char.IsDigit))
        { Error = "PIN must be 4–8 digits."; return Page(); }
        if (await _db.Users.AnyAsync(u => u.Name == Name))
        { Error = "Someone with that name already exists."; return Page(); }

        _db.Users.Add(new User
        {
            Name = Name,
            Role = Role,
            PinHash = PinHasher.Hash(Pin),
            IsActive = true,
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("/Users/Index");
    }
}
