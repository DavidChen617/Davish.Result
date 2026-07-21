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
        var error = new Error("Validation", "Invalid");

        error.AddFieldError("Name", "Required");

        Assert.Equal(["Required"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenSameKey_WhenAddFieldErrorMultipleTimes_ThenAccumulatesMessages()
    {
        var error = new Error("Validation", "Invalid");

        error.AddFieldError("Name", "Required");
        error.AddFieldError("Name", "Too short");

        Assert.Equal(["Required", "Too short"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenDifferentKeys_WhenAddFieldError_ThenKeepsThemSeparate()
    {
        var error = new Error("Validation", "Invalid");

        error.AddFieldError("Name", "Required");
        error.AddFieldError("Age", "Must be positive");

        Assert.Equal(["Required"], error.Fields["Name"]);
        Assert.Equal(["Must be positive"], error.Fields["Age"]);
    }

    [Fact]
    public void GivenCollection_WhenAddFieldError_ThenAddsAllMessages()
    {
        var error = new Error("Validation", "Invalid");

        error.AddFieldError("Name", new[] { "Required", "Too short" });

        Assert.Equal(["Required", "Too short"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenExistingKey_WhenAddFieldErrorCollection_ThenAppendsToExisting()
    {
        var error = new Error("Validation", "Invalid");

        error.AddFieldError("Name", "Required");
        error.AddFieldError("Name", new[] { "Too short", "Invalid chars" });

        Assert.Equal(["Required", "Too short", "Invalid chars"], error.Fields["Name"]);
    }

    [Fact]
    public void GivenError_WhenAddFieldError_ThenReturnsSameInstanceForChaining()
    {
        var error = new Error("Validation", "Invalid");

        var chained = error
            .AddFieldError("Name", "Required")
            .AddFieldError("Age", "Must be positive");

        Assert.Same(error, chained);
        Assert.Equal(2, error.Fields.Count);
    }

    [Fact]
    public void GivenFieldsDictionary_WhenConstructed_ThenUsesProvidedFields()
    {
        var fields = new Dictionary<string, List<string>> { ["Name"] = ["Required"], };

        var error = new Error("Validation", "Invalid", fields);

        Assert.Equal(["Required"], error.Fields["Name"]);
    }

    private sealed class CustomErrorType : ErrorType
    {
        public static readonly CustomErrorType Conflict = new(nameof(Conflict));
        public static readonly CustomErrorType NotFoundLookalike = new("NotFound");

        private CustomErrorType(string name) : base(name)
        {
        }
    }

    [Fact]
    public void GivenBuiltInErrorType_WhenReadingName_ThenComesFromBase()
    {
        Assert.Equal("NotFound", ErrorType.NotFound.Name);
    }

    [Fact]
    public void GivenBuiltInErrorType_WhenChecked_ThenIsAssignableToBase()
    {
        Assert.IsAssignableFrom<ErrorTypeBase>(ErrorType.NotFound);
    }

    [Fact]
    public void GivenCustomErrorType_WhenCreated_ThenInheritsNameFromBase()
    {
        Assert.Equal("Conflict", CustomErrorType.Conflict.Name);
        Assert.IsAssignableFrom<ErrorType>(CustomErrorType.Conflict);
    }

    [Fact]
    public void GivenCustomErrorType_WhenUsedInError_ThenErrorCarriesIt()
    {
        var error = new Error("Some.Code", "Some description", CustomErrorType.Conflict);

        Assert.Same(CustomErrorType.Conflict, error.Type);
    }

    [Fact]
    public void GivenCustomErrorTypeWithBuiltInName_WhenCompared_ThenNotEqualByReference()
    {
        Assert.NotSame(ErrorType.NotFound, CustomErrorType.NotFoundLookalike);
        Assert.Equal("NotFound", CustomErrorType.NotFoundLookalike.Name);
    }
}
