using System.Security.Cryptography;
using System.Text;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Stores;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    public DetailsModel(AppDbContext db) => _db = db;

    public Store Store { get; set; } = default!;
    public CustomerAccount? Account { get; set; }
    public decimal Outstanding { get; set; }
    public decimal TotalDelivered { get; set; }
    public decimal TotalPaid { get; set; }

    // Shown once after granting/resetting so the owner can send it on WhatsApp.
    [TempData] public string? GrantedPassword { get; set; }
    [TempData] public string? GrantedPhone { get; set; }
    [TempData] public string? AccessError { get; set; }

    public record DeliveryRow(int Id, DateOnly Date, decimal Total, string By);
    public List<DeliveryRow> Deliveries { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var store = await _db.Stores.Include(s => s.Fridges).Include(s => s.CustomerAccount)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (store is null) return NotFound();
        Store = store;
        Account = store.CustomerAccount;

        TotalDelivered = await _db.DeliveryItems
            .Where(i => i.Delivery!.StoreId == id).SumAsync(i => (decimal?)i.LineTotal) ?? 0m;
        TotalPaid = await _db.Payments
            .Where(p => p.StoreId == id).SumAsync(p => (decimal?)p.Amount) ?? 0m;
        Outstanding = TotalDelivered - TotalPaid;

        Deliveries = await _db.Deliveries
            .Where(d => d.StoreId == id)
            .OrderByDescending(d => d.DeliveryDate).ThenByDescending(d => d.Id)
            .Take(6)
            .Select(d => new DeliveryRow(d.Id, d.DeliveryDate, d.Items.Sum(i => i.LineTotal), d.DeliveredBy!.Name))
            .ToListAsync();

        Payments = await _db.Payments
            .Where(p => p.StoreId == id)
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Take(6).ToListAsync();

        return Page();
    }

    // Give this store's contact online-ordering access. Creates an account (with a
    // temp password) if the phone is new, and links every store sharing that phone.
    public async Task<IActionResult> OnPostGrantAccessAsync(int id)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id);
        if (store is null) return NotFound();

        var phone = PhoneNormalizer.Normalize(store.Phone);
        if (phone.Length < 6)
        {
            AccessError = "Add a phone number to this store first.";
            return RedirectToPage(new { id });
        }

        var account = await _db.CustomerAccounts.FirstOrDefaultAsync(a => a.Phone == phone);
        if (account is null)
        {
            var temp = TempPassword();
            account = new CustomerAccount
            {
                Phone = phone,
                Name = string.IsNullOrWhiteSpace(store.ContactPerson) ? store.Name : store.ContactPerson!,
                PasswordHash = PinHasher.Hash(temp),
                MustChangePassword = true,
                Source = "owner"
            };
            _db.CustomerAccounts.Add(account);
            GrantedPassword = temp;
            GrantedPhone = phone;
        }

        // Link this store and any siblings with the same phone that aren't linked.
        var unlinked = await _db.Stores.Where(s => s.Phone != null && s.CustomerAccountId == null).ToListAsync();
        foreach (var s in unlinked)
            if (PhoneNormalizer.Normalize(s.Phone) == phone)
                s.CustomerAccount = account;

        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(int id)
    {
        var store = await _db.Stores.Include(s => s.CustomerAccount).FirstOrDefaultAsync(s => s.Id == id);
        if (store?.CustomerAccount is null) return RedirectToPage(new { id });

        var temp = TempPassword();
        store.CustomerAccount.PasswordHash = PinHasher.Hash(temp);
        store.CustomerAccount.MustChangePassword = true;
        await _db.SaveChangesAsync();

        GrantedPassword = temp;
        GrantedPhone = store.CustomerAccount.Phone;
        return RedirectToPage(new { id });
    }

    private static string TempPassword()
    {
        const string chars = "abcdefghijkmnpqrstuvwxyz23456789"; // no ambiguous 0/O/1/l
        var bytes = RandomNumberGenerator.GetBytes(8);
        var sb = new StringBuilder(8);
        foreach (var b in bytes) sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }
}
