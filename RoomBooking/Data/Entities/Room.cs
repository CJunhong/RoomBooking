using System;

namespace RoomBooking.Data.Entities;

internal sealed class Room
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required int Capacity { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
