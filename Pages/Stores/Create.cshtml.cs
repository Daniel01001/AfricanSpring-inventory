using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AfricanSpringInventory.Pages.Stores;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public Store Store { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        Store.CreatedAt = DateTime.UtcNow;
        Store.UpdatedAt = DateTime.UtcNow;
        _db.Stores.Add(Store);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Stores/Details", new { id = Store.Id });
    }
}
