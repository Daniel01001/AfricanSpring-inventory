using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Stores;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public record Row(Store Store, decimal Revenue, decimal Outstanding);
    public List<Row> Rows { get; set; } = new();

    public async Task OnGetAsync()
    {
        var stores = await _db.Stores.OrderBy(s => s.Name).ToListAsync();
        var outstanding = await Finance.OutstandingByStoreAsync(_db);

        var revenue = (await _db.DeliveryItems
            .GroupBy(i => i.Delivery!.StoreId)
            .Select(g => new { StoreId = g.Key, Total = g.Sum(x => x.LineTotal) })
            .ToListAsync())
            .ToDictionary(x => x.StoreId, x => x.Total);

        Rows = stores
            .Select(s => new Row(s,
                revenue.TryGetValue(s.Id, out var r) ? r : 0m,
                outstanding.TryGetValue(s.Id, out var o) ? o : 0m))
            .ToList();
    }
}
