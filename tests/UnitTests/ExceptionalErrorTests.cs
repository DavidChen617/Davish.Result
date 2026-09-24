using Davish.Result;

namespace UnitTests;

public class ExceptionalErrorTests
{
    [Fact]
    public void GivenException_WhenFrom_ThenCodeIsExceptionTypeName()
    {
        var exception = new InvalidOperationException("boom");

        var error = ExceptionalError.From(exception);

        Assert.Equal(nameof(InvalidOperationException), error.Code);
    }

    [Fact]
    public void GivenException_WhenFrom_ThenDescriptionIsExceptionMessage()
    {
        var exception = new InvalidOperationException("boom");

        var error = ExceptionalError.From(exception);

        Assert.Equal("boom", error.Description);
    }

    [Fact]
    public void GivenException_WhenFrom_ThenTypeIsUnexpected()
    {
        var error = ExceptionalError.From(new InvalidOperationException("boom"));

        Assert.Equal(ErrorType.Unexpected, error.Type);
    }

    [Fact]
    public void GivenException_WhenFrom_ThenExceptionPropertyIsTheOriginalInstance()
    {
        var exception = new InvalidOperationException("boom");

        var error = ExceptionalError.From(exception);

        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void GivenExceptionWithoutInnerException_WhenFrom_ThenCausesIsEmpty()
    {
        var error = ExceptionalError.From(new InvalidOperationException("boom"));

        Assert.Empty(error.Causes);
    }

    [Fact]
    public void GivenExceptionWithInnerException_WhenFrom_ThenInnerErrorIsExceptionalErrorWrappingIt()
    {
        var inner = new ArgumentException("bad arg");
        var exception = new InvalidOperationException("boom", inner);

        var error = ExceptionalError.From(exception);

        var innerError = Assert.IsType<ExceptionalError>(error.InnerError);
        Assert.Same(inner, innerError.Exception);
    }

    [Fact]
    public void GivenExceptionWithNestedInnerExceptions_WhenFrom_ThenGetRootCauseReturnsDeepestExceptionalError()
    {
        var root = new ArgumentException("root cause");
        var middle = new InvalidOperationException("middle", root);
        var outer = new InvalidOperationException("outer", middle);

        var error = ExceptionalError.From(outer);

        var rootError = Assert.IsType<ExceptionalError>(error.GetRootCause());
        Assert.Same(root, rootError.Exception);
    }

    [Fact]
    public void GivenTwoExceptionalErrorsWrappingSameExceptionInstance_WhenComparedForEquality_ThenTheyAreEqual()
    {
        var exception = new InvalidOperationException("boom");

        var a = ExceptionalError.From(exception);
        var b = ExceptionalError.From(exception);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GivenTwoExceptionalErrorsWrappingDifferentExceptionInstancesWithSameMessage_WhenComparedForEquality_ThenTheyAreNotEqual()
    {
        var a = ExceptionalError.From(new InvalidOperationException("boom"));
        var b = ExceptionalError.From(new InvalidOperationException("boom"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GivenExceptionalError_WhenComparedToPlainErrorWithSameCodeAndDescription_ThenTheyAreNotEqual()
    {
        var exceptionalError = ExceptionalError.From(new InvalidOperationException("boom"));
        var plainError = new Error(nameof(InvalidOperationException), "boom", ErrorType.Unexpected);

        Assert.NotEqual(exceptionalError, plainError);
    }

    [Fact]
    public void GivenExceptionAndError_WhenFromTwoArg_ThenReturnedErrorCarriesTheGivenErrorsFields()
    {
        var exception = new InvalidOperationException("internal sql detail");
        var safeError = new Error("Order.LoadFailed", "Could not load the order", ErrorType.Unexpected);

        var error = ExceptionalError.From(exception, safeError);

        Assert.Equal("Order.LoadFailed", error.Code);
        Assert.Equal("Could not load the order", error.Description);
        Assert.Equal(ErrorType.Unexpected, error.Type);
    }

    [Fact]
    public void GivenExceptionAndError_WhenFromTwoArg_ThenReturnedErrorIsNotExceptionalError()
    {
        var exception = new InvalidOperationException("internal sql detail");
        var safeError = new Error("Order.LoadFailed", "Could not load the order");

        var error = ExceptionalError.From(exception, safeError);

        Assert.IsNotType<ExceptionalError>(error);
    }

    [Fact]
    public void GivenExceptionAndError_WhenFromTwoArg_ThenInnerErrorIsExceptionalErrorWrappingTheException()
    {
        var exception = new InvalidOperationException("internal sql detail");
        var safeError = new Error("Order.LoadFailed", "Could not load the order");

        var error = ExceptionalError.From(exception, safeError);

        var innerError = Assert.IsType<ExceptionalError>(error.InnerError);
        Assert.Same(exception, innerError.Exception);
        Assert.Equal("internal sql detail", innerError.Description);
    }
}
