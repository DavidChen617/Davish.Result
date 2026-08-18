using Davish.Result;

namespace UnitTests;

public class ResultTests
{
    [Fact]
    public void GivenSuccess_WhenCreated_ThenIsSuccessfulWithNoneError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void GivenError_WhenFailure_ThenCarriesTheError()
    {
        var error = new Error("Some.Code", "Some description");

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GivenNoneError_WhenFailure_ThenThrows()
    {
        Assert.Throws<InvalidResultStateException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void GivenNoneError_WhenFailure_ThenExceptionCarriesTheAttemptedState()
    {
        var exception = Assert.Throws<InvalidResultStateException>(() => Result.Failure(Error.None));

        Assert.False(exception.IsSuccess);
        Assert.Equal(Error.None, exception.Error);
    }

    [Fact]
    public void GivenNoneError_WhenFailure_ThenThrowsIsCatchableAsResultException()
    {
        Assert.ThrowsAny<ResultException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void GivenError_WhenImplicitlyConverted_ThenReturnsFailedResult()
    {
        var error = new Error("Some.Code", "Some description");

        Result result = error;

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GivenValue_WhenSuccess_ThenExposesTheValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GivenFailure_WhenReadingValue_ThenThrows()
    {
        var result = Result.Failure<int>(new Error("E", "boom"));

        Assert.False(result.IsSuccess);
        Assert.Throws<ResultValueUnavailableException>(() => result.Value);
    }

    [Fact]
    public void GivenFailure_WhenReadingValue_ThenExceptionCarriesTheError()
    {
        var error = new Error("E", "boom");
        var result = Result.Failure<int>(error);

        var exception = Assert.Throws<ResultValueUnavailableException>(() => result.Value);

        Assert.Equal(error, exception.Error);
    }

    [Fact]
    public void GivenFailure_WhenReadingValue_ThenThrowsIsCatchableAsResultException()
    {
        var result = Result.Failure<int>(new Error("E", "boom"));

        Assert.ThrowsAny<ResultException>(() => result.Value);
    }

    [Fact]
    public void GivenNonNullValue_WhenImplicitlyConverted_ThenReturnsSuccess()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void GivenNullValue_WhenImplicitlyConverted_ThenReturnsNullValueFailure()
    {
        string? value = null;
        Result<string> result = value;

        Assert.False(result.IsSuccess);
        Assert.Equal(Error.NullValue, result.Error);
    }

    [Fact]
    public void GivenError_WhenImplicitlyConvertedToGenericResult_ThenReturnsFailure()
    {
        var error = new Error("Some.Code", "Some description");

        Result<int> result = error;

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GivenTwoDifferentErrorTypes_WhenCompared_ThenAreNotEqual()
    {
        Assert.NotEqual(ErrorType.BadRequest, ErrorType.Validation);
        Assert.False(ErrorType.BadRequest == ErrorType.Validation);
    }

    [Fact]
    public void GivenSameErrorType_WhenCompared_ThenAreEqual()
    {
        var notFound = ErrorType.NotFound;

        Assert.Same(notFound, ErrorType.NotFound);
        Assert.True(notFound == ErrorType.NotFound);
    }

    [Fact]
    public void GivenErrorWithoutType_WhenCreated_ThenTypeDefaultsToValidation()
    {
        var error = new Error("Some.Code", "Some description");

        Assert.Same(ErrorType.Validation, error.Type);
    }

    [Fact]
    public void GivenErrorWithType_WhenCreated_ThenKeepsTheType()
    {
        var error = new Error("User.NotFound", "User was not found", ErrorType.NotFound);

        Assert.Same(ErrorType.NotFound, error.Type);
    }
}
