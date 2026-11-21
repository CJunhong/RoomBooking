using System;
using RoomBooking.Data.Entities;

namespace RoomBooking.Models.Responses;

public sealed record RoomResponse(
    int Id, string Name, int Capacity, DateTimeOffset CreatedAt)
{
    internal static RoomResponse From(Room room)
    {
        return new(
            room.Id,
            room.Name,
            room.Capacity,
            new DateTimeOffset(room.CreatedAtUtc, TimeSpan.Zero));
    }
}
