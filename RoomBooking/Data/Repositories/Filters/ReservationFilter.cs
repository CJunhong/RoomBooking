using System;
using System.Linq;
using RoomBooking.Data.Entities;

namespace RoomBooking.Data.Repositories.Filters;

internal static class ReservationFilter
{
    public static IQueryable<Reservation> Overlapping(
        this IQueryable<Reservation> query, DateTime? start, DateTime? end)
    {
        if (start.HasValue)
        {
            query = query.Where(reservation => reservation.EndUtc > start.Value);
        }

        if (end.HasValue)
        {
            query = query.Where(reservation => reservation.StartUtc < end.Value);
        }

        return query;
    }
}
