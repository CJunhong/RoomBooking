using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RoomBooking.Models.Responses;
using RoomBooking.Services;

namespace RoomBooking.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(IRoomService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<RoomResponse>>(StatusCodes.Status200OK)]
    public async Task<List<RoomResponse>> List(CancellationToken cancellationToken)
    {
        return await service.ListAsync(cancellationToken);
    }

    [HttpGet("available")]
    [ProducesResponseType<List<RoomResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<List<RoomResponse>> ListAvailable(
        [FromQuery, BindRequired] DateTimeOffset start,
        [FromQuery, BindRequired] DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        return await service.ListAvailableAsync(start, end, cancellationToken);
    }
}
