using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Data;
using RoomBooking.Models.Enums;
using RoomBooking.Data.Repositories;
using Xunit;

namespace RoomBooking.Tests.Repositories;

public sealed class ReservationRepositoryTests : RepositoryTestBase
{
    [Fact]
    public async Task TryCreateAsync_CreatesValidAndAdjacentReservations()
    {
        var repository = new ReservationRepository(Db);

        Assert.Equal(ReservationWriteResult.Created,
            await repository.TryCreateAsync(ReservationFor(1, 10, 11), CancellationToken.None));
        Assert.Equal(ReservationWriteResult.Created,
            await repository.TryCreateAsync(ReservationFor(1, 9, 10), CancellationToken.None));
        Assert.Equal(ReservationWriteResult.Created,
            await repository.TryCreateAsync(ReservationFor(1, 11, 12), CancellationToken.None));
        Assert.Equal(ReservationWriteResult.Created,
            await repository.TryCreateAsync(ReservationFor(2, 10, 11), CancellationToken.None));
    }

    [Fact]
    public async Task TryCreateAsync_RejectsOverlappingReservationInSameRoom()
    {
        var repository = new ReservationRepository(Db);
        await repository.TryCreateAsync(ReservationFor(1, 10, 12), CancellationToken.None);

        var result = await repository.TryCreateAsync(ReservationFor(1, 11, 13), CancellationToken.None);

        Assert.Equal(ReservationWriteResult.Conflict, result);
    }

    [Fact]
    public async Task TryCreateAsync_ReturnsRoomNotFoundForUnknownRoom()
    {
        var repository = new ReservationRepository(Db);

        var result = await repository.TryCreateAsync(ReservationFor(99, 10, 11), CancellationToken.None);

        Assert.Equal(ReservationWriteResult.RoomNotFound, result);
    }

    [Fact]
    public async Task ListAsync_ReturnsOrderedReservationsAndSupportsOverlapFilters()
    {
        var repository = new ReservationRepository(Db);
        await repository.TryCreateAsync(ReservationFor(1, 14, 15, "Afternoon"), CancellationToken.None);
        await repository.TryCreateAsync(ReservationFor(1, 9, 10, "Morning"), CancellationToken.None);
        await repository.TryCreateAsync(ReservationFor(1, 11, 12, "Midday"), CancellationToken.None);
        await repository.TryCreateAsync(ReservationFor(2, 11, 12, "Other room"), CancellationToken.None);

        var all = await repository.ListAsync(1, null, null, CancellationToken.None);
        var fromStart = await repository.ListAsync(1, UtcOffsetHour(10), null, CancellationToken.None);
        var untilEnd = await repository.ListAsync(1, null, UtcOffsetHour(11), CancellationToken.None);
        var window = await repository.ListAsync(1, UtcOffsetHour(10), UtcOffsetHour(14), CancellationToken.None);

        Assert.Equal(new[] { 9, 11, 14 }, all.Select(r => r.StartUtc.Hour).ToArray());
        Assert.All(all, r => Assert.Equal(DateTimeKind.Utc, r.StartUtc.Kind));
        Assert.Equal(new[] { 11, 14 }, fromStart.Select(r => r.StartUtc.Hour).ToArray());
        Assert.Equal(new[] { 9 }, untilEnd.Select(r => r.StartUtc.Hour).ToArray());
        Assert.Equal(new[] { 11 }, window.Select(r => r.StartUtc.Hour).ToArray());
    }

    [Fact]
    public async Task FindAsync_ReturnsReservationOnlyForMatchingRoom()
    {
        var repository = new ReservationRepository(Db);
        var created = ReservationFor(1, 10, 11);
        await repository.TryCreateAsync(created, CancellationToken.None);

        var found = await repository.FindAsync(1, created.Id, CancellationToken.None);
        var wrongRoom = await repository.FindAsync(2, created.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
        Assert.Null(wrongRoom);
    }
    
    [Fact]
    public async Task TryCreateAsync_SerializesConcurrentCompetingWritesOnFileBackedSqlite()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"room-booking-concurrency-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Foreign Keys=True;Default Timeout=5";
        var options = new DbContextOptionsBuilder<BookingDbContext>().UseSqlite(connectionString).Options;

        try
        {
            await using (var setupDb = new BookingDbContext(options))
            {
                await setupDb.Database.EnsureCreatedAsync();
                await setupDb.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");
            }

            var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task<ReservationWriteResult> AttemptBookingAsync(string title)
            {
                await barrier.Task;
                await using var context = new BookingDbContext(options);
                var repository = new ReservationRepository(context);
                return await repository.TryCreateAsync(ReservationFor(1, 10, 11, title), CancellationToken.None);
            }

            var first = AttemptBookingAsync("Writer A");
            var second = AttemptBookingAsync("Writer B");
            barrier.SetResult();

            var results = await Task.WhenAll(first, second);

            Assert.Equal(1, results.Count(r => r == ReservationWriteResult.Created));
            Assert.Equal(1, results.Count(r => r == ReservationWriteResult.Conflict));

            await using var verifyDb = new BookingDbContext(options);
            Assert.Equal(1, await verifyDb.Reservations.CountAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var file = dbPath + suffix;
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
    }
}
