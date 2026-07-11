using System.ComponentModel.DataAnnotations;

namespace AfricanSpringInventory.Models;

public class Fridge
{
    public int Id { get; set; }

    // Nullable: a fridge can be ours and sitting in storage before it's placed.
    public int? StoreId { get; set; }
    public Store? Store { get; set; }

    [MaxLength(80)]
    public string? SerialNumber { get; set; }

    public DateOnly? DateInstalled { get; set; }

    public FridgeStatus Status { get; set; } = FridgeStatus.InStorage;

    public string? Notes { get; set; }
}
