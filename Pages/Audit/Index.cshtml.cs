using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Audit;

[Authorize(Policy = "OwnerOnly")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<AuditEntry> Entries { get; set; } = new();

    public async Task OnGetAsync()
    {
        Entries = await _db.AuditEntries
            .OrderByDescending(a => a.CreatedAt)
            .Take(200)
            .ToListAsync();
    }
}
