using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;

namespace ShopPOS.Data;

public class PosDbContext : DbContext
{
    public PosDbContext(DbContextOptions<PosDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleAmendment> SaleAmendments => Set<SaleAmendment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ShopSetting> ShopSettings => Set<ShopSetting>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<ExpenseAndCash> ExpenseAndCashEntries => Set<ExpenseAndCash>();
    public DbSet<WorkerDailyCash> WorkerDailyCash => Set<WorkerDailyCash>();
    public DbSet<WorkerAdvance> WorkerAdvances => Set<WorkerAdvance>();
    public DbSet<WorkerLeave> WorkerLeaves => Set<WorkerLeave>();
    public DbSet<OwnerPersonalExpense> OwnerPersonalExpenses => Set<OwnerPersonalExpense>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorKhataEntry> VendorKhataEntries => Set<VendorKhataEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.DisplayName).HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Category).HasMaxLength(100);
            e.Property(p => p.Barcode).HasMaxLength(50);
            e.Property(p => p.Sku).HasMaxLength(50);
            e.Property(p => p.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasIndex(c => c.Phone).IsUnique();
            e.Property(c => c.Phone).HasMaxLength(20);
            e.Property(c => c.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.HasIndex(s => s.ReceiptNo).IsUnique();
            e.Property(s => s.ReceiptNo).HasMaxLength(20);
            e.Property(s => s.SoldByUsername).HasMaxLength(50);
            e.Property(s => s.Subtotal).HasPrecision(18, 2);
            e.Property(s => s.DiscountPercent).HasPrecision(5, 2);
            e.Property(s => s.DiscountAmount).HasPrecision(18, 2);
            e.Property(s => s.TaxAmount).HasPrecision(18, 2);
            e.Property(s => s.Total).HasPrecision(18, 2);
            e.Property(s => s.ReturnedAmount).HasPrecision(18, 2);
            e.Property(s => s.AddedAmount).HasPrecision(18, 2);
            e.Property(s => s.NetTotal).HasPrecision(18, 2);
            e.Property(s => s.AmountReceived).HasPrecision(18, 2);
            e.Property(s => s.ChangeAmount).HasPrecision(18, 2);
            e.Property(s => s.CustomerPhone).HasMaxLength(20);
            e.Property(s => s.CustomerEmail).HasMaxLength(200);
            e.HasMany(s => s.Items).WithOne(i => i.Sale).HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.Returns).WithOne(r => r.Sale).HasForeignKey(r => r.SaleId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(s => s.Amendments).WithOne(a => a.Sale).HasForeignKey(a => a.SaleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleItem>(e =>
        {
            e.Property(i => i.UnitPriceAtSale).HasPrecision(18, 2);
            e.Property(i => i.LineTotal).HasPrecision(18, 2);
            e.Property(i => i.ProductName).HasMaxLength(200);
            e.HasMany(i => i.Returns).WithOne(r => r.SaleItem).HasForeignKey(r => r.SaleItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleReturn>(e =>
        {
            e.Property(r => r.UnitPriceAtOriginalSale).HasPrecision(18, 2);
            e.Property(r => r.RefundAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SaleAmendment>(e =>
        {
            e.Property(a => a.UnitPrice).HasPrecision(18, 2);
            e.Property(a => a.AmountDelta).HasPrecision(18, 2);
            e.Property(a => a.ProductName).HasMaxLength(200);
            e.Property(a => a.ProcessedByUsername).HasMaxLength(50);
            e.Property(a => a.Reason).HasMaxLength(500);
            e.HasOne(a => a.SaleItem).WithMany().HasForeignKey(a => a.SaleItemId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.Username).HasMaxLength(50);
            e.Property(a => a.EntityType).HasMaxLength(50);
            e.Property(a => a.EntityId).HasMaxLength(50);
            e.HasIndex(a => a.Timestamp);
        });

        modelBuilder.Entity<ShopSetting>(e =>
        {
            e.HasIndex(s => s.Key).IsUnique();
            e.Property(s => s.Key).HasMaxLength(100);
            e.Property(s => s.Value).HasMaxLength(4000);
        });

        modelBuilder.Entity<Worker>(e =>
        {
            e.Property(w => w.Name).HasMaxLength(100);
            e.Property(w => w.Phone).HasMaxLength(20);
            e.Property(w => w.Role).HasMaxLength(50);
            e.Property(w => w.MonthlySalary).HasPrecision(18, 2);
            e.Property(w => w.StandardShiftHours).HasPrecision(5, 2);
            e.Property(w => w.HourlyOvertimeRate).HasPrecision(18, 2);
            e.Property(w => w.FingerprintTemplate).HasMaxLength(4000);
            e.HasMany(w => w.AttendanceRecords).WithOne(a => a.Worker).HasForeignKey(a => a.WorkerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.DailyCashRecords).WithOne(x => x.Worker).HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.AdvanceRecords).WithOne(x => x.Worker).HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.LeaveRecords).WithOne(x => x.Worker).HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attendance>(e =>
        {
            e.HasIndex(a => new { a.WorkerId, a.Date });
            e.Property(a => a.Date).HasColumnType("date");
            e.Property(a => a.OvertimeHours).HasPrecision(8, 2);
        });

        modelBuilder.Entity<WorkerDailyCash>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(300);
        });

        modelBuilder.Entity<WorkerAdvance>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(300);
        });

        modelBuilder.Entity<WorkerLeave>(e =>
        {
            e.Property(x => x.Reason).HasMaxLength(300);
        });

        modelBuilder.Entity<OwnerPersonalExpense>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<Vendor>(e =>
        {
            e.Property(v => v.Name).HasMaxLength(150);
            e.Property(v => v.Phone).HasMaxLength(20);
            e.Property(v => v.Address).HasMaxLength(300);
            e.HasMany(v => v.KhataEntries).WithOne(k => k.Vendor).HasForeignKey(k => k.VendorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VendorKhataEntry>(e =>
        {
            e.Property(x => x.InvoiceNumber).HasMaxLength(50);
            e.Property(x => x.TotalBill).HasPrecision(18, 2);
            e.Property(x => x.CashPaid).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.AttachmentPath).HasMaxLength(500);
            e.HasIndex(x => new { x.VendorId, x.IsDeleted });
        });

        modelBuilder.Entity<ExpenseAndCash>(e =>
        {
            e.HasIndex(x => x.Date);
            e.Property(x => x.DailyCashIntake).HasPrecision(18, 2);
            e.Property(x => x.TotalExpense).HasPrecision(18, 2);
            e.Property(x => x.ExpenseDescription).HasMaxLength(500);
        });
    }
}
