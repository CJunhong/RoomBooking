using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoomBooking.Data.Repositories;
using Xunit;

namespace RoomBooking.Tests.Repositories;

public sealed class RoomRepositoryTests : RepositoryTestBase
{
    [Fact]
    public async Task ListAsync_ReturnsSeededRoomsOrderedById()
    {
        var repository = new RoomRepository(Db);

        var rooms = await repository.ListAsync(CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3 }, rooms.Select(r => r.Id).ToArray());
        Assert.Equal(new[] { 4, 10, 50 }, rooms.Select(r => r.Capacity).ToArray());
        Assert.All(rooms, r => Assert.Equal(DateTimeKind.Utc, r.CreatedAtUtc.Kind));
    }

    [Fact]
    public async Task RoomExistsAsync_ReturnsTrueForExistingAndFalseForMissing()
    {
        var repository = new RoomRepository(Db);

        Assert.True(await repository.RoomExistsAsync(1, CancellationToken.None));
        Assert.False(await repository.RoomExistsAsync(99, CancellationToken.None));
    }

    [Theory]
    [InlineData(10, 0, 11, 0)]  // exact match
    [InlineData(9, 30, 10, 30)] // overlaps start
    [InlineData(10, 30, 11, 30)] // overlaps end
    [InlineData(9, 0, 12, 0)]   // encloses existing
    [InlineData(10, 15, 10, 45)] // enclosed by existing
    public async Task ListAvailableAsync_ExcludesRoomWithAnyOverlappingReservation(
        int startHour, int startMinute, int endHour, int endMinute)
    {
        Db.Reservations.Add(ReservationFor(1, 10, 11));
        await Db.SaveChangesAsync();
        var repository = new RoomRepository(Db);

        var available = await repository.ListAvailableAsync(
            UtcOffsetHour(startHour, startMinute),
            UtcOffsetHour(endHour, endMinute),
            CancellationToken.None);

        Assert.Equal(new[] { 2, 3 }, available.Select(room => room.Id).ToArray());
    }

    [Theory]
    [InlineData(9, 0, 10, 0)]  // adjacent before
    [InlineData(11, 0, 12, 0)] // adjacent after
    [InlineData(13, 0, 14, 0)] // disjoint
    public async Task ListAvailableAsync_IncludesRoomWhenReservationsAreAdjacentOrDisjoint(
        int startHour, int startMinute, int endHour, int endMinute)
    {
        Db.Reservations.Add(ReservationFor(1, 10, 11));
        await Db.SaveChangesAsync();
        var repository = new RoomRepository(Db);

        var available = await repository.ListAvailableAsync(
            UtcOffsetHour(startHour, startMinute),
            UtcOffsetHour(endHour, endMinute),
            CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3 }, available.Select(room => room.Id).ToArray());
    }
}
