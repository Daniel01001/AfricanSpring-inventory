using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Approvals;

[Authorize(Policy = "OwnerOnly")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditLog _audit;
    public IndexModel(AppDbContext db, AuditLog audit) { _db = db; _audit = audit; }

    public record Row(int Id, string Name, string? Location, string Phone, string Customer, DateTime CreatedAt);
    public List<Row> Pending { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Stores someone signed up for online that haven't been vetted yet.
        Pending = await _db.Stores
            .Where(s => s.Status == StoreStatus.Prospect && s.CustomerAccount != null && s.CustomerAccount.Source == "website")
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new Row(s.Id, s.Name, s.Location, s.CustomerAccount!.Phone, s.CustomerAccount!.Name, s.CreatedAt))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id) => await SetStatus(id, StoreStatus.Supplying);
    public async Task<IActionResult> OnPostDeclineAsync(int id) => await SetStatus(id, StoreStatus.Declined);

    private async Task<IActionResult> SetStatus(int id, StoreStatus status)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store is not null)
        {
            store.Status = status;
            store.UpdatedAt = DateTime.UtcNow;
            _audit.Add(User, "store.approval", $"{store.Name} {(status == StoreStatus.Supplying ? "approved" : "declined")}");
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
