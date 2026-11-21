using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RoomBooking.Models.Responses;
using RoomBooking.Controllers;
using RoomBooking.Services;
using Xunit;

namespace RoomBooking.Tests.Controllers;

public sealed class RoomsControllerTests
{
    private readonly Mock<IRoomService> _mockRoomService;
    private readonly RoomsController _controller;

    public RoomsControllerTests()
    {
        _mockRoomService = new Mock<IRoomService>(MockBehavior.Strict);
        _controller = new RoomsController(_mockRoomService.Object);
    }

    [Fact]
    public async Task List_ReturnsRoomsFromService()
    {
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rooms = new List<RoomResponse> { new(1, "Small Room", 4, createdAt) };
        _mockRoomService.Setup(s => s.ListAsync(CancellationToken.None)).ReturnsAsync(rooms);

        var result = await _controller.List(CancellationToken.None);

        Assert.Equal(rooms, result);
        _mockRoomService.Verify(s => s.ListAsync(CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task ListAvailable_PassesTimeRangeToService()
    {
        var start = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);
        var rooms = new List<RoomResponse>();
        _mockRoomService.Setup(s => s.ListAvailableAsync(start, end, CancellationToken.None)).ReturnsAsync(rooms);

        var result = await _controller.ListAvailable(start, end, CancellationToken.None);

        Assert.Equal(rooms, result);
        _mockRoomService.Verify(s => s.ListAvailableAsync(start, end, CancellationToken.None), Times.Once());
    }
}
