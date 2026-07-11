using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Fridges;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public int? StoreId { get; set; }
    [BindProperty] public string? SerialNumber { get; set; }
    [BindProperty] public FridgeStatus Status { get; set; } = FridgeStatus.InStorage;
    [BindProperty] public DateOnly? DateInstalled { get; set; }
    [BindProperty] public string? Notes { get; set; }

    public List<SelectListItem> Stores { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();

        _db.Fridges.Add(new Fridge
        {
            StoreId = StoreId == 0 ? null : StoreId,
            SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? null : SerialNumber.Trim(),
            Status = Status,
            DateInstalled = DateInstalled,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("/Fridges/Index");
    }

    private async Task LoadAsync()
    {
        Stores = await _db.Stores
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()))
            .ToListAsync();
    }
}
