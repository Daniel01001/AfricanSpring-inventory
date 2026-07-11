namespace AfricanSpringInventory.Models;

public class Delivery
{
    public int Id { get; set; }

    public int StoreId { get; set; }
    public Store? Store { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public int DeliveredByUserId { get; set; }
    public User? DeliveredBy { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DeliveryItem> Items { get; set; } = new List<DeliveryItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public decimal Total => Items?.Sum(i => i.LineTotal) ?? 0m;
}
