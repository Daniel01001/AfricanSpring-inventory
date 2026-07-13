using System.Security.Claims;
using AfricanSpringInventory.Data;
using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditLog _audit;
    public IndexModel(AppDbContext db, AuditLog audit) { _db = db; _audit = audit; }

    public List<Order> Orders { get; set; } = new();
    public int NewCount { get; set; }

    public async Task OnGetAsync()
    {
        Orders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt).Take(100)
            .ToListAsync();
        NewCount = Orders.Count(o => o.Status == OrderStatus.New);
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, OrderStatus status)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        var from = order.Status;
        if (from == status) return RedirectToPage();

        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reference = $"AS-{order.Id:D4}";

        // Delivered → write a Delivery + stock movements once, so it hits the ledger.
        if (status == OrderStatus.Delivered && order.DeliveryId is null
            && order.StoreId is int sid && order.Items.Any(i => i.ProductId != null))
        {
            var delivery = new Delivery
            {
                StoreId = sid,
                DeliveryDate = today,
                DeliveredByUserId = userId,
                Notes = $"Online order {reference}"
            };
            foreach (var it in order.Items.Where(i => i.ProductId != null))
            {
                delivery.Items.Add(new DeliveryItem
                {
                    ProductId = it.ProductId!.Value,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    LineTotal = it.LineTotal
                });
                delivery.StockMovements.Add(new StockMovement
                {
                    ProductId = it.ProductId!.Value,
                    MovementType = StockMovementType.Delivery,
                    QuantityChange = -it.Quantity,
                    MovementDate = today,
                    RecordedByUserId = userId
                });
            }
            _db.Deliveries.Add(delivery);
            order.Delivery = delivery;
            _audit.Add(User, "order.delivered", $"{reference} delivered — R{delivery.Items.Sum(i => i.LineTotal):0.00}, stock deducted");
        }

        // Paid → record a Payment once (only for a delivered order) to settle it.
        if (status == OrderStatus.Paid && order.PaymentId is null
            && order.DeliveryId is not null && order.StoreId is int psid)
        {
            var amount = order.Items.Sum(i => i.LineTotal);
            var payment = new Payment
            {
                StoreId = psid,
                Amount = amount,
                PaymentDate = today,
                Method = PaymentMethod.Cash,
                RecordedByUserId = userId,
                Notes = $"Online order {reference}"
            };
            _db.Payments.Add(payment);
            order.Payment = payment;
            _audit.Add(User, "order.paid", $"{reference} paid — R{amount:0.00}, balance settled");
        }

        // Cancelled → undo any ledger entries this order created (delete the
        // delivery + its stock movements, and the payment), restoring stock and balance.
        if (status == OrderStatus.Cancelled)
        {
            var reversed = false;
            if (order.DeliveryId is int did)
            {
                var delivery = await _db.Deliveries
                    .Include(d => d.Items)
                    .Include(d => d.StockMovements)
                    .FirstOrDefaultAsync(d => d.Id == did);
                if (delivery is not null)
                {
                    _db.StockMovements.RemoveRange(delivery.StockMovements);
                    _db.DeliveryItems.RemoveRange(delivery.Items);
                    _db.Deliveries.Remove(delivery);
                }
                order.DeliveryId = null;
                reversed = true;
            }
            if (order.PaymentId is int pid)
            {
                var payment = await _db.Payments.FindAsync(pid);
                if (payment is not null) _db.Payments.Remove(payment);
                order.PaymentId = null;
                reversed = true;
            }
            if (reversed)
                _audit.Add(User, "order.cancelled", $"{reference} cancelled — delivery/payment reversed, stock restored");
        }

        order.Status = status;
        _audit.Add(User, "order.status", $"{reference}: {from.Label()} → {status.Label()}");
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
