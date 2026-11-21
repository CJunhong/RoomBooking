using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoomBooking.Models.Responses;

namespace RoomBooking.Services;

public interface IRoomService
{
    Task<List<RoomResponse>> ListAsync(CancellationToken cancellationToken);
    Task<List<RoomResponse>> ListAvailableAsync(
        DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
}
