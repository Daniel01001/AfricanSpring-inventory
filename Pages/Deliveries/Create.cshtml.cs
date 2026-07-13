using System.Security.Claims;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Deliveries;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditLog _audit;
    public CreateModel(AppDbContext db, AuditLog audit) { _db = db; _audit = audit; }

    public class LineInput
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string UnitType { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public decimal? Quantity { get; set; }
    }

    [BindProperty] public int StoreId { get; set; }
    [BindProperty] public DateOnly DeliveryDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public List<LineInput> Lines { get; set; } = new();

    public List<SelectListItem> Stores { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync(int? storeId)
    {
        await LoadStoresAsync();
        if (storeId.HasValue) StoreId = storeId.Value;

        Lines = (await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync())
            .Select(p => new LineInput { ProductId = p.Id, Name = p.Name, UnitType = p.UnitType, UnitPrice = p.UnitPrice })
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadStoresAsync();

        // Rehydrate product name/price from the DB — never trust posted prices.
        var products = await _db.Products.Where(p => p.IsActive).ToDictionaryAsync(p => p.Id);
        foreach (var line in Lines)
        {
            if (products.TryGetValue(line.ProductId, out var pr))
            {
                line.Name = pr.Name;
                line.UnitType = pr.UnitType;
                line.UnitPrice = pr.UnitPrice;
            }
        }

        var chosen = Lines
            .Where(l => l.Quantity is > 0 && products.ContainsKey(l.ProductId))
            .ToList();

        if (StoreId == 0) { Error = "Pick a store."; return Page(); }
        if (chosen.Count == 0) { Error = "Enter a quantity for at least one product."; return Page(); }

        var userId = CurrentUserId();
        var delivery = new Delivery
        {
            StoreId = StoreId,
            DeliveryDate = DeliveryDate,
            DeliveredByUserId = userId,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
        };

        foreach (var l in chosen)
        {
            var pr = products[l.ProductId];
            var qty = l.Quantity!.Value;

            delivery.Items.Add(new DeliveryItem
            {
                ProductId = pr.Id,
                Quantity = qty,
                UnitPrice = pr.UnitPrice,
                LineTotal = qty * pr.UnitPrice,
            });

            // Auto-deduct stock: a negative Delivery movement linked to this delivery.
            delivery.StockMovements.Add(new StockMovement
            {
                ProductId = pr.Id,
                MovementType = StockMovementType.Delivery,
                QuantityChange = -qty,
                MovementDate = DeliveryDate,
                RecordedByUserId = userId,
            });
        }

        _db.Deliveries.Add(delivery);
        var storeName = Stores.FirstOrDefault(s => s.Value == StoreId.ToString())?.Text ?? $"store #{StoreId}";
        _audit.Add(User, "delivery.created", $"Delivery to {storeName} — R{delivery.Items.Sum(i => i.LineTotal):0.00}");
        await _db.SaveChangesAsync();
        return RedirectToPage("/Stores/Details", new { id = StoreId });
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task LoadStoresAsync()
    {
        Stores = await _db.Stores
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()))
            .ToListAsync();
    }
}
