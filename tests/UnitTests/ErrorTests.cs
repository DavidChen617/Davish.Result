using Davish.Result;

namespace UnitTests;

public class ErrorTests
{
    [Fact]
    public void GivenNewError_WhenCreated_ThenFieldsIsEmpty()
    {
        var error = new Error("Some.Code", "Some description");

        Assert.Empty(error.Fields);
    }

    [Fact]
    public void GivenNewKey_WhenAddFieldError_ThenCreatesEntryWithTheMessage()
    {
        var error = new Error("Validation", "Invalid").AddFieldError("Name", "Required");

        Assert.Equal(["Required"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenSameKey_WhenAddFieldErrorMultipleTimes_ThenAccumulatesMessages()
    {
        var error = new Error("Validation", "Invalid")
            .AddFieldError("Name", "Required")
            .AddFieldError("Name", "Too short");

        Assert.Equal(["Required", "Too short"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenDifferentKeys_WhenAddFieldError_ThenKeepsThemSeparate()
    {
        var error = new Error("Validation", "Invalid")
            .AddFieldError("Name", "Required")
            .AddFieldError("Age", "Must be positive");

        Assert.Equal(["Required"], error.Fields["Name"]);
        Assert.Equal(["Must be positive"], error.Fields["Age"]);
    }

    [Fact]
    public void GivenCollection_WhenAddFieldError_ThenAddsAllMessages()
    {
        var error = new Error("Validation", "Invalid").AddFieldError("Name", new[] { "Required", "Too short" });

        Assert.Equal(["Required", "Too short"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenExistingKey_WhenAddFieldErrorCollection_ThenAppendsToExisting()
    {
        var error = new Error("Validation", "Invalid")
            .AddFieldError("Name", "Required")
            .AddFieldError("Name", new[] { "Too short", "Invalid chars" });

        Assert.Equal(["Required", "Too short", "Invalid chars"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenEmptyMessageCollection_WhenAddFieldError_ThenFieldKeyIsCreatedLikeTheSingleMessageOverload()
    {
        var error = new Error("Validation", "Invalid").AddFieldError("Name", Array.Empty<string>());

        Assert.True(error.Fields.ContainsKey("Name"));
        Assert.Empty(error.Fields["Name"]);
    }

    [Fact]
    public void GivenError_WhenAddFieldError_ThenReturnsANewInstanceAndLeavesTheOriginalUnchanged()
    {
        var error = new Error("Validation", "Invalid");

        var chained = error
            .AddFieldError("Name", "Required")
            .AddFieldError("Age", "Must be positive");

        Assert.NotSame(error, chained);
        Assert.Empty(error.Fields);
        Assert.Equal(2, chained.Fields.Count);
    }

    [Fact]
    public void GivenSharedSingleton_WhenAddFieldErrorCalledFromUnrelatedCode_ThenTheSingletonIsUnaffected()
    {
        _ = Error.NullValue.AddFieldError("SomeField", "polluted");

        Assert.Empty(Error.NullValue.Fields);
    }

    [Fact]
    public void GivenFieldsDictionary_WhenConstructed_ThenUsesProvidedFields()
    {
        var fields = new Dictionary<string, List<string>> { ["Name"] = ["Required"], };

        var error = new Error("Validation", "Invalid", fields);

        Assert.Equal(["Required"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenNullFieldsDictionary_WhenConstructingError_ThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Error("V", "bad", (Dictionary<string, List<string>>)null!));
    }

    private static class CustomErrorType
    {
        public static readonly ErrorType Conflict = new(nameof(Conflict));
        public static readonly ErrorType NotFoundLookalike = new("NotFound");
    }

    [Fact]
    public void GivenBuiltInErrorType_WhenReadingName_ThenReturnsIt()
    {
        Assert.Equal("NotFound", ErrorType.NotFound.Value);
    }

    [Fact]
    public void GivenCustomErrorType_WhenCreated_ThenHasTheGivenName()
    {
        Assert.Equal("Conflict", CustomErrorType.Conflict.Value);
    }

    [Fact]
    public void GivenCustomErrorType_WhenUsedInError_ThenErrorCarriesIt()
    {
        var error = new Error("Some.Code", "Some description", CustomErrorType.Conflict);

        Assert.Equal(CustomErrorType.Conflict, error.Type);
    }

    [Fact]
    public void GivenCustomErrorTypeWithBuiltInName_WhenCompared_ThenEqualByValue()
    {
        // ErrorType is a record struct: two values with the same Name are the same category,
        // regardless of which static field they came from.
        Assert.Equal(ErrorType.NotFound, CustomErrorType.NotFoundLookalike);
    }

    [Fact]
    public void GivenTwoStructurallyIdenticalErrors_WhenComparedForEquality_ThenTheyAreEqual()
    {
        var a = new Error("X.1", "same description");
        var b = new Error("X.1", "same description");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GivenTwoErrorsWithTheSameFieldsInDifferentInstances_WhenComparedForEquality_ThenTheyAreEqual()
    {
        var a = new Error("V", "bad").AddFieldError("Name", "Required");
        var b = new Error("V", "bad").AddFieldError("Name", "Required");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GivenErrorsWithDifferentFieldMessages_WhenComparedForEquality_ThenTheyAreNotEqual()
    {
        var a = new Error("V", "bad").AddFieldError("Name", "Required");
        var b = new Error("V", "bad").AddFieldError("Name", "Too short");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GivenNewError_WhenCreated_ThenCausesIsEmptyAndInnerErrorIsNull()
    {
        var error = new Error("Some.Code", "Some description");

        Assert.Empty(error.Causes);
        Assert.Null(error.InnerError);
    }

    [Fact]
    public void GivenSingleCause_WhenCausedBy_ThenCausesContainsItAndInnerErrorIsIt()
    {
        var cause = new Error("Db.Timeout", "Query timed out");

        var error = new Error("Order.LoadFailed", "Could not load order").CausedBy(cause);

        Assert.Equal([cause], error.Causes);
        Assert.Equal(cause, error.InnerError);
    }

    [Fact]
    public void GivenMultipleCauses_WhenCausedBy_ThenInnerErrorIsTheFirstOne()
    {
        var stockError = new Error("Stock.Insufficient", "Not enough stock");
        var paymentError = new Error("Payment.Declined", "Payment declined");

        var error = new Error("Checkout.Failed", "Checkout failed").CausedBy([stockError, paymentError]);

        Assert.Equal(stockError, error.InnerError);
    }

    [Fact]
    public void GivenMultipleCauses_WhenCausedBy_ThenCausesKeepsInsertionOrder()
    {
        var stockError = new Error("Stock.Insufficient", "Not enough stock");
        var paymentError = new Error("Payment.Declined", "Payment declined");

        var error = new Error("Checkout.Failed", "Checkout failed").CausedBy([stockError, paymentError]);

        Assert.Equal([stockError, paymentError], error.Causes);
    }

    [Fact]
    public void GivenError_WhenCausedByCalledMultipleTimes_ThenAccumulatesCauses()
    {
        var first = new Error("A", "a");
        var second = new Error("B", "b");

        var error = new Error("C", "c").CausedBy(first).CausedBy(second);

        Assert.Equal([first, second], error.Causes);
    }

    [Fact]
    public void GivenError_WhenCausedBy_ThenReturnsANewInstanceAndLeavesTheOriginalUnchanged()
    {
        var error = new Error("C", "c");

        var chained = error.CausedBy(new Error("A", "a"));

        Assert.NotSame(error, chained);
        Assert.Empty(error.Causes);
        Assert.Single(chained.Causes);
    }

    [Fact]
    public void GivenSharedSingleton_WhenCausedByCalledFromUnrelatedCode_ThenTheSingletonIsUnaffected()
    {
        _ = Error.NullValue.CausedBy(new Error("A", "a"));

        Assert.Empty(Error.NullValue.Causes);
    }

    [Fact]
    public void GivenEmptyCollection_WhenCausedBy_ThenCausesUnchanged()
    {
        var error = new Error("C", "c").CausedBy(new Error("A", "a"));

        var unchanged = error.CausedBy(Array.Empty<Error>());

        Assert.Equal(error.Causes, unchanged.Causes);
    }

    [Fact]
    public void GivenErrorWithNoCauses_WhenGetRootCause_ThenReturnsItself()
    {
        var error = new Error("A", "a");

        Assert.Equal(error, error.GetRootCause());
    }

    [Fact]
    public void GivenChainOfSingleCauses_WhenGetRootCause_ThenReturnsTheDeepestError()
    {
        var dbError = new Error("Db.Timeout", "Query timed out");
        var orderError = new Error("Order.LoadFailed", "Could not load order").CausedBy(dbError);
        var apiError = new Error("Api.RequestFailed", "Request failed").CausedBy(orderError);

        Assert.Equal(dbError, apiError.GetRootCause());
    }

    [Fact]
    public void GivenAggregationPoint_WhenGetRootCause_ThenStopsThereEvenIfNestedFurther()
    {
        var stockError = new Error("Stock.Insufficient", "Not enough stock")
            .CausedBy(new Error("Warehouse.Unreachable", "n/a"));
        var paymentError = new Error("Payment.Declined", "Payment declined");
        var checkoutError = new Error("Checkout.Failed", "Checkout failed").CausedBy([stockError, paymentError]);

        Assert.Equal(checkoutError, checkoutError.GetRootCause());
    }

    [Fact]
    public void GivenTwoErrorsWithTheSameCauses_WhenComparedForEquality_ThenTheyAreEqual()
    {
        var a = new Error("C", "c").CausedBy(new Error("A", "a"));
        var b = new Error("C", "c").CausedBy(new Error("A", "a"));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GivenTwoErrorsWithDifferentCauses_WhenComparedForEquality_ThenTheyAreNotEqual()
    {
        var a = new Error("C", "c").CausedBy(new Error("A", "a"));
        var b = new Error("C", "c").CausedBy(new Error("B", "b"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GivenTwoErrorsWithSameCausesInDifferentOrder_WhenComparedForEquality_ThenTheyAreNotEqual()
    {
        var a = new Error("C", "c").CausedBy([new Error("A", "a"), new Error("B", "b")]);
        var b = new Error("C", "c").CausedBy([new Error("B", "b"), new Error("A", "a")]);

        Assert.NotEqual(a, b);
    }
}
