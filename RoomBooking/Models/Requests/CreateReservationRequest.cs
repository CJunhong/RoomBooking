using System;
using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Models.Requests;

public sealed class CreateReservationRequest
{
    public required DateTimeOffset Start { get; set; }
    public required DateTimeOffset End { get; set; }

    [Required, StringLength(200)]
    public required string Title { get; set; }
}
