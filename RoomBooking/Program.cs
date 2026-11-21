using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoomBooking.Data;
using RoomBooking.Errors;
using RoomBooking.Data.Repositories;
using RoomBooking.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<BookingDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("Bookings")
    ?? throw new InvalidOperationException("The Bookings connection string is required.")));

builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("../openapi/v1.json", "Room Booking API v1");
    });
}
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (db.Database.IsSqlite())
    {
        await db.Database.ExecuteSqlRawAsync(
            "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");
    }
}

app.Run();
