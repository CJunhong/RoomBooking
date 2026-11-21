using System;

namespace RoomBooking.Models.Requests;

public sealed class GetReservationsRequest
{
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
}
