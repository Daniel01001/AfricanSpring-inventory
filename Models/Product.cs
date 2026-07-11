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

    public bool IsActive { get; set; } = true;

    public ICollection<DeliveryItem> DeliveryItems { get; set; } = new List<DeliveryItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
