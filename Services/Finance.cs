using AfricanSpringInventory.Data;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Services;

public static class Finance
{
    // Outstanding per store = total delivered - total paid (running balance).
    public static async Task<Dictionary<int, decimal>> OutstandingByStoreAsync(AppDbContext db)
    {
        var delivered = await db.DeliveryItems
            .GroupBy(i => i.Delivery!.StoreId)
            .Select(g => new { StoreId = g.Key, Total = g.Sum(x => x.LineTotal) })
            .ToListAsync();

        var paid = await db.Payments
            .GroupBy(p => p.StoreId)
            .Select(g => new { StoreId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();

        var map = delivered.ToDictionary(x => x.StoreId, x => x.Total);
        foreach (var p in paid)
            map[p.StoreId] = (map.TryGetValue(p.StoreId, out var d) ? d : 0m) - p.Total;
        return map;
    }

    public static async Task<decimal> OutstandingForStoreAsync(AppDbContext db, int storeId)
    {
        var delivered = await db.DeliveryItems
            .Where(i => i.Delivery!.StoreId == storeId)
            .SumAsync(i => (decimal?)i.LineTotal) ?? 0m;
        var paid = await db.Payments
            .Where(p => p.StoreId == storeId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        return delivered - paid;
    }

    // On-hand per product = sum of all signed stock movements.
    public static async Task<Dictionary<int, decimal>> StockOnHandAsync(AppDbContext db)
    {
        return await db.StockMovements
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.QuantityChange) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);
    }
}
