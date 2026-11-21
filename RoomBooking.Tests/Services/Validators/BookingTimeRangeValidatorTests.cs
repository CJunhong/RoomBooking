using System;
using RoomBooking.Models.Enums;
using RoomBooking.Errors;
using RoomBooking.Services.Validators;
using Xunit;

namespace RoomBooking.Tests.Services.Validators;

public sealed class BookingTimeRangeValidatorTests
{
    [Fact]
    public void Validate_AllowsStartEarlierThanEnd()
    {
        var start = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes(30);

        BookingTimeRangeValidator.Validate(start, end);
    }

    [Theory]
    [InlineData("2030-01-01T10:00:00Z", "2030-01-01T10:00:00Z")]
    [InlineData("2030-01-01T12:00:00+02:00", "2030-01-01T10:00:00Z")]
    [InlineData("2030-01-01T11:00:00Z", "2030-01-01T10:00:00Z")]
    public void Validate_RejectsEqualOrReversedRange(string startIso, string endIso)
    {
        var start = DateTimeOffset.Parse(startIso);
        var end = DateTimeOffset.Parse(endIso);

        var error = Assert.Throws<BookingException>(() => BookingTimeRangeValidator.Validate(start, end));

        Assert.Equal(BookingError.InvalidRange, error.Error);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("2030-01-01T10:00:00Z", null)]
    [InlineData(null, "2030-01-01T11:00:00Z")]
    [InlineData("2030-01-01T10:00:00Z", "2030-01-01T11:00:00Z")]
    public void ValidateNullable_AllowsMissingOrValidBounds(string? startIso, string? endIso)
    {
        DateTimeOffset? start = startIso is null ? null : DateTimeOffset.Parse(startIso);
        DateTimeOffset? end = endIso is null ? null : DateTimeOffset.Parse(endIso);

        BookingTimeRangeValidator.Validate(start, end);
    }
}
