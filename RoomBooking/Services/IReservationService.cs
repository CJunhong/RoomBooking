using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoomBooking.Models.Requests;
using RoomBooking.Models.Responses;

namespace RoomBooking.Services;

public interface IReservationService
{
    Task<List<ReservationResponse>> ListAsync(
        int roomId, GetReservationsRequest request, CancellationToken cancellationToken);
    Task<ReservationResponse> FindAsync(
        int roomId, int reservationId, CancellationToken cancellationToken);
    Task<ReservationResponse> CreateAsync(
        int roomId, CreateReservationRequest request, CancellationToken cancellationToken);
}
