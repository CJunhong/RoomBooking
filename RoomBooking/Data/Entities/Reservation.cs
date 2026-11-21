using System;

namespace RoomBooking.Data.Entities;

internal sealed class Reservation
{
    public int Id { get; set; }
    public required int RoomId { get; set; }
    public required string Title { get; set; }
    public required DateTime StartUtc { get; set; }
    public required DateTime EndUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
