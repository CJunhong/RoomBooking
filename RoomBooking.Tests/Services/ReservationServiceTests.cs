using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RoomBooking.Models.Enums;
using RoomBooking.Models.Requests;
using RoomBooking.Data.Entities;
using RoomBooking.Errors;
using RoomBooking.Data.Repositories;
using RoomBooking.Services;
using Xunit;

namespace RoomBooking.Tests.Services;

public sealed class ReservationServiceTests
{
    private readonly Mock<IRoomRepository> _mockRoomRepository;
    private readonly Mock<IReservationRepository> _mockReservationRepository;
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _mockRoomRepository = new Mock<IRoomRepository>(MockBehavior.Strict);
        _mockReservationRepository = new Mock<IReservationRepository>(MockBehavior.Strict);
        _service = new ReservationService(_mockReservationRepository.Object, _mockRoomRepository.Object);
    }

    [Fact]
    public async Task ListAsync_RejectsInvalidTimeRangeBeforeCheckingRoom()
    {
        var start = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var request = new GetReservationsRequest { Start = start, End = start };

        var error = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            await _service.ListAsync(1, request, CancellationToken.None);
        });

        Assert.Equal(BookingError.InvalidRange, error.Error);
    }

    [Fact]
    public async Task ListAsync_ThrowsRoomNotFoundForUnknownRoom()
    {
        _mockRoomRepository.Setup(repo => repo.RoomExistsAsync(99, CancellationToken.None)).ReturnsAsync(false);

        var error = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            await _service.ListAsync(99, new GetReservationsRequest(), CancellationToken.None);
        });

        Assert.Equal(BookingError.RoomNotFound, error.Error);
        _mockRoomRepository.Verify(repo => repo.RoomExistsAsync(99, CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task ListAsync_MapsReservationsToResponses()
    {
        var startUtc = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2030, 1, 1, 11, 0, 0, DateTimeKind.Utc);
        var createdUtc = new DateTime(2030, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var request = new GetReservationsRequest { Start = new DateTimeOffset(startUtc, TimeSpan.Zero) };
        var entities = new List<Reservation>
        {
            new() { Id = 5, RoomId = 1, Title = "Standup", StartUtc = startUtc, EndUtc = endUtc, CreatedAtUtc = createdUtc }
        };
        _mockRoomRepository.Setup(repo => repo.RoomExistsAsync(1, CancellationToken.None)).ReturnsAsync(true);
        _mockReservationRepository.Setup(repo => repo.ListAsync(1, request.Start, request.End, CancellationToken.None))
            .ReturnsAsync(entities);

        var result = await _service.ListAsync(1, request, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(5, item.Id);
        Assert.Equal(1, item.RoomId);
        Assert.Equal("Standup", item.Title);
        Assert.Equal(new DateTimeOffset(startUtc, TimeSpan.Zero), item.Start);
        Assert.Equal(new DateTimeOffset(endUtc, TimeSpan.Zero), item.End);
        Assert.Equal(new DateTimeOffset(createdUtc, TimeSpan.Zero), item.CreatedAt);
        _mockRoomRepository.Verify(repo => repo.RoomExistsAsync(1, CancellationToken.None), Times.Once());
        _mockReservationRepository.Verify(repo => repo.ListAsync(1, request.Start, request.End, CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task FindAsync_ReturnsMappedReservationWhenFound()
    {
        var startUtc = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2030, 1, 1, 11, 0, 0, DateTimeKind.Utc);
        var entity = new Reservation { Id = 42, RoomId = 7, Title = "Sync", StartUtc = startUtc, EndUtc = endUtc };
        _mockReservationRepository.Setup(repo => repo.FindAsync(7, 42, CancellationToken.None)).ReturnsAsync(entity);

        var response = await _service.FindAsync(7, 42, CancellationToken.None);

        Assert.Equal(42, response.Id);
        Assert.Equal(7, response.RoomId);
        Assert.Equal("Sync", response.Title);
        _mockReservationRepository.Verify(repo => repo.FindAsync(7, 42, CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task FindAsync_ThrowsReservationNotFoundWhenMissing()
    {
        _mockReservationRepository.Setup(repo => repo.FindAsync(7, 999, CancellationToken.None))
            .ReturnsAsync((Reservation?)null);

        var error = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            await _service.FindAsync(7, 999, CancellationToken.None);
        });

        Assert.Equal(BookingError.ReservationNotFound, error.Error);
        _mockReservationRepository.Verify(repo => repo.FindAsync(7, 999, CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidRangeWithoutCallingRepository()
    {
        var start = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var request = new CreateReservationRequest { Start = start, End = start, Title = "Invalid" };

        var error = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            await _service.CreateAsync(1, request, CancellationToken.None);
        });

        Assert.Equal(BookingError.InvalidRange, error.Error);
    }

    [Fact]
    public async Task CreateAsync_NormalizesUtcTrimsTitleAndReturnsCreatedResponse()
    {
        Reservation? persisted = null;
        _mockReservationRepository.Setup(repo => repo.TryCreateAsync(It.IsAny<Reservation>(), CancellationToken.None))
            .Callback<Reservation, CancellationToken>((reservation, _) =>
            {
                reservation.Id = 15;
                persisted = reservation;
            })
            .ReturnsAsync(ReservationWriteResult.Created);
        var start = new DateTimeOffset(2030, 1, 1, 18, 0, 0, TimeSpan.FromHours(8));
        var request = new CreateReservationRequest
        {
            Start = start,
            End = start.AddHours(1),
            Title = " Design review "
        };

        var response = await _service.CreateAsync(7, request, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(7, persisted.RoomId);
        Assert.Equal("Design review", persisted.Title);
        Assert.Equal(DateTimeKind.Utc, persisted.StartUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, persisted.EndUtc.Kind);
        Assert.Equal(start.UtcDateTime, persisted.StartUtc);
        Assert.Equal(request.End.UtcDateTime, persisted.EndUtc);

        Assert.Equal(15, response.Id);
        Assert.Equal(7, response.RoomId);
        Assert.Equal("Design review", response.Title);
        Assert.Equal(start.ToUniversalTime(), response.Start);
        Assert.Equal(request.End.ToUniversalTime(), response.End);
        _mockReservationRepository.Verify(repo => repo.TryCreateAsync(It.IsAny<Reservation>(), CancellationToken.None), Times.Once());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_RejectsBlankTitleWithoutCallingRepository(string invalidTitle)
    {
        var start = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var request = new CreateReservationRequest { Start = start, End = start.AddHours(1), Title = invalidTitle };

        var error = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            await _service.CreateAsync(1, request, CancellationToken.None);
        });

        Assert.Equal(BookingError.InvalidTitle, error.Error);
    }

    [Fact]
    public async Task CreateAsync_RejectsTitleExceeding200CharactersWithoutCallingRepository()
    {
        var start = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var request = new CreateReservationRequest { Start = start, End = start.AddHours(1), Title = new string('a', 201) };

        var error = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            await _service.CreateAsync(1, request, CancellationToken.None);
        });

        Assert.Equal(BookingError.InvalidTitle, error.Error);
    }

    [Theory]
    [InlineData(nameof(ReservationWriteResult.Conflict), nameof(BookingError.Conflict))]
    [InlineData(nameof(ReservationWriteResult.RoomNotFound), nameof(BookingError.RoomNotFound))]
    public async Task CreateAsync_MapsRepositoryFailureToBookingException(
        string writeResultName, string expectedErrorName)
    {
        var writeResult = Enum.Parse<ReservationWriteResult>(writeResultName);
        var expectedError = Enum.Parse<BookingError>(expectedErrorName);
        _mockReservationRepository.Setup(repo => repo.TryCreateAsync(It.IsAny<Reservation>(), CancellationToken.None))
            .ReturnsAsync(writeResult);
        var start = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var request = new CreateReservationRequest { Start = start, End = start.AddHours(1), Title = "Review" };

        var error = await Assert.ThrowsAsync<BookingException>(async () =>
        {
            await _service.CreateAsync(7, request, CancellationToken.None);
        });

        Assert.Equal(expectedError, error.Error);
        _mockReservationRepository.Verify(repo => repo.TryCreateAsync(It.IsAny<Reservation>(), CancellationToken.None), Times.Once());
    }
}
