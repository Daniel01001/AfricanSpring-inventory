using System.ComponentModel.DataAnnotations;

namespace AfricanSpringInventory.Models;

// An order request submitted from the public marketing site. WhatsApp is still
// the main conversation channel; this just makes sure nothing slips through.
public class Order
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string CustomerName { get; set; } = "";

    [Required, MaxLength(40)]
    public string Phone { get; set; } = "";

    // Snapshot of what they asked for (products can be renamed or removed later).
    [MaxLength(120)]
    public string ProductName { get; set; } = "";

    // Optional link to the catalog product, if one matched.
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    // Set when the order came from a signed-in portal customer (else guest).
    public int? CustomerAccountId { get; set; }
    public CustomerAccount? CustomerAccount { get; set; }

    public int? StoreId { get; set; }
    public Store? Store { get; set; }

    [MaxLength(1000)]
    public string? Details { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;

    // Ledger entries this order created when it was Delivered / Paid (idempotency).
    public int? DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    [MaxLength(30)]
    public string Source { get; set; } = "website";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
