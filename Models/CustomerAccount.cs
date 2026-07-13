using System.ComponentModel.DataAnnotations;

namespace AfricanSpringInventory.Models;

// A customer login for the public ordering portal. Identified by phone; can own
// several stores (a person with multiple taverns). Passwords are hashed.
public class CustomerAccount
{
    public int Id { get; set; }

    // Canonical +27... form (see PhoneNormalizer). Unique.
    [Required, MaxLength(20)]
    public string Phone { get; set; } = "";

    [MaxLength(120)]
    public string Name { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    // Owner-issued accounts start with a temp password the customer must replace.
    public bool MustChangePassword { get; set; }

    public bool IsActive { get; set; } = true;

    // "website" (self-registered) or "owner" (granted from the app).
    [MaxLength(20)]
    public string Source { get; set; } = "website";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Store> Stores { get; set; } = new List<Store>();
}
