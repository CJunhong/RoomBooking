using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Data;
using RoomBooking.Data.Entities;
using RoomBooking.Data.Repositories.Filters;

namespace RoomBooking.Data.Repositories;

internal sealed class RoomRepository(BookingDbContext db) : IRoomRepository
{
    public async Task<bool> RoomExistsAsync(int roomId, CancellationToken cancellationToken)
    {
        return await db.Rooms.AnyAsync(room => room.Id == roomId, cancellationToken);
    }

    public async Task<List<Room>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Rooms.AsNoTracking().OrderBy(room => room.Id).ToListAsync(cancellationToken);
    }

    public async Task<List<Room>> ListAvailableAsync(
        DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var overlappingReservations = db.Reservations
            .Overlapping(start.UtcDateTime, end.UtcDateTime);

        return await db.Rooms.AsNoTracking()
            .Where(room => !overlappingReservations.Any(res => res.RoomId == room.Id))
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);
    }
}
