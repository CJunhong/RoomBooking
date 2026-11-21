using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RoomBooking.Models.Enums;
using RoomBooking.Data.Entities;
using RoomBooking.Errors;
using RoomBooking.Data.Repositories;
using RoomBooking.Services;
using Xunit;

namespace RoomBooking.Tests.Services;

public sealed class RoomServiceTests
{
    private readonly Mock<IRoomRepository> _mockRoomRepository;
    private readonly RoomService _service;

    public RoomServiceTests()
    {
        _mockRoomRepository = new Mock<IRoomRepository>(MockBehavior.Strict);
        _service = new RoomService(_mockRoomRepository.Object);
    }

    [Fact]
    public async Task ListAsync_MapsEntitiesToRoomResponses()
    {
        var createdAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rooms = new List<Room>
        {
            new() { Id = 1, Name = "Small Room", Capacity = 4, CreatedAtUtc = createdAtUtc }
        };
        _mockRoomRepository.Setup(r => r.ListAsync(CancellationToken.None)).ReturnsAsync(rooms);

        var result = await _service.ListAsync(CancellationToken.None);

        var response = Assert.Single(result);
        Assert.Equal(1, response.Id);
        Assert.Equal("Small Room", response.Name);
        Assert.Equal(4, response.Capacity);
        Assert.Equal(new DateTimeOffset(createdAtUtc, TimeSpan.Zero), response.CreatedAt);
        _mockRoomRepository.Verify(r => r.ListAsync(CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task ListAvailableAsync_MapsAvailableRoomsForValidRange()
    {
        var start = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);
        var createdAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rooms = new List<Room>
        {
            new() { Id = 2, Name = "Conference Room", Capacity = 10, CreatedAtUtc = createdAtUtc }
        };
        _mockRoomRepository.Setup(r => r.ListAvailableAsync(start, end, CancellationToken.None)).ReturnsAsync(rooms);

        var result = await _service.ListAvailableAsync(start, end, CancellationToken.None);

        var response = Assert.Single(result);
        Assert.Equal(2, response.Id);
        Assert.Equal("Conference Room", response.Name);
        _mockRoomRepository.Verify(r => r.ListAvailableAsync(start, end, CancellationToken.None), Times.Once());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public async Task ListAvailableAsync_RejectsInvalidRangeWithoutCallingRepository(int durationMinutes)
    {
        var start = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes(durationMinutes);

        var error = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            await _service.ListAvailableAsync(start, end, CancellationToken.None);
        });

        Assert.Equal(BookingError.InvalidRange, error.Error);
    }
}
