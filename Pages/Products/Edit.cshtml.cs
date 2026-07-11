using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AfricanSpringInventory.Pages.Products;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public Product Product { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        Product = p;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var p = await _db.Products.FindAsync(Product.Id);
        if (p is null) return NotFound();

        p.Name = Product.Name;
        p.UnitType = Product.UnitType;
        p.UnitPrice = Product.UnitPrice;
        p.IsActive = Product.IsActive;

        await _db.SaveChangesAsync();
        return RedirectToPage("/Products/Index");
    }
}
