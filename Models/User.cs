using System.ComponentModel.DataAnnotations;

namespace AfricanSpringInventory.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = "";

    // Salted hash of the user's PIN — never store the raw PIN.
    [Required]
    public string PinHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Friend;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
