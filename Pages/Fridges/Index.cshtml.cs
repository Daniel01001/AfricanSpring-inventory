using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Fridges;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<Fridge> Fridges { get; set; } = new();

    public async Task OnGetAsync()
    {
        Fridges = await _db.Fridges
            .Include(f => f.Store)
            .OrderBy(f => f.Status)
            .ThenBy(f => f.Id)
            .ToListAsync();
    }
}
