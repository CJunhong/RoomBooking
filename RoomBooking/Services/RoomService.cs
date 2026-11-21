using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoomBooking.Models.Responses;
using RoomBooking.Data.Repositories;
using RoomBooking.Services.Validators;

namespace RoomBooking.Services;

internal sealed class RoomService(IRoomRepository repository) : IRoomService
{
    public async Task<List<RoomResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var rooms = await repository.ListAsync(cancellationToken);
        return rooms.ConvertAll(RoomResponse.From);
    }

    public async Task<List<RoomResponse>> ListAvailableAsync(
        DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        BookingTimeRangeValidator.Validate(start, end);
        var rooms = await repository.ListAvailableAsync(start, end, cancellationToken);
        return rooms.ConvertAll(RoomResponse.From);
    }
}
