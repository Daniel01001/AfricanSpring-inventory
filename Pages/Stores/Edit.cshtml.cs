using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AfricanSpringInventory.Pages.Stores;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public Store Store { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var s = await _db.Stores.FindAsync(id);
        if (s is null) return NotFound();
        Store = s;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var s = await _db.Stores.FindAsync(Store.Id);
        if (s is null) return NotFound();

        s.Name = Store.Name;
        s.ContactPerson = Store.ContactPerson;
        s.Phone = Store.Phone;
        s.Location = Store.Location;
        s.Status = Store.Status;
        s.FridgeArrangement = Store.FridgeArrangement;
        s.Notes = Store.Notes;
        s.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToPage("/Stores/Details", new { id = s.Id });
    }
}
