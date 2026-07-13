using System.Security.Claims;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Payments;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditLog _audit;
    public CreateModel(AppDbContext db, AuditLog audit) { _db = db; _audit = audit; }

    [BindProperty] public int StoreId { get; set; }
    [BindProperty] public decimal? Amount { get; set; }
    [BindProperty] public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    [BindProperty] public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    [BindProperty] public string? Notes { get; set; }

    public List<SelectListItem> Stores { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync(int? storeId)
    {
        await LoadStoresAsync();
        if (storeId.HasValue) StoreId = storeId.Value;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadStoresAsync();

        if (StoreId == 0) { Error = "Pick a store."; return Page(); }
        if (Amount is not > 0) { Error = "Enter an amount greater than zero."; return Page(); }

        _db.Payments.Add(new Payment
        {
            StoreId = StoreId,
            Amount = Amount.Value,
            PaymentDate = PaymentDate,
            Method = Method,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
            RecordedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        });
        var storeName = Stores.FirstOrDefault(s => s.Value == StoreId.ToString())?.Text ?? $"store #{StoreId}";
        _audit.Add(User, "payment.recorded", $"Payment R{Amount.Value:0.00} from {storeName} ({Method})");
        await _db.SaveChangesAsync();
        return RedirectToPage("/Stores/Details", new { id = StoreId });
    }

    private async Task LoadStoresAsync()
    {
        Stores = await _db.Stores
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()))
            .ToListAsync();
    }
}
