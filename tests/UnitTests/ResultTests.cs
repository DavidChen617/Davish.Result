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
    public void GivenHandBuiltNoneTypedError_WhenFailure_ThenThrows()
    {
        var handCraftedNone = new Error(string.Empty, string.Empty, ErrorType.None);

        Assert.Throws<InvalidResultStateException>(() => Result.Failure(handCraftedNone));
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

        Assert.Equal(notFound, ErrorType.NotFound);
        Assert.True(notFound == ErrorType.NotFound);
    }

    [Fact]
    public void GivenErrorWithoutType_WhenCreated_ThenTypeDefaultsToValidation()
    {
        var error = new Error("Some.Code", "Some description");

        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public void GivenErrorWithType_WhenCreated_ThenKeepsTheType()
    {
        var error = new Error("User.NotFound", "User was not found", ErrorType.NotFound);

        Assert.Equal(ErrorType.NotFound, error.Type);
    }

    [Fact]
    public void GivenException_WhenFailure_ThenErrorIsExceptionalErrorWithExceptionMessageAsDescription()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.Failure(exception);

        Assert.False(result.IsSuccess);
        var error = Assert.IsType<ExceptionalError>(result.Error);
        Assert.Equal("boom", error.Description);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void GivenExceptionAndError_WhenFailure_ThenResultErrorIsTheGivenErrorNotExceptionalError()
    {
        var exception = new InvalidOperationException("internal detail");
        var safeError = new Error("Order.LoadFailed", "Could not load the order");

        var result = Result.Failure(exception, safeError);

        Assert.False(result.IsSuccess);
        Assert.IsNotType<ExceptionalError>(result.Error);
        Assert.Equal("Order.LoadFailed", result.Error.Code);
        Assert.Equal("Could not load the order", result.Error.Description);
    }

    [Fact]
    public void GivenExceptionAndError_WhenFailure_ThenInnerErrorIsExceptionalErrorWrappingTheException()
    {
        var exception = new InvalidOperationException("internal detail");
        var safeError = new Error("Order.LoadFailed", "Could not load the order");

        var result = Result.Failure(exception, safeError);

        var innerError = Assert.IsType<ExceptionalError>(result.Error.InnerError);
        Assert.Same(exception, innerError.Exception);
    }

    [Fact]
    public void GivenException_WhenFailureGeneric_ThenSameBehaviorAppliesToResultOfTValue()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.Failure<int>(exception);

        Assert.False(result.IsSuccess);
        var error = Assert.IsType<ExceptionalError>(result.Error);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void GivenExceptionAndError_WhenFailureGeneric_ThenSameBehaviorAppliesToResultOfTValue()
    {
        var exception = new InvalidOperationException("internal detail");
        var safeError = new Error("Order.LoadFailed", "Could not load the order");

        var result = Result.Failure<int>(exception, safeError);

        Assert.False(result.IsSuccess);
        Assert.IsNotType<ExceptionalError>(result.Error);
        var innerError = Assert.IsType<ExceptionalError>(result.Error.InnerError);
        Assert.Same(exception, innerError.Exception);
    }

    [Fact]
    public void GivenSuccessResult_WhenDeconstructed_ThenIsSuccessTrueAndErrorIsNone()
    {
        var (isSuccess, error) = Result.Success();

        Assert.True(isSuccess);
        Assert.Equal(Error.None, error);
    }

    [Fact]
    public void GivenFailedResult_WhenDeconstructed_ThenIsSuccessFalseAndErrorIsCarried()
    {
        var failureError = new Error("Some.Code", "Some description");

        var (isSuccess, error) = Result.Failure(failureError);

        Assert.False(isSuccess);
        Assert.Equal(failureError, error);
    }

    [Fact]
    public void GivenSuccessResultValue_WhenDeconstructed_ThenValueIsCarried()
    {
        var (isSuccess, value, error) = Result.Success(42);

        Assert.True(isSuccess);
        Assert.Equal(42, value);
        Assert.Equal(Error.None, error);
    }

    [Fact]
    public void GivenFailedResultValue_WhenDeconstructed_ThenValueIsDefaultAndDoesNotThrow()
    {
        var failureError = new Error("Some.Code", "Some description");

        var (isSuccess, value, error) = Result.Failure<int>(failureError);

        Assert.False(isSuccess);
        Assert.Equal(default, value);
        Assert.Equal(failureError, error);
    }
}
