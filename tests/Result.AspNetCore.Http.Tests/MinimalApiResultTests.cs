using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Davish.Result.AspNetCore.Http.Tests;

/// <summary>
/// Tests <see cref="ResultToMinimalResultExtension"/> and the <see cref="MinimalApiResult"/>/
/// <see cref="MinimalApiResult{TValue}"/> family it returns. Success is written immediately; failure is deferred
/// to the registered <see cref="IMinimalApiFailureHandler"/> at <see cref="IResult.ExecuteAsync"/> time, so tests
/// that need the actual response body/status code execute against a real <see cref="DefaultHttpContext"/>.
/// </summary>
public class MinimalApiResultTests
{
    private static readonly Error NotFoundError = new("Booking.NotFound", "Booking was not found", ErrorType.NotFound);

    private sealed record Booking(int Id);

    private static async Task<DefaultHttpContext> ExecuteAsync(IResult result)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResultAspNetCore(o => o.AddMinimalApiResult());

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };

        await result.ExecuteAsync(httpContext);
        httpContext.Response.Body.Position = 0;

        return httpContext;
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext httpContext)
    {
        using var reader = new StreamReader(httpContext.Response.Body);
        return await reader.ReadToEndAsync();
    }

    // ---- Non-generic (Result, no value) ----

    [Fact]
    public void GivenSuccessResult_WhenToOk_ThenStatusCodeIs200()
    {
        var httpResult = Result.Success().ToOk();

        Assert.Equal(StatusCodes.Status200OK, httpResult.StatusCode);
    }

    [Fact]
    public void GivenFailedResult_WhenToOk_ThenStatusCodeReflectsErrorType()
    {
        var httpResult = Result.Failure(NotFoundError).ToOk();

        Assert.Equal(StatusCodes.Status404NotFound, httpResult.StatusCode);
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToOkExecuted_ThenWrites200()
    {
        var context = await ExecuteAsync(Result.Success().ToOk());

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenFailedResult_WhenToOkExecuted_ThenDelegatesToDefaultMinimalApiFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure(NotFoundError).ToOk());

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Contains(NotFoundError.Code, body);
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToNoContentExecuted_ThenWrites204()
    {
        var context = await ExecuteAsync(Result.Success().ToNoContent());

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenFailedResult_WhenToNoContentExecuted_ThenDelegatesToDefaultMinimalApiFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure(NotFoundError).ToNoContent());

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public void GivenSuccessResult_WhenToCreated_ThenStatusCodeIs201()
    {
        // Executing CreatedAtRoute for real requires a real route table (LinkGenerator), which is what
        // ResultAspNetCoreHttpIntegrationTests.GivenNewBooking_WhenPost_ThenReturnsCreatedWithLocationHeader
        // covers end-to-end. This just checks the synchronous StatusCode property.
        var httpResult = Result.Success().ToCreated("GetBooking", new { id = 1 });

        Assert.Equal(StatusCodes.Status201Created, httpResult.StatusCode);
    }

    [Fact]
    public async Task GivenFailedResult_WhenToCreatedExecuted_ThenDelegatesToDefaultMinimalApiFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure(NotFoundError).ToCreated("GetBooking"));

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToAcceptedExecuted_ThenWrites202WithLocation()
    {
        var context = await ExecuteAsync(Result.Success().ToAccepted("/bookings/1/status"));

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("/bookings/1/status", context.Response.Headers.Location);
    }

    [Fact]
    public async Task GivenFailedResult_WhenToAcceptedExecuted_ThenDelegatesToDefaultMinimalApiFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure(NotFoundError).ToAccepted());

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    // ---- Generic (Result<T>, with value) ----

    [Fact]
    public void GivenSuccessResultValue_WhenToOk_ThenValuePropertyReturnsIt()
    {
        var httpResult = Result.Success(42).ToOk();

        Assert.Equal(StatusCodes.Status200OK, httpResult.StatusCode);
        Assert.Equal(42, ((IValueHttpResult<int>)httpResult).Value);
    }

    [Fact]
    public void GivenFailedResultValue_WhenGenericValueRead_ThenThrowsResultValueUnavailableException()
    {
        IValueHttpResult<int> httpResult = Result.Failure<int>(NotFoundError).ToOk();

        Assert.Throws<ResultValueUnavailableException>(() => httpResult.Value);
    }

    [Fact]
    public void GivenFailedResultValue_WhenNonGenericValueRead_ThenReturnsNullInsteadOfThrowing()
    {
        IValueHttpResult httpResult = Result.Failure<int>(NotFoundError).ToOk();

        Assert.Null(httpResult.Value);
    }

    [Fact]
    public async Task GivenSuccessResultValue_WhenToOkExecuted_ThenWritesValueAsBody()
    {
        var context = await ExecuteAsync(Result.Success(42).ToOk());

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Equal("42", body);
    }

    [Fact]
    public async Task GivenFailedResultValue_WhenToOkExecuted_ThenDelegatesToDefaultMinimalApiFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure<int>(NotFoundError).ToOk());

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public void GivenSuccessResultValue_WhenToCreated_ThenStatusCodeIs201()
    {
        // See GivenSuccessResult_WhenToCreated_ThenStatusCodeIs201 above for why this isn't executed end-to-end here.
        var httpResult = Result.Success(new Booking(42)).ToCreated("GetBooking", b => new { id = b.Id });

        Assert.Equal(StatusCodes.Status201Created, httpResult.StatusCode);
    }

    [Fact]
    public async Task GivenFailedResultValue_WhenToCreatedWithRouteValuesFactory_ThenFactoryIsNotInvoked()
    {
        var factoryInvoked = false;

        var context = await ExecuteAsync(Result.Failure<Booking>(NotFoundError).ToCreated("GetBooking", value =>
        {
            factoryInvoked = true;
            return new { value.Id };
        }));

        Assert.False(factoryInvoked);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenSuccessResultValue_WhenToAcceptedExecuted_ThenWritesValueAndLocation()
    {
        var context = await ExecuteAsync(Result.Success(42).ToAccepted("/bookings/42/status"));

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("/bookings/42/status", context.Response.Headers.Location);
    }

    [Fact]
    public async Task GivenSuccessResultValue_WhenToNoContentExecuted_ThenWrites204EvenThoughValueExists()
    {
        var context = await ExecuteAsync(Result.Success(42).ToNoContent());

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }
}
