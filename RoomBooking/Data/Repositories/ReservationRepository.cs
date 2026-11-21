using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Models.Enums;
using RoomBooking.Data.Entities;
using RoomBooking.Data.Repositories.Filters;

namespace RoomBooking.Data.Repositories;

internal sealed class ReservationRepository(BookingDbContext db) : IReservationRepository
{
    // List reservations for a room, optionally filtering by the given time range. 
    // If start and end are both null, all reservations for the room are returned.
    public async Task<List<Reservation>> ListAsync(
        int roomId, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken)
    {
        return await db.Reservations.AsNoTracking().Overlapping(start?.UtcDateTime, end?.UtcDateTime)
                .Where(reservation => reservation.RoomId == roomId)
                .OrderBy(reservation => reservation.StartUtc)
                .ThenBy(reservation => reservation.Id)
                .ToListAsync(cancellationToken);
    }

    public async Task<Reservation?> FindAsync(int roomId, int reservationId, CancellationToken cancellationToken)
    {
        return await db.Reservations.AsNoTracking().SingleOrDefaultAsync(
                reservation => reservation.RoomId == roomId && reservation.Id == reservationId,
                cancellationToken);
    }

    public async Task<ReservationWriteResult> TryCreateAsync(
        Reservation reservation, CancellationToken cancellationToken)
    {
        // Consider of the meeting will not be deleted
        if (!await db.Rooms.AnyAsync(room => room.Id == reservation.RoomId, cancellationToken))
            return ReservationWriteResult.RoomNotFound;
        
        // SQLite acquires its write lock before the first read in this non-deferred transaction.
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        if (await db.Reservations.Overlapping(reservation.StartUtc, reservation.EndUtc)
            .AnyAsync(existing => existing.RoomId == reservation.RoomId, cancellationToken))
        {
            return ReservationWriteResult.Conflict;
        }

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ReservationWriteResult.Created;
    }
}
