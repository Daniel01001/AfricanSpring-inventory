using AfricanSpringInventory.Models;
using Microsoft.EntityFrameworkCore;

namespace AfricanSpringInventory.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Fridge> Fridges => Set<Fridge>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PushDevice> PushDevices => Set<PushDevice>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Store enums as text for readability in the DB.
        b.Entity<User>().Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        b.Entity<Store>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<Store>().Property(x => x.FridgeArrangement).HasConversion<string>().HasMaxLength(20);
        b.Entity<Fridge>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<Payment>().Property(x => x.Method).HasConversion<string>().HasMaxLength(20);
        b.Entity<StockMovement>().Property(x => x.MovementType).HasConversion<string>().HasMaxLength(20);
        b.Entity<Order>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        // Money / quantity precision.
        b.Entity<Product>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        b.Entity<Product>().Property(x => x.ReorderThreshold).HasPrecision(12, 2);
        b.Entity<DeliveryItem>().Property(x => x.Quantity).HasPrecision(12, 2);
        b.Entity<DeliveryItem>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        b.Entity<DeliveryItem>().Property(x => x.LineTotal).HasPrecision(12, 2);
        b.Entity<Payment>().Property(x => x.Amount).HasPrecision(12, 2);
        b.Entity<StockMovement>().Property(x => x.QuantityChange).HasPrecision(12, 2);

        // Deliveries own their items and (delivery-type) stock movements.
        b.Entity<DeliveryItem>()
            .HasOne(x => x.Delivery).WithMany(d => d.Items)
            .HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<StockMovement>()
            .HasOne(x => x.Delivery).WithMany(d => d.StockMovements)
            .HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.Cascade);

        // Don't let deleting a user/product/store cascade-wipe historical records.
        b.Entity<Delivery>()
            .HasOne(x => x.DeliveredBy).WithMany(u => u.Deliveries)
            .HasForeignKey(x => x.DeliveredByUserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Payment>()
            .HasOne(x => x.RecordedBy).WithMany(u => u.Payments)
            .HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<StockMovement>()
            .HasOne(x => x.RecordedBy).WithMany(u => u.StockMovements)
            .HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<DeliveryItem>()
            .HasOne(x => x.Product).WithMany(p => p.DeliveryItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<StockMovement>()
            .HasOne(x => x.Product).WithMany(p => p.StockMovements)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        // A fridge keeps existing if its store is deleted (it just becomes unassigned).
        b.Entity<Fridge>()
            .HasOne(x => x.Store).WithMany(s => s.Fridges)
            .HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.SetNull);

        // Orders link loosely to a product; keep the order if the product is removed.
        b.Entity<Order>()
            .HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);

        // Push subscriptions belong to a user and go away with them.
        b.Entity<PushDevice>().HasIndex(x => x.Endpoint).IsUnique();
        b.Entity<PushDevice>()
            .HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<User>().HasIndex(x => x.Name);
        b.Entity<Delivery>().HasIndex(x => x.DeliveryDate);
        b.Entity<Payment>().HasIndex(x => x.PaymentDate);
        b.Entity<StockMovement>().HasIndex(x => x.MovementDate);
        b.Entity<Order>().HasIndex(x => x.CreatedAt);
    }
}
