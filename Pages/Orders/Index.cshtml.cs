using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<Order> Orders { get; set; } = new();
    public int NewCount { get; set; }

    public async Task OnGetAsync()
    {
        Orders = await _db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(100)
            .ToListAsync();
        NewCount = Orders.Count(o => o.Status == OrderStatus.New);
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        order.Status = status;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
