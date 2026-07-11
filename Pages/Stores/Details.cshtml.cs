using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Stores;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    public DetailsModel(AppDbContext db) => _db = db;

    public Store Store { get; set; } = default!;
    public decimal Outstanding { get; set; }
    public decimal TotalDelivered { get; set; }
    public decimal TotalPaid { get; set; }

    public record DeliveryRow(int Id, DateOnly Date, decimal Total, string By);
    public List<DeliveryRow> Deliveries { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var store = await _db.Stores.Include(s => s.Fridges).FirstOrDefaultAsync(s => s.Id == id);
        if (store is null) return NotFound();
        Store = store;

        TotalDelivered = await _db.DeliveryItems
            .Where(i => i.Delivery!.StoreId == id).SumAsync(i => (decimal?)i.LineTotal) ?? 0m;
        TotalPaid = await _db.Payments
            .Where(p => p.StoreId == id).SumAsync(p => (decimal?)p.Amount) ?? 0m;
        Outstanding = TotalDelivered - TotalPaid;

        Deliveries = await _db.Deliveries
            .Where(d => d.StoreId == id)
            .OrderByDescending(d => d.DeliveryDate).ThenByDescending(d => d.Id)
            .Take(6)
            .Select(d => new DeliveryRow(d.Id, d.DeliveryDate, d.Items.Sum(i => i.LineTotal), d.DeliveredBy!.Name))
            .ToListAsync();

        Payments = await _db.Payments
            .Where(p => p.StoreId == id)
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Take(6).ToListAsync();

        return Page();
    }
}
