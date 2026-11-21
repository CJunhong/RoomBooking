# Room Booking API

A .NET 10 ASP.NET Core Web API with EF Core 10 and local SQLite.

## Run

From the solution directory with the .NET 10 SDK installed:

```sh
dotnet build
dotnet test
dotnet run --project RoomBooking --launch-profile RoomBooking
```

Startup creates the SQLite database at `RoomBooking/Data/room-booking.db` by default when started with the project launch profile and seeds three rooms with capacities of 4, 10, and 50. Set `ConnectionStrings__Bookings` to override the default connection string.

## Swagger UI

1. Start the API with `dotnet run --project RoomBooking --launch-profile RoomBooking`. To debug from your IDE, select the `RoomBooking` launch profile and start debugging.
2. Open [Swagger UI](http://localhost:5000/swagger), expand an endpoint, and select **Try it out**.
3. For `GET /api/rooms/{roomId}/reservations`, enter `1` for `roomId`, `2030-01-01T18:30:00+08:00` for `Start`, and `2030-01-01T20:00:00+08:00` for `End`. Run the POST example below first, then select **Execute** to see the reservation that overlaps the window. Response timestamps are returned in UTC.

The launch profile sets the environment to `Development` and listens on port 5000. If Swagger returns 404, check the startup log for the hosting environment and listening address. The raw specification is available at [OpenAPI JSON](http://localhost:5000/openapi/v1.json).

## Project structure

```text
RoomBooking.sln
RoomBooking/
  Controllers/    RoomsController and ReservationsController
  Services/       RoomService and ReservationService with separate interfaces
    Validators/   Shared booking time range validation
  Data/           BookingDbContext and EF Core model configuration
    Entities/     Internal Room and Reservation entities
    Repositories/ Room and reservation repositories with separate interfaces
      Filters/    Shared reservation filters
  Models/
    Requests/     Request DTOs
    Responses/    Response DTOs
    Enums/        BookingError and ReservationWriteResult
  Errors/         Booking errors and Problem Details handler
RoomBooking.Tests/
  Controllers/    Controller unit tests with mocked services
  Services/       Service unit tests
    Validators/   Booking time range validator tests
  Repositories/   Query, constraint, and concurrent SQLite transaction tests
```

## API

| Method | Path | Behavior |
| --- | --- | --- |
| GET | `/api/rooms` | Lists all rooms |
| GET | `/api/rooms/available?start=...&end=...` | Lists rooms free for the entire window. Both bounds are required. |
| GET | `/api/rooms/{roomId}/reservations` | Lists reservations by start time. Optional start and end bounds filter by overlap. |
| GET | `/api/rooms/{roomId}/reservations/{reservationId}` | Gets a single reservation |
| POST | `/api/rooms/{roomId}/reservations` | Creates a reservation and returns 201 with a Location header |

Send ISO 8601 timestamps with an offset or UTC `Z` suffix. URL-encode a positive offset in query strings.

```sh
curl -i http://localhost:5000/api/rooms/1/reservations \
  -H 'Content-Type: application/json' \
  -d '{"start":"2030-01-01T10:00:00Z","end":"2030-01-01T11:00:00Z","title":"Design review"}'
```

Invalid requests return `400`. Missing resources return `404`. Overlapping reservations return `409`. SQLite busy or locked errors return `503` with a `Retry-After` header of 1 second. Errors are represented as Problem Details (`application/problem+json`).

## Design

- **Half-open intervals**: Reservations use `[start, end)` semantics. Two intervals overlap when `existing.StartUtc < requestedEnd` and `existing.EndUtc > requestedStart`. Adjacent reservations (e.g., `10:00–11:00` and `11:00–12:00`) are allowed.
- **Conflict handling**: `ReservationRepository.TryCreateAsync` starts a non-deferred serializable SQLite transaction (`BEGIN IMMEDIATE`), acquiring the database write lock before checking for overlaps so competing API writes serialize cleanly against committed data.
- **Concurrency & read/write throughput**: Startup requests WAL mode and applies `synchronous=NORMAL` to the startup connection. WAL allows readers and a writer to proceed concurrently.
- **Time storage & indexing**: Timestamps are persisted as UTC `DateTime` values so SQLite lexicographical ordering matches chronological ordering. The composite index `(RoomId, EndUtc, StartUtc)` supports narrowing the candidate reservations by room and end time for conflict checks and room availability queries.

## Production Considerations

To keep the exercise focused and easy to run locally, several production capabilities were intentionally omitted. In a real production deployment, I would approach them as follows:

- **Multi-instance concurrency & database engine**: SQLite serializes writes through a single-file lock and cannot prevent conflicting direct writes outside this API. For a multi-instance service, I would move to **PostgreSQL** and enforce non-overlapping intervals at the database level using a `tstzrange` column with a **GiST exclusion constraint** (`EXCLUDE USING gist (room_id WITH =, during WITH &&)`), mapping exclusion violations (`23P01`) to `409 Conflict`.
- **Schema migrations & deployment**: Replace `EnsureCreatedAsync` with EF Core Migrations executed as a dedicated CI/CD or pre-deployment migration step (rather than at application startup across multiple replicas).
- **Authentication & authorization**: Add OIDC / JWT Bearer authentication, store `CreatedByUserId` on each reservation, and enforce ownership/role-based authorization on mutating endpoints.
- **Reservation lifecycle & idempotency**: Add cancellation (`DELETE`, or soft-delete to retain audit history) and rescheduling (`PUT`/`PATCH` with optimistic concurrency via `ETag`/`If-Match`). Support an `Idempotency-Key` header on `POST` so client retries after transient network timeouts do not fail with spurious `409 Conflict` responses.
- **Pagination & query bounds**: Add keyset/cursor pagination ordered by `(StartUtc, Id)` with a capped `pageSize` on `GET /api/rooms/{roomId}/reservations`, and consider adding a `(RoomId, StartUtc, Id)` index if read-heavy listing workloads dominate.
- **Observability, health checks & protection**: Instrument with OpenTelemetry (distributed traces, structured logs, and metrics such as conflict rates and DB lock wait times), expose `/health/live` and `/health/ready` endpoints, and enable ASP.NET Core Rate Limiting.
