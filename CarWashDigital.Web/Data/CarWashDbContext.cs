using CarWashDigital.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWashDigital.Web.Data;

public class CarWashDbContext : DbContext
{
    public CarWashDbContext(DbContextOptions<CarWashDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceItem> Services => Set<ServiceItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingHistory> BookingHistories => Set<BookingHistory>();
    public DbSet<BusinessConfig> BusinessConfigs => Set<BusinessConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Name).HasMaxLength(120).IsRequired();
            e.Property(u => u.Email).HasMaxLength(160).IsRequired();
            e.Property(u => u.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.Property(v => v.Brand).HasMaxLength(80).IsRequired();
            e.Property(v => v.Model).HasMaxLength(80).IsRequired();
            e.Property(v => v.Category).HasMaxLength(40).IsRequired();
            e.Property(v => v.Plate).HasMaxLength(20).IsRequired();
            e.HasOne(v => v.User).WithMany(u => u.Vehicles).HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceItem>(e =>
        {
            e.HasIndex(s => s.Code).IsUnique();
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();
            e.Property(s => s.Includes).HasColumnType("jsonb");
            e.Property(s => s.PricesByCategory).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.HasIndex(b => b.BookingCode).IsUnique();
            e.HasIndex(b => new { b.Date, b.TimeSlot });
            e.Property(b => b.Price).HasColumnType("numeric(10,2)");
            e.Property(b => b.ServiceIncludes).HasColumnType("jsonb");
            e.HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(b => b.History).WithOne(h => h.Booking).HasForeignKey(h => h.BookingId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BusinessConfig>(e =>
        {
            e.Property(c => c.OpenDays).HasColumnType("jsonb");
            e.Property(c => c.ClosedDates).HasColumnType("jsonb");
        });
    }
}
