using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AfricanSpringInventory.Pages.Products;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public Product Product { get; set; } = new() { UnitType = "bag", IsActive = true };

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        _db.Products.Add(Product);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Products/Index");
    }
}
