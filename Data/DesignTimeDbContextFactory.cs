using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AfricanSpringInventory.Data;

// Used only by `dotnet ef` at design time. Its presence means the tooling
// builds the context directly instead of running Program's startup (which would
// try to migrate/seed a live database). The connection string just needs to name
// the provider — building a migration doesn't open a connection.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=africanspring;Username=postgres;Password=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
