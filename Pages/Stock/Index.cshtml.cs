using System.Security.Claims;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Stock;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    private const decimal LowThreshold = 30m;

    public record Row(int Id, string Name, string Unit, decimal Price, decimal OnHand, DateOnly? LastUpdated, bool Low);
    public List<Row> Rows { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync(int productId, decimal qty, string dir)
    {
        if (productId != 0 && qty > 0)
        {
            var add = dir == "add";
            _db.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                MovementType = add ? StockMovementType.Production : StockMovementType.Adjustment,
                QuantityChange = add ? qty : -qty,
                MovementDate = DateOnly.FromDateTime(DateTime.Now),
                RecordedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            });
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        var onHand = await Finance.StockOnHandAsync(_db);
        var last = (await _db.StockMovements
            .GroupBy(m => m.ProductId)
            .Select(g => new { g.Key, Last = g.Max(x => x.MovementDate) })
            .ToListAsync())
            .ToDictionary(x => x.Key, x => x.Last);

        Rows = products.Select(p =>
        {
            var qty = onHand.TryGetValue(p.Id, out var q) ? q : 0m;
            return new Row(p.Id, p.Name, p.UnitType, p.UnitPrice, qty,
                last.TryGetValue(p.Id, out var d) ? d : null,
                qty < LowThreshold);
        }).ToList();
    }
}
