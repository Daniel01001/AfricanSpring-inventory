namespace AfricanSpringInventory.Services;

// Normalise SA phone numbers to a single canonical form (+27XXXXXXXXX) so that
// "083 123 4567", "0831234567" and "+27831234567" all match as one identity.
public static class PhoneNormalizer
{
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return "";

        if (digits.StartsWith("0")) digits = "27" + digits[1..];
        else if (!digits.StartsWith("27")) digits = "27" + digits;

        return "+" + digits;
    }
}
