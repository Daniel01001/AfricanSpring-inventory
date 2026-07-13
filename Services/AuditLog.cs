using System.Security.Claims;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;

namespace AfricanSpringInventory.Services;

// Records who did what. Entries are added to the context and commit with the
// surrounding SaveChanges, so an action and its audit row land together.
public class AuditLog
{
    private readonly AppDbContext _db;
    public AuditLog(AppDbContext db) => _db = db;

    public void Add(ClaimsPrincipal user, string action, string summary)
    {
        _db.AuditEntries.Add(new AuditEntry
        {
            UserId = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null,
            UserName = user.Identity?.Name ?? "",
            Action = action,
            Summary = summary
        });
    }
}
