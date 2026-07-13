using System.ComponentModel.DataAnnotations;

namespace AfricanSpringInventory.Models;

// A record of a meaningful action taken in the app, so it's clear who did what.
public class AuditEntry
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    [MaxLength(80)]
    public string UserName { get; set; } = "";

    // Short machine-ish label, e.g. "order.delivered", "payment.recorded".
    [MaxLength(60)]
    public string Action { get; set; } = "";

    // Human-readable one-liner shown in the audit view.
    [MaxLength(300)]
    public string Summary { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
