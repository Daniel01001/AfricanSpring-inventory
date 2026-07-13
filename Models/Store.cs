using System.ComponentModel.DataAnnotations;

namespace AfricanSpringInventory.Models;

public class Store
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(120)]
    public string? ContactPerson { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [MaxLength(160)]
    public string? Location { get; set; }

    public StoreStatus Status { get; set; } = StoreStatus.Prospect;

    public FridgeArrangement FridgeArrangement { get; set; } = FridgeArrangement.None;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Set when a customer portal account owns/manages this store (nullable).
    public int? CustomerAccountId { get; set; }
    public CustomerAccount? CustomerAccount { get; set; }

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Fridge> Fridges { get; set; } = new List<Fridge>();
}
