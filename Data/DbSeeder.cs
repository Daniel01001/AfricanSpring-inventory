using AfricanSpringInventory.Models;
using AfricanSpringInventory.Services;

namespace AfricanSpringInventory.Data;

public static class DbSeeder
{
    // Creates the first login(s) and sample products on an empty database.
    // Default PINs can be overridden with env vars, and should be changed after first login.
    public static void Seed(AppDbContext db)
    {
        if (!db.Users.Any())
        {
            var ownerPin = Environment.GetEnvironmentVariable("SEED_OWNER_PIN") ?? "1234";
            var friendPin = Environment.GetEnvironmentVariable("SEED_FRIEND_PIN") ?? "0000";

            db.Users.AddRange(
                new User { Name = "Daniel", Role = UserRole.Owner, PinHash = PinHasher.Hash(ownerPin) },
                new User { Name = "Mongezi", Role = UserRole.Friend, PinHash = PinHasher.Hash(friendPin) },
                new User { Name = "Minenhle", Role = UserRole.Friend, PinHash = PinHasher.Hash(friendPin) }
            );
        }

        if (!db.Products.Any())
        {
            db.Products.Add(new Product { Name = "2kg Ice Bag", UnitType = "bag", UnitPrice = 16m });
        }

        db.SaveChanges();
    }
}
