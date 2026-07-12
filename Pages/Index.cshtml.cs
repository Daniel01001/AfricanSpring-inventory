using AfricanSpringInventory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public string FirstName { get; set; } = "";

    // The day being viewed (defaults to today; friends can page back to see past days).
    [BindProperty(SupportsGet = true)] public DateOnly? Date { get; set; }
    public DateOnly SelectedDate { get; private set; }
    public DateOnly Today { get; private set; }

    public int DeliveriesCount { get; set; }
    public decimal ValueDelivered { get; set; }

    public record DeliveryRow(int StoreId, string Store, string Items, decimal Total, string By);
    public List<DeliveryRow> Deliveries { get; set; } = new();

    public async Task OnGetAsync()
    {
        FirstName = (User.Identity?.Name ?? "").Split(' ').FirstOrDefault() ?? "";

        Today = DateOnly.FromDateTime(DateTime.UtcNow);
        SelectedDate = Date ?? Today;
        if (SelectedDate > Today) SelectedDate = Today;

        DeliveriesCount = await _db.Deliveries.CountAsync(d => d.DeliveryDate == SelectedDate);
        ValueDelivered = await _db.DeliveryItems
            .Where(i => i.Delivery!.DeliveryDate == SelectedDate)
            .SumAsync(i => (decimal?)i.LineTotal) ?? 0m;

        var rows = await _db.Deliveries
            .Where(d => d.DeliveryDate == SelectedDate)
            .OrderByDescending(d => d.Id)
            .Select(d => new
            {
                d.StoreId,
                Store = d.Store!.Name,
                Total = d.Items.Sum(i => i.LineTotal),
                By = d.DeliveredBy!.Name,
                Lines = d.Items.Select(i => new { i.Quantity, Name = i.Product!.Name }).ToList()
            })
            .ToListAsync();

        Deliveries = rows.Select(r => new DeliveryRow(
            r.StoreId, r.Store,
            string.Join(", ", r.Lines.Select(l => $"{l.Quantity:0.##}× {l.Name}")),
            r.Total, r.By)).ToList();
    }
}
