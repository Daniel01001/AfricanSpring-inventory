namespace AfricanSpringInventory.Models;

// A browser/device Web Push subscription belonging to an app user. One user can
// have several (phone, laptop, etc.). Endpoint is the push service URL.
public class PushDevice
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Endpoint { get; set; } = "";
    public string P256dh { get; set; } = "";
    public string Auth { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
