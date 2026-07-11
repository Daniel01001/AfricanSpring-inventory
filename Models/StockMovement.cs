namespace AfricanSpringInventory.Models;

public class StockMovement
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public StockMovementType MovementType { get; set; }

    // Signed: + for Production, - for Delivery, +/- for Adjustment.
    public decimal QuantityChange { get; set; }

    public DateOnly MovementDate { get; set; }

    // Set when MovementType == Delivery, linking the deduction to its delivery.
    public int? DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }

    public string? Notes { get; set; }

    public int RecordedByUserId { get; set; }
    public User? RecordedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
