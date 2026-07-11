using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Fridges;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public int Id { get; set; }
    [BindProperty] public int? StoreId { get; set; }
    [BindProperty] public string? SerialNumber { get; set; }
    [BindProperty] public FridgeStatus Status { get; set; }
    [BindProperty] public DateOnly? DateInstalled { get; set; }
    [BindProperty] public string? Notes { get; set; }

    public List<SelectListItem> Stores { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var f = await _db.Fridges.FindAsync(id);
        if (f is null) return NotFound();

        Id = f.Id;
        StoreId = f.StoreId;
        SerialNumber = f.SerialNumber;
        Status = f.Status;
        DateInstalled = f.DateInstalled;
        Notes = f.Notes;

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var f = await _db.Fridges.FindAsync(Id);
        if (f is null) return NotFound();

        f.StoreId = StoreId == 0 ? null : StoreId;
        f.SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? null : SerialNumber.Trim();
        f.Status = Status;
        f.DateInstalled = DateInstalled;
        f.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes;

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
