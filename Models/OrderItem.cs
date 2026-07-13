using System.ComponentModel.DataAnnotations;

namespace AfricanSpringInventory.Models;

// A line on a website/portal order. Structured (unlike the order's text summary)
// so a Delivered order can be turned into a real Delivery + stock movement.
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    // Name snapshot (products can be renamed/removed later).
    [MaxLength(120)]
    public string ProductName { get; set; } = "";

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
