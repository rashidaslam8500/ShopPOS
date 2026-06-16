using Microsoft.EntityFrameworkCore;

namespace ShopPOS.Api.Data;

public class CloudDbContext : DbContext
{
    public CloudDbContext(DbContextOptions<CloudDbContext> options) : base(options) { }

    public DbSet<CloudSale> Sales => Set<CloudSale>();
    public DbSet<CloudProduct> Products => Set<CloudProduct>();
    public DbSet<CloudCustomer> Customers => Set<CloudCustomer>();
    public DbSet<CloudSyncLog> SyncLogs => Set<CloudSyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CloudSale>()
            .HasIndex(s => new { s.ShopId, s.LocalSaleId })
            .IsUnique();

        modelBuilder.Entity<CloudProduct>()
            .HasIndex(p => new { p.ShopId, p.LocalProductId })
            .IsUnique();

        modelBuilder.Entity<CloudCustomer>()
            .HasIndex(c => new { c.ShopId, c.LocalCustomerId })
            .IsUnique();
    }
}
