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
}
