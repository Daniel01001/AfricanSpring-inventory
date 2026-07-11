using System.Security.Claims;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Users;

[Authorize(Policy = "OwnerOnly")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public UserRole Role { get; set; }
    [BindProperty] public bool IsActive { get; set; }
    [BindProperty] public string? NewPin { get; set; }

    public bool IsSelf { get; set; }
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u is null) return NotFound();

        Id = u.Id;
        Name = u.Name;
        Role = u.Role;
        IsActive = u.IsActive;
        IsSelf = CurrentUserId() == u.Id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var u = await _db.Users.FindAsync(Id);
        if (u is null) return NotFound();
        IsSelf = CurrentUserId() == u.Id;

        Name = Name.Trim();
        if (Name.Length == 0) { Error = "Enter a name."; return Page(); }
        if (await _db.Users.AnyAsync(x => x.Name == Name && x.Id != Id))
        { Error = "Someone with that name already exists."; return Page(); }

        // Don't let the owner lock themselves out.
        if (IsSelf && (!IsActive || Role != UserRole.Owner))
        { Error = "You can't disable or demote your own account."; return Page(); }

        if (!string.IsNullOrEmpty(NewPin))
        {
            if (NewPin.Length < 4 || NewPin.Length > 8 || !NewPin.All(char.IsDigit))
            { Error = "New PIN must be 4–8 digits."; return Page(); }
            u.PinHash = PinHasher.Hash(NewPin);
        }

        u.Name = Name;
        u.Role = Role;
        u.IsActive = IsActive;

        await _db.SaveChangesAsync();
        return RedirectToPage("/Users/Index");
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
