using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoomBooking.Data.Entities;

namespace RoomBooking.Data.Repositories;

internal interface IRoomRepository
{
    Task<bool> RoomExistsAsync(int roomId, CancellationToken cancellationToken);
    Task<List<Room>> ListAsync(CancellationToken cancellationToken);
    Task<List<Room>> ListAvailableAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
}
