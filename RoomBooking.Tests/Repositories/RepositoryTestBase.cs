using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Data;
using RoomBooking.Data.Entities;
using Xunit;

namespace RoomBooking.Tests.Repositories;

public abstract class RepositoryTestBase : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
    private protected BookingDbContext Db { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BookingDbContext>().UseSqlite(_connection).Options;
        Db = new BookingDbContext(options);
        await Db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private protected static Reservation ReservationFor(int roomId, int startHour, int endHour, string title = "Design review")
    {
        return new()
        {
            RoomId = roomId,
            StartUtc = UtcHour(startHour),
            EndUtc = UtcHour(endHour),
            Title = title
        };
    }

    private protected static DateTime UtcHour(int hour, int minute = 0)
    {
        return new(2026, 1, 1, hour, minute, 0, DateTimeKind.Utc);
    }

    private protected static DateTimeOffset UtcOffsetHour(int hour, int minute = 0)
    {
        return new(UtcHour(hour, minute), TimeSpan.Zero);
    }
}
