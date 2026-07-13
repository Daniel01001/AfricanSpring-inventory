using AfricanSpringInventory.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Customers;

[Authorize(Policy = "OwnerOnly")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public record Row(int Id, string Name, string Phone, int Stores, bool IsActive, bool MustChange, string Source, DateTime? LastLogin);
    public List<Row> Accounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Accounts = await _db.CustomerAccounts
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new Row(a.Id, a.Name, a.Phone, a.Stores.Count, a.IsActive, a.MustChangePassword, a.Source, a.LastLoginAt))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var a = await _db.CustomerAccounts.FindAsync(id);
        if (a is not null)
        {
            a.IsActive = !a.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
