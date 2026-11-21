using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoomBooking.Data.Entities;
using RoomBooking.Models.Enums;

namespace RoomBooking.Data.Repositories;

internal interface IReservationRepository
{
    Task<List<Reservation>> ListAsync(
        int roomId, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken);
    Task<Reservation?> FindAsync(int roomId, int reservationId, CancellationToken cancellationToken);
    Task<ReservationWriteResult> TryCreateAsync(Reservation reservation, CancellationToken cancellationToken);
}
