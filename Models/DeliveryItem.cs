namespace AfricanSpringInventory.Models;

public class DeliveryItem
{
    public int Id { get; set; }

    public int DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    // Price snapshot at the time of delivery — independent of later price changes.
    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
