using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages;

[Authorize(Policy = "OwnerOnly")]
public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;
    public DashboardModel(AppDbContext db) => _db = db;

    public int DeliveriesThisWeek { get; set; }
    public decimal UnitsThisWeek { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalDeliveryCount { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int OwingCount { get; set; }
    public decimal StockUnits { get; set; }
    public int ProductCount { get; set; }
    public int NewOrders { get; set; }

    public record NamedAmount(string Name, decimal Amount);
    public List<NamedAmount> Outstanding { get; set; } = new();

    public record LowStockItem(string Name, decimal OnHand, decimal Threshold);
    public List<LowStockItem> LowStock { get; set; } = new();

    public record RecentDelivery(int StoreId, string Store, DateOnly Date, string Items, decimal Total);
    public List<RecentDelivery> Recent { get; set; } = new();

    public List<(StoreStatus Status, int Count)> Pipeline { get; set; } = new();

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // Monday

        DeliveriesThisWeek = await _db.Deliveries.CountAsync(d => d.DeliveryDate >= weekStart);
        UnitsThisWeek = await _db.DeliveryItems
            .Where(i => i.Delivery!.DeliveryDate >= weekStart).SumAsync(i => (decimal?)i.Quantity) ?? 0m;

        TotalRevenue = await _db.DeliveryItems.SumAsync(i => (decimal?)i.LineTotal) ?? 0m;
        TotalDeliveryCount = await _db.Deliveries.CountAsync();

        var names = await _db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name);

        var outstanding = await Finance.OutstandingByStoreAsync(_db);
        TotalOutstanding = outstanding.Values.Where(v => v > 0).Sum();
        Outstanding = outstanding
            .Where(kv => kv.Value > 0 && names.ContainsKey(kv.Key))
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new NamedAmount(names[kv.Key], kv.Value))
            .ToList();
        OwingCount = Outstanding.Count;

        var onHand = await Finance.StockOnHandAsync(_db);
        StockUnits = onHand.Values.Sum();

        var activeProducts = await _db.Products.Where(p => p.IsActive).ToListAsync();
        ProductCount = activeProducts.Count;
        LowStock = activeProducts
            .Where(p => p.ReorderThreshold > 0 && (onHand.TryGetValue(p.Id, out var q) ? q : 0m) <= p.ReorderThreshold)
            .Select(p => new LowStockItem(p.Name, onHand.TryGetValue(p.Id, out var qty) ? qty : 0m, p.ReorderThreshold))
            .OrderBy(x => x.OnHand)
            .ToList();

        NewOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.New);

        var recent = await _db.Deliveries
            .OrderByDescending(d => d.DeliveryDate).ThenByDescending(d => d.Id)
            .Take(5)
            .Select(d => new
            {
                d.StoreId,
                Store = d.Store!.Name,
                d.DeliveryDate,
                Total = d.Items.Sum(i => i.LineTotal),
                Lines = d.Items.Select(i => new { Qty = i.Quantity, i.Product!.Name }).ToList()
            })
            .ToListAsync();

        Recent = recent.Select(d => new RecentDelivery(
            d.StoreId, d.Store, d.DeliveryDate,
            string.Join(", ", d.Lines.Select(l => $"{l.Qty:0.##}× {l.Name}")),
            d.Total)).ToList();

        var counts = await _db.Stores
            .GroupBy(s => s.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        Pipeline = Enum.GetValues<StoreStatus>()
            .Select(s => (s, counts.FirstOrDefault(c => c.Key == s)?.Count ?? 0))
            .ToList();
    }
}
