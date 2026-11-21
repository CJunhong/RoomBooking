using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoomBooking.Models.Enums;
using RoomBooking.Models.Requests;
using RoomBooking.Models.Responses;
using RoomBooking.Errors;
using RoomBooking.Data.Entities;
using RoomBooking.Data.Repositories;
using RoomBooking.Services.Validators;

namespace RoomBooking.Services;

internal sealed class ReservationService(IReservationRepository repository, IRoomRepository roomRepository) : IReservationService
{
    public async Task<List<ReservationResponse>> ListAsync(
        int roomId, GetReservationsRequest request, CancellationToken cancellationToken)
    {
        BookingTimeRangeValidator.Validate(request.Start, request.End);
        if (!await roomRepository.RoomExistsAsync(roomId, cancellationToken))
            throw new BookingException(BookingError.RoomNotFound);

        var reservations = await repository.ListAsync(roomId, request.Start, request.End, cancellationToken);
        return reservations.ConvertAll(ReservationResponse.From);
    }

    public async Task<ReservationResponse> FindAsync(
        int roomId, int reservationId, CancellationToken cancellationToken)
    {
        var reservation = await repository.FindAsync(roomId, reservationId, cancellationToken);
        return reservation is null
            ? throw new BookingException(BookingError.ReservationNotFound)
            : ReservationResponse.From(reservation);
    }

    public async Task<ReservationResponse> CreateAsync(
        int roomId, CreateReservationRequest request, CancellationToken cancellationToken)
    {
        BookingTimeRangeValidator.Validate(request.Start, request.End);

        var title = request.Title.Trim();
        if (title.Length == 0 || title.Length > 200)
            throw new BookingException(BookingError.InvalidTitle);

        var reservation = new Reservation
        {
            RoomId = roomId,
            StartUtc = request.Start.UtcDateTime,
            EndUtc = request.End.UtcDateTime,
            Title = title
        };

        var result = await repository.TryCreateAsync(reservation, cancellationToken);
        return result switch
        {
            ReservationWriteResult.Created => ReservationResponse.From(reservation),
            ReservationWriteResult.RoomNotFound => throw new BookingException(BookingError.RoomNotFound),
            ReservationWriteResult.Conflict => throw new BookingException(BookingError.Conflict),
            _ => throw new InvalidOperationException("Unexpected reservation write result.")
        };
    }
}
