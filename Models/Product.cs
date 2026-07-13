using System.ComponentModel.DataAnnotations;

namespace AfricanSpringInventory.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = "";

    // e.g. "block", "bag", "kg"
    [Required, MaxLength(30)]
    public string UnitType { get; set; } = "";

    public decimal UnitPrice { get; set; }

    // When on-hand stock falls to or below this, the product is flagged low.
    // 0 means no low-stock alert for this product.
    public decimal ReorderThreshold { get; set; }

    public bool IsActive { get; set; } = true;

    // Optional product photo, resized + stored in the DB (persists on Render).
    public byte[]? ImageData { get; set; }
    [MaxLength(40)]
    public string? ImageContentType { get; set; }

    public ICollection<DeliveryItem> DeliveryItems { get; set; } = new List<DeliveryItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
