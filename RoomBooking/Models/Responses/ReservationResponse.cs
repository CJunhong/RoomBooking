using System;
using RoomBooking.Data.Entities;

namespace RoomBooking.Models.Responses;

public sealed record ReservationResponse(
    int Id, int RoomId, DateTimeOffset Start, DateTimeOffset End,
    string Title, DateTimeOffset CreatedAt)
{
    internal static ReservationResponse From(Reservation reservation)
    {
        return new(
            reservation.Id,
            reservation.RoomId,
            new DateTimeOffset(reservation.StartUtc, TimeSpan.Zero),
            new DateTimeOffset(reservation.EndUtc, TimeSpan.Zero),
            reservation.Title,
            new DateTimeOffset(reservation.CreatedAtUtc, TimeSpan.Zero));
    }
}
