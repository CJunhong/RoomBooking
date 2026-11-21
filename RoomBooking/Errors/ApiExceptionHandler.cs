using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoomBooking.Models.Enums;

namespace RoomBooking.Errors;

internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request was cancelled by the client.");
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var cause = exception;
        if (exception is DbUpdateException updateException &&
            updateException.InnerException is SqliteException sqliteException)
        {
            cause = sqliteException;
        }

        var (status, title, detail) = cause switch
        {
            BookingException { Error: BookingError.InvalidRange } =>
                (400, "Invalid time range", "Start must be earlier than end."),
            BookingException { Error: BookingError.InvalidTitle } =>
                (400, "Invalid title", "Title must be between 1 and 200 non-whitespace characters."),
            BookingException { Error: BookingError.RoomNotFound } =>
                (404, "Room not found", "The requested room does not exist."),
            BookingException { Error: BookingError.ReservationNotFound } =>
                (404, "Reservation not found", "The requested reservation does not exist in this room."),
            BookingException { Error: BookingError.Conflict } =>
                (409, "Reservation conflict", "The room is already booked during the requested time range."),
            SqliteException { SqliteErrorCode: 5 or 6 } =>
                (503, "Database busy", "Retry the request shortly."),
            _ => (500, "Internal server error", "An unexpected error occurred.")
        };

        if (status == 500)
            logger.LogError(exception, "Unexpected booking API error");
        else if (status == 503)
        {
            logger.LogWarning(exception, "SQLite busy/locked while handling booking request");
            context.Response.Headers["Retry-After"] = "1";
        }

        context.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        if (!await problemDetails.TryWriteAsync(new ProblemDetailsContext
            { HttpContext = context, ProblemDetails = problem, Exception = exception }))
        {
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
        }

        return true;
    }
}
