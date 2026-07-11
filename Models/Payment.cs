namespace AfricanSpringInventory.Models;

public class Payment
{
    public int Id { get; set; }

    public int StoreId { get; set; }
    public Store? Store { get; set; }

    public decimal Amount { get; set; }

    public DateOnly PaymentDate { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public int RecordedByUserId { get; set; }
    public User? RecordedBy { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
