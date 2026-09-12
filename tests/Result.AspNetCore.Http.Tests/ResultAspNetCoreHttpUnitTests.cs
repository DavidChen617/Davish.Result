using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Davish.Result.AspNetCore.Http.Tests;

[Collection(ResultHttpOptionsCollection.Name)]
public class ResultAspNetCoreHttpUnitTests
{
    private static readonly Error NotFoundError = new("Booking.NotFound", "Booking was not found", ErrorType.NotFound);
    private static readonly Error UnexpectedError = new("Booking.Unexpected", "Something went wrong", ErrorType.Unexpected);
    private static readonly Error UnmappedError = new("Booking.Unmapped", "No mapping for this type", new ErrorType("SomethingElse"));

    private static Error ValidationErrorWithFields()
    {
        var error = new Error("Booking.Invalid", "Validation failed", ErrorType.Validation);
        error.AddFieldError("Name", "Name is required");
        error.AddFieldError("Date", "Date must be in the future");
        return error;
    }

    private sealed record Booking(int Id);

    [Fact]
    public void GivenSuccessResult_WhenToOk_ThenReturnsOk()
    {
        var result = Result.Success();

        var httpResult = result.ToOk();

        var ok = Assert.IsType<Ok>(httpResult);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public void GivenFailedResult_WhenToOk_ThenReturnsProblem()
    {
        var result = Result.Failure(NotFoundError);

        var httpResult = result.ToOk();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, problem.ProblemDetails.Status);
        Assert.Equal(NotFoundError.Code, problem.ProblemDetails.Title);
        Assert.Equal(NotFoundError.Description, problem.ProblemDetails.Detail);
    }

    [Fact]
    public void GivenSuccessResult_WhenToNoContent_ThenReturnsNoContent()
    {
        var result = Result.Success();

        var httpResult = result.ToNoContent();

        var noContent = Assert.IsType<NoContent>(httpResult);
        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
    }

    [Fact]
    public void GivenFailedResult_WhenToNoContent_ThenReturnsProblem()
    {
        var result = Result.Failure(UnexpectedError);

        var httpResult = result.ToNoContent();

        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    [Fact]
    public void GivenSuccessResult_WhenToCreated_ThenReturnsCreatedAtRoute()
    {
        var result = Result.Success();

        var httpResult = result.ToCreated("GetBooking", new { id = 1 });

        var created = Assert.IsType<CreatedAtRoute>(httpResult);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("GetBooking", created.RouteName);
    }

    [Fact]
    public void GivenFailedResult_WhenToCreated_ThenReturnsProblem()
    {
        var result = Result.Failure(NotFoundError);

        var httpResult = result.ToCreated("GetBooking");

        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    [Fact]
    public void GivenSuccessResult_WhenToAccepted_ThenReturnsAccepted()
    {
        var result = Result.Success();

        var httpResult = result.ToAccepted("/bookings/1/status");

        var accepted = Assert.IsType<Accepted>(httpResult);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
        Assert.Equal("/bookings/1/status", accepted.Location);
    }

    [Fact]
    public void GivenFailedResult_WhenToAccepted_ThenReturnsProblem()
    {
        var result = Result.Failure(NotFoundError);

        var httpResult = result.ToAccepted();

        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    public static IEnumerable<object[]> BuiltInErrorTypeStatusCodes =>
    [
        [ErrorType.Validation, StatusCodes.Status400BadRequest],
        [ErrorType.NullValue, StatusCodes.Status400BadRequest],
        [ErrorType.NotFound, StatusCodes.Status404NotFound],
        [ErrorType.BadRequest, StatusCodes.Status400BadRequest],
        [ErrorType.Unauthorized, StatusCodes.Status401Unauthorized],
        [ErrorType.Forbidden, StatusCodes.Status403Forbidden],
        [ErrorType.Conflict, StatusCodes.Status409Conflict],
        [ErrorType.Unexpected, StatusCodes.Status500InternalServerError],
        [ErrorType.ServiceUnavailable, StatusCodes.Status503ServiceUnavailable]
    ];

    [Theory]
    [MemberData(nameof(BuiltInErrorTypeStatusCodes))]
    public void GivenFailedResultWithoutFields_WhenToProblemDetail_ThenMapsStatusCodeByErrorType(ErrorType errorType, int expectedStatusCode)
    {
        var error = new Error("Some.Code", "Some description", errorType);
        var result = Result.Failure(error);

        var httpResult = result.ToProblemDetail();

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
    public void GivenFailedResultWithUnmappedErrorType_WhenToProblemDetail_ThenDefaultsToInternalServerError()
    {
        var result = Result.Failure(UnmappedError);

        var httpResult = result.ToProblemDetail();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.ProblemDetails.Status);
    }

    [Fact]
    public void GivenErrorTypeConstructedSeparatelyWithABuiltInName_WhenToProblemDetail_ThenMapsAsTheSameBuiltInCategory()
    {
        // ErrorType is a record struct: a value built from the same Name is the same category by value,
        // even if it wasn't referenced via the ErrorType.NotFound static field.
        var notFoundByValue = new ErrorType(ErrorType.NotFound.Value);
        var result = Result.Failure(new Error("Booking.Lookalike", "Same name as NotFound", notFoundByValue));

        var httpResult = result.ToProblemDetail();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, problem.ProblemDetails.Status);
    }

    [Fact]
    public void GivenConfigureWithCustomBuilder_WhenToProblemDetail_ThenBuiltInDefaultsAndCustomEntryBothApply()
    {
        var rateLimited = new ErrorType("RateLimited.ConfigureBuilder");
        ResultHttpOptions.ResetForTesting();
        try
        {
            ResultHttpOptions.Configure(v =>
                v.CustomMap = new Dictionary<ErrorType, int> { [rateLimited] = StatusCodes.Status429TooManyRequests });

            var custom = Result.Failure(new Error("Booking.RateLimited", "Too many requests", rateLimited));
            var builtIn = Result.Failure(NotFoundError);

            var customProblem = Assert.IsType<ProblemHttpResult>(custom.ToProblemDetail());
            var builtInProblem = Assert.IsType<ProblemHttpResult>(builtIn.ToProblemDetail());

            Assert.Equal(StatusCodes.Status429TooManyRequests, customProblem.ProblemDetails.Status);
            Assert.Equal(StatusCodes.Status404NotFound, builtInProblem.ProblemDetails.Status);
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public void GivenConfigureWithCustomMap_WhenToProblemDetail_ThenUsesConfiguredStatusCode()
    {
        var rateLimited = new ErrorType("RateLimited.DirectConfigure");
        ResultHttpOptions.ResetForTesting();

        try
        {
            ResultHttpOptions.Configure(v =>
                v.CustomMap = new Dictionary<ErrorType, int> { [rateLimited] = StatusCodes.Status429TooManyRequests });

            var result = Result.Failure(new Error("Booking.RateLimited", "Too many requests", rateLimited));

            var problem = Assert.IsType<ProblemHttpResult>(result.ToProblemDetail());
            Assert.Equal(StatusCodes.Status429TooManyRequests, problem.ProblemDetails.Status);
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public void GivenNoConfigureCall_WhenToProblemDetail_ThenBuiltInDefaultsApply()
    {
        ResultHttpOptions.ResetForTesting();

        try
        {
            var problem = Assert.IsType<ProblemHttpResult>(Result.Failure(NotFoundError).ToProblemDetail());
            Assert.Equal(StatusCodes.Status404NotFound, problem.ProblemDetails.Status);
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public void GivenConfigureWithUseDefaultFalseAndCustomMap_WhenToProblemDetail_ThenOnlyCustomMapApplies()
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

            var customProblem = Assert.IsType<ProblemHttpResult>(custom.ToProblemDetail());
            var builtInProblem = Assert.IsType<ProblemHttpResult>(builtIn.ToProblemDetail());

            Assert.Equal(StatusCodes.Status429TooManyRequests, customProblem.ProblemDetails.Status);
            Assert.Equal(StatusCodes.Status500InternalServerError, builtInProblem.ProblemDetails.Status);
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public void GivenConfigureCalledAfterResolveStatusCodeHasRun_WhenConfigure_ThenThrows()
    {
        ResultHttpOptions.ResetForTesting();
        try
        {
            Result.Failure(NotFoundError).ToProblemDetail();

            Assert.Throws<ResultHttpOptionsLockedException>(() =>
                ResultHttpOptions.Configure(v => { }));
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public void GivenConfigureCalledAfterResolveStatusCodeHasRun_WhenConfigure_ThenThrowIsCatchableAsResultException()
    {
        ResultHttpOptions.ResetForTesting();
        try
        {
            Result.Failure(NotFoundError).ToProblemDetail();

            Assert.ThrowsAny<ResultException>(() =>
                ResultHttpOptions.Configure(v => { }));
        }
        finally
        {
            ResultHttpOptions.ResetForTesting();
        }
    }

    [Fact]
    public void GivenFailedResultWithFields_WhenToProblemDetail_ThenDelegatesToValidationProblem()
    {
        var result = Result.Failure(ValidationErrorWithFields());

        var httpResult = result.ToProblemDetail();

        Assert.IsType<ValidationProblem>(httpResult);
    }

    [Fact]
    public void GivenFailedResultWithFields_WhenToValidationProblemDetail_ThenReturnsValidationProblemWithFieldErrors()
    {
        var error = ValidationErrorWithFields();
        var result = Result.Failure(error);

        var httpResult = result.ToValidationProblemDetail();

        var validationProblem = Assert.IsType<ValidationProblem>(httpResult);
        Assert.Equal(error.Code, validationProblem.ProblemDetails.Title);
        Assert.Equal(error.Description, validationProblem.ProblemDetails.Detail);
        Assert.Equal(new[] { "Name is required" }, validationProblem.ProblemDetails.Errors["Name"]);
        Assert.Equal(new[] { "Date must be in the future" }, validationProblem.ProblemDetails.Errors["Date"]);
    }

    [Fact]
    public void GivenSuccessResultValue_WhenToOk_ThenReturnsOkWithValue()
    {
        var result = Result.Success(42);

        var httpResult = result.ToOk();

        var ok = Assert.IsType<Ok<int>>(httpResult);
        Assert.Equal(42, ok.Value);
    }

    [Fact]
    public void GivenFailedResultValue_WhenToOk_ThenReturnsProblem()
    {
        var result = Result.Failure<int>(NotFoundError);

        var httpResult = result.ToOk();

        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    [Fact]
    public void GivenSuccessResultValue_WhenToCreated_ThenReturnsCreatedAtRouteWithValueAndRouteValues()
    {
        var result = Result.Success(42);

        var httpResult = result.ToCreated("GetBooking", value => new { id = value });

        var created = Assert.IsType<CreatedAtRoute<int>>(httpResult);
        Assert.Equal(42, created.Value);
        Assert.Equal("GetBooking", created.RouteName);
    }

    [Fact]
    public void GivenSuccessResultValue_WhenToCreatedWithoutRouteValues_ThenRouteValuesIsEmpty()
    {
        var result = Result.Success(42);

        var httpResult = result.ToCreated("GetBooking");

        var created = Assert.IsType<CreatedAtRoute<int>>(httpResult);
        Assert.Empty(created.RouteValues);
    }

    [Fact]
    public void GivenFailedResultValue_WhenToCreated_ThenReturnsProblem()
    {
        var result = Result.Failure<int>(NotFoundError);

        var httpResult = result.ToCreated("GetBooking");

        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    [Fact]
    public void GivenFailedResultValue_WhenToCreatedWithRouteValuesFactory_ThenFactoryIsNotInvoked()
    {
        var result = Result.Failure<Booking>(NotFoundError);
        var factoryInvoked = false;

        var httpResult = result.ToCreated("GetBooking", value =>
        {
            factoryInvoked = true;
            return new { value.Id };
        });

        Assert.False(factoryInvoked);
        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    [Fact]
    public void GivenSuccessResultValue_WhenToAccepted_ThenReturnsAcceptedWithValue()
    {
        var result = Result.Success(42);

        var httpResult = result.ToAccepted("/bookings/42/status");

        var accepted = Assert.IsType<Accepted<int>>(httpResult);
        Assert.Equal(42, accepted.Value);
        Assert.Equal("/bookings/42/status", accepted.Location);
    }

    [Fact]
    public void GivenFailedResultValue_WhenToAccepted_ThenReturnsProblem()
    {
        var result = Result.Failure<int>(NotFoundError);

        var httpResult = result.ToAccepted();

        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    [Fact]
    public void GivenSuccessResultValue_WhenToNoContent_ThenReturnsNoContent()
    {
        var result = Result.Success(42);

        var httpResult = result.ToNoContent();

        Assert.IsType<NoContent>(httpResult);
    }

    [Fact]
    public void GivenFailedResultValue_WhenToNoContent_ThenReturnsProblem()
    {
        var result = Result.Failure<int>(NotFoundError);

        var httpResult = result.ToNoContent();

        Assert.IsType<ProblemHttpResult>(httpResult);
    }
}
