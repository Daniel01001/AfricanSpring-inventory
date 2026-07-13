using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AfricanSpringInventory.Pages.Products;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public Product Product { get; set; } = new();
    [BindProperty] public IFormFile? Image { get; set; }
    public bool HasImage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        Product = p;
        HasImage = p.ImageData != null;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { HasImage = Product.ImageData != null; return Page(); }

        var p = await _db.Products.FindAsync(Product.Id);
        if (p is null) return NotFound();

        p.Name = Product.Name;
        p.UnitType = Product.UnitType;
        p.UnitPrice = Product.UnitPrice;
        p.ReorderThreshold = Product.ReorderThreshold;
        p.IsActive = Product.IsActive;

        if (Image is { Length: > 0 })
        {
            var jpeg = await ImageResizer.ToJpegAsync(Image.OpenReadStream());
            if (jpeg is not null) { p.ImageData = jpeg; p.ImageContentType = "image/jpeg"; }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/Products/Index");
    }
}
