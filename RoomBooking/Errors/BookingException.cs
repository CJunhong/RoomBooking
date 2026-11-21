using System;
using RoomBooking.Models.Enums;

namespace RoomBooking.Errors;

internal sealed class BookingException(BookingError error) : Exception(error.ToString())
{
    public BookingError Error { get; } = error;
}
