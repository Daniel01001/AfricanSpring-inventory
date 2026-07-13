using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AfricanSpringInventory.Pages.Products;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public Product Product { get; set; } = new() { UnitType = "bag", IsActive = true };
    [BindProperty] public IFormFile? Image { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (Image is { Length: > 0 })
        {
            var jpeg = await ImageResizer.ToJpegAsync(Image.OpenReadStream());
            if (jpeg is not null) { Product.ImageData = jpeg; Product.ImageContentType = "image/jpeg"; }
        }

        _db.Products.Add(Product);
        await _db.SaveChangesAsync();
        return RedirectToPage("/Products/Index");
    }
}
