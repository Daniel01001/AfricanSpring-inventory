using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Users;

[Authorize(Policy = "OwnerOnly")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<User> People { get; set; } = new();

    public async Task OnGetAsync()
    {
        People = await _db.Users
            .OrderByDescending(u => u.IsActive)
            .ThenBy(u => u.Role)
            .ThenBy(u => u.Name)
            .ToListAsync();
    }
}
