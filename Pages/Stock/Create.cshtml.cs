using System.Security.Claims;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Stock;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public int ProductId { get; set; }
    [BindProperty] public StockMovementType MovementType { get; set; } = StockMovementType.Production;
    [BindProperty] public decimal? Quantity { get; set; }
    [BindProperty] public DateOnly MovementDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    [BindProperty] public string? Notes { get; set; }

    public List<SelectListItem> Products { get; set; } = new();
    public record OnHandRow(string Name, decimal Qty, string Unit);
    public List<OnHandRow> OnHand { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();

        if (ProductId == 0) { Error = "Pick a product."; return Page(); }
        if (Quantity is null or 0) { Error = "Enter a quantity."; return Page(); }

        // Production always adds; an adjustment keeps the sign entered (negative reduces).
        var change = MovementType == StockMovementType.Production
            ? Math.Abs(Quantity.Value)
            : Quantity.Value;

        _db.StockMovements.Add(new StockMovement
        {
            ProductId = ProductId,
            MovementType = MovementType,
            QuantityChange = change,
            MovementDate = MovementDate,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
            RecordedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("/Index");
    }

    private async Task LoadAsync()
    {
        var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        Products = products.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();

        var onHand = await Finance.StockOnHandAsync(_db);
        OnHand = products
            .Select(p => new OnHandRow(p.Name, onHand.TryGetValue(p.Id, out var q) ? q : 0m, p.UnitType))
            .ToList();
    }
}
