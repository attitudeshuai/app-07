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
    }
}
