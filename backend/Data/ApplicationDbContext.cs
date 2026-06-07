using Microsoft.EntityFrameworkCore;
using PointsMall.Models;

namespace PointsMall.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderHistory> OrderHistories { get; set; }
    public DbSet<MemberUser> MemberUsers { get; set; }
    public DbSet<PointsRecord> PointsRecords { get; set; }
    public DbSet<MemberLevel> MemberLevels { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<FlashSale> FlashSales { get; set; }
    public DbSet<FlashSaleUserPurchase> FlashSaleUserPurchases { get; set; }
    public DbSet<CheckInRecord> CheckInRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderNo)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasMany(o => o.OrderHistories)
            .WithOne()
            .HasForeignKey(oh => oh.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemberUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<MemberUser>()
            .HasIndex(u => u.Phone)
            .IsUnique();

        modelBuilder.Entity<MemberUser>()
            .HasMany(u => u.PointsRecords)
            .WithOne(r => r.MemberUser)
            .HasForeignKey(r => r.MemberUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemberUser>()
            .HasMany(u => u.Orders)
            .WithOne(o => o.MemberUser)
            .HasForeignKey(o => o.MemberUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PointsRecord>()
            .HasIndex(r => r.MemberUserId);

        modelBuilder.Entity<PointsRecord>()
            .HasIndex(r => r.Type);

        modelBuilder.Entity<MemberLevel>()
            .HasIndex(l => l.Name)
            .IsUnique();

        modelBuilder.Entity<MemberLevel>()
            .HasIndex(l => l.MinPoints);

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name);

        modelBuilder.Entity<Category>()
            .HasMany(c => c.Children)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        modelBuilder.Entity<FlashSale>()
            .HasIndex(f => f.ProductId);

        modelBuilder.Entity<FlashSale>()
            .HasIndex(f => f.StartTime);

        modelBuilder.Entity<FlashSale>()
            .HasIndex(f => f.EndTime);

        modelBuilder.Entity<FlashSale>()
            .HasIndex(f => f.IsActive);

        modelBuilder.Entity<FlashSale>()
            .HasOne(f => f.Product)
            .WithMany()
            .HasForeignKey(f => f.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderType);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.FlashSaleId);

        modelBuilder.Entity<FlashSaleUserPurchase>()
            .HasIndex(p => new { p.FlashSaleId, p.MemberUserId })
            .IsUnique();

        modelBuilder.Entity<FlashSaleUserPurchase>()
            .HasIndex(p => p.FlashSaleId);

        modelBuilder.Entity<FlashSaleUserPurchase>()
            .HasIndex(p => p.MemberUserId);

        modelBuilder.Entity<CheckInRecord>()
            .HasIndex(r => r.MemberUserId);

        modelBuilder.Entity<CheckInRecord>()
            .HasIndex(r => r.CheckInDate);

        modelBuilder.Entity<CheckInRecord>()
            .HasIndex(r => new { r.MemberUserId, r.CheckInDate })
            .IsUnique();

        modelBuilder.Entity<MemberUser>()
            .HasMany(u => u.CheckInRecords)
            .WithOne(r => r.MemberUser)
            .HasForeignKey(r => r.MemberUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
