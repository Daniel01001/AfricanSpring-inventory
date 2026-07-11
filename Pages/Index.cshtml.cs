using System.Security.Claims;
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
    public int DeliveriesToday { get; set; }
    public decimal ValueToday { get; set; }

    public async Task OnGetAsync()
    {
        FirstName = (User.Identity?.Name ?? "").Split(' ').FirstOrDefault() ?? "";

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todays = _db.Deliveries.Where(d => d.DeliveryDate == today);
        DeliveriesToday = await todays.CountAsync();
        ValueToday = await _db.DeliveryItems
            .Where(i => i.Delivery!.DeliveryDate == today)
            .SumAsync(i => (decimal?)i.LineTotal) ?? 0m;
    }
}
