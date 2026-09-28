using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Davish.Result.AspNetCore.Http.Tests;

[Collection(ResultHttpOptionsCollection.Name)]
public class DefaultMinimalApiFailureHandlerTests
{
    private static readonly Error NotFoundError = new("Booking.NotFound", "Booking was not found", ErrorType.NotFound);
    private static readonly Error UnmappedError = new("Booking.Unmapped", "No mapping for this type", new ErrorType("SomethingElse"));

    private static Error ValidationErrorWithFields() =>
        new Error("Booking.Invalid", "Validation failed", ErrorType.Validation)
            .AddFieldError("Name", "Name is required")
            .AddFieldError("Date", "Date must be in the future");

    private static readonly DefaultMinimalApiFailureHandler Handler = new();

    public static IEnumerable<object[]> BuiltInErrorTypeStatusCodes =>
    [
        [ErrorType.Validation, StatusCodes.Status400BadRequest],
        [ErrorType.NullValue, StatusCodes.Status400BadRequest],
        [ErrorType.NotFound, StatusCodes.Status404NotFound],
        [ErrorType.BadRequest, StatusCodes.Status400BadRequest],
        [ErrorType.Unauthorized, StatusCodes.Status401Unauthorized],
        [ErrorType.Forbidden, StatusCodes.Status403Forbidden],
        [ErrorType.Conflict, StatusCodes.Status409Conflict],
        [ErrorType.TooManyRequests, StatusCodes.Status429TooManyRequests],
        [ErrorType.Unexpected, StatusCodes.Status500InternalServerError],
        [ErrorType.ServiceUnavailable, StatusCodes.Status503ServiceUnavailable]
    ];

    [Theory]
    [MemberData(nameof(BuiltInErrorTypeStatusCodes))]
    public async Task GivenFailedResultWithoutFields_WhenHandleAsync_ThenMapsStatusCodeByErrorType(ErrorType errorType, int expectedStatusCode)
    {
        var error = new Error("Some.Code", "Some description", errorType);
        var result = Result.Failure(error);

        var httpResult = await Handler.HandleAsync(result, CancellationToken.None);

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(expectedStatusCode, problem.ProblemDetails.Status);
        Assert.Equal(error.Code, problem.ProblemDetails.Title);
        Assert.Equal(error.Description, problem.ProblemDetails.Detail);
    }

    [Fact]
    public void GivenBuiltInErrorType_WhenToStatusCode_ThenReturnsTheMappedStatusCode()
    {
        var statusCode = ErrorType.NotFound.ToStatusCode();

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
    }

    [Fact]
    public async Task GivenFailedResultWithUnmappedErrorType_WhenHandleAsync_ThenDefaultsToInternalServerError()
    {
        var result = Result.Failure(UnmappedError);

        var httpResult = await Handler.HandleAsync(result, CancellationToken.None);

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.ProblemDetails.Status);
    }

    [Fact]
    public async Task GivenErrorTypeConstructedSeparatelyWithABuiltInName_WhenHandleAsync_ThenMapsAsTheSameBuiltInCategory()
    {
        // ErrorType is a record struct: a value built from the same Name is the same category by value,
        // even if it wasn't referenced via the ErrorType.NotFound static field.
        var notFoundByValue = new ErrorType(ErrorType.NotFound.Value);
        var result = Result.Failure(new Error("Booking.Lookalike", "Same name as NotFound", notFoundByValue));

        var httpResult = await Handler.HandleAsync(result, CancellationToken.None);

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, problem.ProblemDetails.Status);
    }

    [Fact]
    public async Task GivenConfigureWithCustomBuilder_WhenHandleAsync_ThenBuiltInDefaultsAndCustomEntryBothApply()
    {
        var rateLimited = new ErrorType("RateLimited.ConfigureBuilder");
        ResultHttpOptions.ResetForTesting();
        try
        {
            ResultHttpOptions.Configure(v =>
                v.CustomMap = new Dictionary<ErrorType, int> { [rateLimited] = StatusCodes.Status429TooManyRequests });

            var custom = Result.Failure(new Error("Booking.RateLimited", "Too many requests", rateLimited));
            var builtIn = Result.Failure(NotFoundError);

            var customProblem = Assert.IsType<ProblemHttpResult>(await Handler.HandleAsync(custom, CancellationToken.None));
            var builtInProblem = Assert.IsType<ProblemHttpResult>(await Handler.HandleAsync(builtIn, CancellationToken.None));

            Assert.Equal(StatusCodes.Status429TooManyRequests, customProblem.ProblemDetails.Status);
            Assert.Equal(StatusCodes.Status404NotFound, builtInProblem.ProblemDetails.Status);
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public async Task GivenConfigureWithUseDefaultFalseAndCustomMap_WhenHandleAsync_ThenOnlyCustomMapApplies()
    {
        var rateLimited = new ErrorType("RateLimited.CustomMap");
        ResultHttpOptions.ResetForTesting();
        try
        {
            ResultHttpOptions.Configure(v =>
            {
                v.UseDefault = false;
                v.CustomMap = new Dictionary<ErrorType, int> { [rateLimited] = StatusCodes.Status429TooManyRequests };
            });

            var custom = Result.Failure(new Error("Booking.RateLimited", "Too many requests", rateLimited));
            var builtIn = Result.Failure(NotFoundError);

            var customProblem = Assert.IsType<ProblemHttpResult>(await Handler.HandleAsync(custom, CancellationToken.None));
            var builtInProblem = Assert.IsType<ProblemHttpResult>(await Handler.HandleAsync(builtIn, CancellationToken.None));

            Assert.Equal(StatusCodes.Status429TooManyRequests, customProblem.ProblemDetails.Status);
            Assert.Equal(StatusCodes.Status500InternalServerError, builtInProblem.ProblemDetails.Status);
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public async Task GivenConfigureCalledAfterResolveStatusCodeHasRun_WhenConfigure_ThenThrows()
    {
        ResultHttpOptions.ResetForTesting();
        try
        {
            await Handler.HandleAsync(Result.Failure(NotFoundError), CancellationToken.None);

            Assert.Throws<ResultHttpOptionsLockedException>(() =>
                ResultHttpOptions.Configure(v => { }));
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public async Task GivenFailedResultWithFields_WhenHandleAsync_ThenReturnsValidationProblemWithFieldErrors()
    {
        var error = ValidationErrorWithFields();
        var result = Result.Failure(error);

        var httpResult = await Handler.HandleAsync(result, CancellationToken.None);

        var validationProblem = Assert.IsType<ValidationProblem>(httpResult);
        Assert.Equal(error.Code, validationProblem.ProblemDetails.Title);
        Assert.Equal(error.Description, validationProblem.ProblemDetails.Detail);
        Assert.Equal(new[] { "Name is required" }, validationProblem.ProblemDetails.Errors["Name"]);
        Assert.Equal(new[] { "Date must be in the future" }, validationProblem.ProblemDetails.Errors["Date"]);
    }
}
