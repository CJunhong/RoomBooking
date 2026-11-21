using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoomBooking.Data.Entities;

namespace RoomBooking.Data;

internal sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms
    {
        get
        {
            return Set<Room>();
        }
    }

    public DbSet<Reservation> Reservations
    {
        get
        {
            return Set<Reservation>();
        }
    }

    private static readonly DateTime SeedCreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void OnModelCreating(ModelBuilder model)
    {
        // room seed data (static UTC timestamps keep EF Core migrations deterministic)
        model.Entity<Room>().HasData(
            new Room { Id = 1, Name = "Small Room", Capacity = 4, CreatedAtUtc = SeedCreatedAtUtc },
            new Room { Id = 2, Name = "Conference Room", Capacity = 10, CreatedAtUtc = SeedCreatedAtUtc },
            new Room { Id = 3, Name = "Auditorium", Capacity = 50, CreatedAtUtc = SeedCreatedAtUtc });

        // utc converter for DateTime properties
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            value => EnsureUtc(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        // room configuration
        var rooms = model.Entity<Room>();
        rooms.ToTable(table => table.HasCheckConstraint(
            "CK_Room_Capacity", "Capacity > 0"));
        rooms.Property(r => r.Name).HasMaxLength(100);
        rooms.Property(r => r.CreatedAtUtc).HasConversion(utcConverter);

        // reservation configuration
        var reservations = model.Entity<Reservation>();
        reservations.HasOne<Room>().WithMany().HasForeignKey(r => r.RoomId);
        reservations.Property(r => r.Title).HasMaxLength(200);
        reservations.Property(r => r.StartUtc).HasConversion(utcConverter);
        reservations.Property(r => r.EndUtc).HasConversion(utcConverter);
        reservations.Property(r => r.CreatedAtUtc).HasConversion(utcConverter);
        reservations.HasIndex(r => new { r.RoomId, r.EndUtc, r.StartUtc });
        reservations.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Reservation_TimeRange", "StartUtc < EndUtc");
            table.HasCheckConstraint("CK_Reservation_Title", "length(trim(Title)) > 0");
        });
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
                ? value
                : throw new InvalidOperationException($"Expected UTC DateTime, got {value.Kind}.");
    }
}
