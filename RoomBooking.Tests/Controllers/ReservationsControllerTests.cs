using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RoomBooking.Models.Requests;
using RoomBooking.Models.Responses;
using RoomBooking.Controllers;
using RoomBooking.Services;
using Xunit;

namespace RoomBooking.Tests.Controllers;

public sealed class ReservationsControllerTests
{
    private readonly Mock<IReservationService> _mockReservationService;
    private readonly ReservationsController _controller;
    private static readonly DateTimeOffset FixedStart = new(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);

    public ReservationsControllerTests()
    {
        _mockReservationService = new Mock<IReservationService>(MockBehavior.Strict);
        _controller = new ReservationsController(_mockReservationService.Object);
    }

    [Fact]
    public async Task List_PassesRoomIdAndFiltersToService()
    {
        var request = new GetReservationsRequest { Start = FixedStart, End = FixedStart.AddHours(2) };
        var reservations = new List<ReservationResponse>();
        _mockReservationService.Setup(s => s.ListAsync(7, request, CancellationToken.None)).ReturnsAsync(reservations);

        var result = await _controller.List(7, request, CancellationToken.None);

        Assert.Equal(reservations, result);
        _mockReservationService.Verify(s => s.ListAsync(7, request, CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task Get_PassesRoomAndReservationIdsToService()
    {
        var reservation = new ReservationResponse(42, 7, FixedStart, FixedStart.AddHours(1), "Review", FixedStart);
        _mockReservationService.Setup(s => s.FindAsync(7, 42, CancellationToken.None)).ReturnsAsync(reservation);

        var result = await _controller.Get(7, 42, CancellationToken.None);

        Assert.Equal(reservation, result);
        _mockReservationService.Verify(s => s.FindAsync(7, 42, CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task Create_Returns201CreatedWithLocationRouteValues()
    {
        var expected = new ReservationResponse(42, 7, FixedStart, FixedStart.AddHours(1), "Design review", FixedStart);
        var request = new CreateReservationRequest
        {
            Start = expected.Start,
            End = expected.End,
            Title = expected.Title
        };
        _mockReservationService.Setup(s => s.CreateAsync(7, request, CancellationToken.None)).ReturnsAsync(expected);

        var result = await _controller.Create(7, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(7, Assert.IsType<int>(created.RouteValues!["roomId"]));
        Assert.Equal(42, Assert.IsType<int>(created.RouteValues["reservationId"]));
        Assert.Equal(expected, created.Value);
        _mockReservationService.Verify(s => s.CreateAsync(7, request, CancellationToken.None), Times.Once());
    }
}
