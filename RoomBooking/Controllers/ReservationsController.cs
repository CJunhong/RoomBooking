using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Models.Requests;
using RoomBooking.Models.Responses;
using RoomBooking.Services;

namespace RoomBooking.Controllers;

[ApiController]
[Route("api/rooms/{roomId:int}/reservations")]
public sealed class ReservationsController(IReservationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<ReservationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<List<ReservationResponse>> List(
        [FromRoute] int roomId, [FromQuery] GetReservationsRequest request,
        CancellationToken cancellationToken)
    {
        return await service.ListAsync(roomId, request, cancellationToken);
    }

    [HttpGet("{reservationId:int}")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ReservationResponse> Get(
        [FromRoute] int roomId, [FromRoute] int reservationId, CancellationToken cancellationToken)
    {
        return await service.FindAsync(roomId, reservationId, cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromRoute] int roomId, [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var reservation = await service.CreateAsync(roomId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { roomId, reservationId = reservation.Id }, reservation);
    }
}
