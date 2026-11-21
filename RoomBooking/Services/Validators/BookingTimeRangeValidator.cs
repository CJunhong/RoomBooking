using System;
using RoomBooking.Models.Enums;
using RoomBooking.Errors;

namespace RoomBooking.Services.Validators;

internal static class BookingTimeRangeValidator
{
    public static void Validate(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (!start.HasValue || !end.HasValue)
        {
            return;
        }

        if (start.Value >= end.Value)
        {
            throw new BookingException(BookingError.InvalidRange);
        }
    }
}
