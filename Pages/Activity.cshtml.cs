using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages;

public class ActivityModel : PageModel
{
    private readonly AppDbContext _db;
    public ActivityModel(AppDbContext db) => _db = db;

    public record Item(DateOnly Date, DateTime CreatedAt, string Kind, string Title, string Sub, string Amount, bool Positive);
    public List<IGrouping<DateOnly, Item>> Days { get; set; } = new();

    public async Task OnGetAsync()
    {
        var deliveries = await _db.Deliveries
            .OrderByDescending(d => d.CreatedAt).Take(30)
            .Select(d => new Item(
                d.DeliveryDate, d.CreatedAt, "delivery",
                d.Store!.Name,
                "Delivery by " + d.DeliveredBy!.Name,
                "R " + d.Items.Sum(i => i.LineTotal).ToString("N2"),
                false))
            .ToListAsync();

        var payments = await _db.Payments
            .OrderByDescending(p => p.CreatedAt).Take(30)
            .Select(p => new Item(
                p.PaymentDate, p.CreatedAt, "payment",
                p.Store!.Name,
                p.Method + " payment · by " + p.RecordedBy!.Name,
                "R " + p.Amount.ToString("N2"),
                true))
            .ToListAsync();

        var movements = await _db.StockMovements
            .Where(m => m.MovementType != StockMovementType.Delivery)
            .OrderByDescending(m => m.CreatedAt).Take(30)
            .Select(m => new Item(
                m.MovementDate, m.CreatedAt, "stock",
                m.Product!.Name,
                (m.MovementType == StockMovementType.Production ? "Made ice" : "Adjustment") + " · by " + m.RecordedBy!.Name,
                (m.QuantityChange > 0 ? "+" : "") + m.QuantityChange.ToString("0.##") + " " + m.Product.UnitType,
                m.QuantityChange > 0))
            .ToListAsync();

        Days = deliveries.Concat(payments).Concat(movements)
            .OrderByDescending(i => i.CreatedAt)
            .Take(40)
            .GroupBy(i => i.Date)
            .OrderByDescending(g => g.Key)
            .ToList();
    }
}
