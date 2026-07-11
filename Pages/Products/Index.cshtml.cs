using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Products;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<Product> Products { get; set; } = new();
    public Dictionary<int, decimal> OnHand { get; set; } = new();

    public async Task OnGetAsync()
    {
        Products = await _db.Products
            .OrderByDescending(p => p.IsActive).ThenBy(p => p.Name)
            .ToListAsync();
        OnHand = await Finance.StockOnHandAsync(_db);
    }

    public bool IsLow(Product p) =>
        p.ReorderThreshold > 0 && (OnHand.TryGetValue(p.Id, out var q) ? q : 0m) <= p.ReorderThreshold;
}
