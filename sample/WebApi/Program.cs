using Davish.Result;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Davish.Result.AspNetCore.Http: Exception -> Result mapping, and Minimal API failure handling.
builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException>((context, exception) =>
        Result.Failure(new Error("Booking.Locked", $"Booking {exception.BookingId} is already confirmed.", ErrorType.Conflict)));

    o.ConfigureProblemDetails(c => c.CustomizeProblemDetails = cotext =>
            cotext.ProblemDetails.Instance = $"{cotext.HttpContext.Request.Method} {cotext.HttpContext.Request.Path}");

    o.AddMinimalApiResult();

    o.ConfigureStatusCodes(x => x.UseDefault = true);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(); // required for MapExceptionToResult to take effect

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Davish.Result.AspNetCore.Http sample endpoints.
var bookings = new Dictionary<int, Booking> { [1] = new Booking(1, "Alice", Confirmed: false) };

app.MapGet("/bookings/{id:int}", (int id) => FindBooking(id).ToOk())
    .WithName("GetBooking");

app.MapPost("/bookings", (CreateBookingRequest request) =>
    CreateBooking(request).ToCreated("GetBooking", b => new { id = b.Id }));

app.MapDelete("/bookings/{id:int}", (int id) => DeleteBooking(id).ToNoContent());

// Throws instead of returning a Result — MapExceptionToResult<BookingLockedException>(...) above converts it
// into the same 409 Conflict + ProblemDetails shape a directly-returned Result.Failure would produce.
app.MapPost("/bookings/{id:int}/confirm", (int id) =>
{
    if (!bookings.TryGetValue(id, out var booking))
        return FindBooking(id).ToNoContent();

    if (booking.Confirmed)
        throw new BookingLockedException(id);

    bookings[id] = booking with { Confirmed = true };
    return Result.Success().ToNoContent();
});

Result<Booking> FindBooking(int id) =>
    bookings.TryGetValue(id, out var booking)
        ? Result.Success(booking)
        : Result.Failure<Booking>(new Error("Booking.NotFound", $"Booking {id} was not found.", ErrorType.NotFound));

Result<Booking> CreateBooking(CreateBookingRequest request)
{
    var id = bookings.Count == 0 ? 1 : bookings.Keys.Max() + 1;
    var booking = new Booking(id, request.CustomerName, Confirmed: false);
    bookings[id] = booking;
    return Result.Success(booking);
}

Result DeleteBooking(int id) =>
    bookings.Remove(id)
        ? Result.Success()
        : Result.Failure(new Error("Booking.NotFound", $"Booking {id} was not found.", ErrorType.NotFound));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record Booking(int Id, string CustomerName, bool Confirmed);

record CreateBookingRequest(string CustomerName);

sealed class BookingLockedException(int bookingId) : Exception
{
    public int BookingId { get; } = bookingId;
}
