using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Davish.Result.AspNetCore.Http.Tests;

/// <summary>
/// <see cref="ResultToMinimalResultExtension"/> against a real, in-memory ASP.NET Core host
/// (<see cref="TestServer"/>) instead of calling the extension methods directly, to catch issues that only
/// show up once ASP.NET Core actually routes a request and writes the response (e.g. a <c>CreatedAtRoute</c>
/// call failing because the target route was never named).
/// </summary>
[Collection(ResultHttpOptionsCollection.Name)]
public sealed class ResultAspNetCoreHttpIntegrationTests : IAsyncLifetime
{
    private static readonly Error NotFoundError = new("Booking.NotFound", "Booking was not found", ErrorType.NotFound);

    private IHost _host = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        ResultHttpOptions.ResetForTesting();

        _host = await new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();

                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddCustomResultErrorTypeMap(v =>
                        v.CustomMap = new Dictionary<ErrorTypeBase, int>
                        {
                            [RateLimitedErrorType.Instance] = StatusCodes.Status429TooManyRequests
                        });
                });

                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/bookings/{id:int}", (int id) => Find(id).ToOk())
                            .WithName("GetBooking");

                        endpoints.MapPost("/bookings/{id:int}", (int id) =>
                            Create(id).ToCreated("GetBooking", b => new { id = b.Id }));

                        endpoints.MapDelete("/bookings/{id:int}", (int id) => Delete(id).ToNoContent());

                        endpoints.MapPost("/bookings/{id:int}/accept", (int id) =>
                            Find(id).ToAccepted($"/bookings/{id}/status"));

                        endpoints.MapGet("/bookings/rate-limited", () =>
                            Result.Failure<Booking>(new Error("Booking.RateLimited", "Slow down", RateLimitedErrorType.Instance)).ToOk());
                    });
                });
            })
            .StartAsync();

        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        ResultHttpOptions.ResetForTesting();
    }

    [Fact]
    public async Task GivenExistingBooking_WhenGet_ThenReturnsOkWithBody()
    {
        var response = await _client.GetAsync("/bookings/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<Booking>();
        Assert.Equal(1, booking!.Id);
    }

    [Fact]
    public async Task GivenMissingBooking_WhenGet_ThenReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync("/bookings/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(NotFoundError.Code, body);
    }

    [Fact]
    public async Task GivenNewBooking_WhenPost_ThenReturnsCreatedWithLocationHeader()
    {
        var response = await _client.PostAsync("/bookings/2", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/bookings/2", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task GivenBooking_WhenDelete_ThenReturnsNoContent()
    {
        var response = await _client.DeleteAsync("/bookings/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GivenBooking_WhenAccept_ThenReturnsAcceptedWithLocationHeader()
    {
        var response = await _client.PostAsync("/bookings/1/accept", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("/bookings/1/status", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GivenCustomMappedErrorType_WhenGet_ThenReturnsRegisteredStatusCode()
    {
        var response = await _client.GetAsync("/bookings/rate-limited");

        Assert.Equal((HttpStatusCode)StatusCodes.Status429TooManyRequests, response.StatusCode);
    }

    private static Result<Booking> Find(int id) =>
        id == 1 ? Result.Success(new Booking(1)) : Result.Failure<Booking>(NotFoundError);

    private static Result<Booking> Create(int id) => Result.Success(new Booking(id));

    private static Result Delete(int id) =>
        id == 1 ? Result.Success() : Result.Failure(NotFoundError);

    private sealed record Booking(int Id);

    private sealed class RateLimitedErrorType : ErrorType
    {
        public static readonly RateLimitedErrorType Instance = new();
        private RateLimitedErrorType() : base(nameof(RateLimitedErrorType))
        {
        }
    }
}
