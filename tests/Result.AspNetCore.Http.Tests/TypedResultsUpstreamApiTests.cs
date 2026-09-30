using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Davish.Result.AspNetCore.Http.Tests;

/// <summary>
/// Detects upstream changes to <see cref="TypedResults"/> (e.g. after bumping the target framework or SDK) so a new
/// or changed success shape is noticed instead of silently going unwrapped.
/// </summary>
/// <remarks>
/// <para>
/// This tests the <em>reviewed inventory</em> of <c>TypedResults</c>, not that every signature has a one-to-one
/// wrapper: a wrapper may deliberately take a different shape (see <c>ReviewedSignatures</c> below). What it
/// guarantees is that a human has looked at each upstream method and either wrapped it (name mapped to its
/// <c>ToXxx</c> extension in <see cref="WrappedMethods"/>) or excluded it (<see cref="ExcludedFailureShapes"/>).
/// </para>
/// <para>
/// When one of these tests fails after an upgrade, decide for the new member: wrap it in
/// <see cref="ResultToMinimalResultExtension"/> and add its signature to <c>ReviewedSignatures</c>, or exclude it here.
/// </para>
/// </remarks>
public class TypedResultsUpstreamApiTests
{
    /// <summary>
    /// Upstream methods that are wrapped, mapped to the extension method name(s) that wrap them. Known deliberate
    /// deviations from a one-to-one signature match:
    /// <c>CreatedAtRoute&lt;T&gt;(T, string, RouteValueDictionary)</c> has no overload (the generic <c>ToCreated</c>
    /// takes a <c>Func&lt;T, object?&gt;</c> that can return one; adding it would break <c>ToCreated("r", null)</c>);
    /// <c>Text(ReadOnlySpan&lt;byte&gt;, ...)</c> is wrapped with <c>ReadOnlyMemory&lt;byte&gt;</c> because a span cannot be captured;
    /// the paired <c>Text</c>/<c>Content</c> overloads with and without <c>statusCode</c> are merged into one with an optional parameter.
    /// </summary>
    private static readonly Dictionary<string, string[]> WrappedMethods = new()
    {
        ["Accepted"] = ["ToAccepted"],
        ["AcceptedAtRoute"] = ["ToAcceptedAtRoute"],
        ["Bytes"] = ["ToBytes"],
        ["Challenge"] = ["ToChallenge"],
        ["Content"] = ["ToContent"],
        ["Created"] = ["ToCreated", "ToCreatedAtLocation"],
        ["CreatedAtRoute"] = ["ToCreated"],
        ["File"] = ["ToFile"],
        ["Forbid"] = ["ToForbid"],
        ["Json"] = ["ToJson"],
        ["LocalRedirect"] = ["ToLocalRedirect"],
        ["NoContent"] = ["ToNoContent"],
        ["Ok"] = ["ToOk"],
        ["PhysicalFile"] = ["ToPhysicalFile"],
        ["Redirect"] = ["ToRedirect"],
        ["RedirectToRoute"] = ["ToRedirectToRoute"],
        ["ServerSentEvents"] = ["ToServerSentEvents"],
        ["SignIn"] = ["ToSignIn"],
        ["SignOut"] = ["ToSignOut"],
        ["StatusCode"] = ["ToStatus"],
        ["Stream"] = ["ToStream"],
        ["Text"] = ["ToText"],
        ["VirtualFile"] = ["ToVirtualFile"],
    };

    /// <summary>
    /// Upstream methods that are intentionally not wrapped: they express a failure, which is handled by
    /// <c>IMinimalApiFailureHandler</c> from the result's <see cref="ErrorType"/> instead.
    /// </summary>
    private static readonly HashSet<string> ExcludedFailureShapes =
    [
        "BadRequest",
        "Conflict",
        "InternalServerError",
        "NotFound",
        "Problem",
        "Unauthorized",
        "UnprocessableEntity",
        "ValidationProblem",
    ];

    /// <summary>
    /// Every signature of the wrapped methods that has been reviewed, as <c>Name&lt;TypeArgs&gt;(ParameterTypes)</c>
    /// (the same notation as the upstream API tables, parameter names omitted).
    /// </summary>
    private static readonly string[] ReviewedSignatures =
    [
        "Accepted(String)",
        "Accepted(Uri)",
        "Accepted<TValue>(String, TValue)",
        "Accepted<TValue>(Uri, TValue)",
        "AcceptedAtRoute(String, Object)",
        "AcceptedAtRoute(String, RouteValueDictionary)",
        "AcceptedAtRoute<TValue>(TValue, String, Object)",
        "AcceptedAtRoute<TValue>(TValue, String, RouteValueDictionary)",
        "Bytes(Byte[], String, String, Boolean, Nullable<DateTimeOffset>, EntityTagHeaderValue)",
        "Bytes(ReadOnlyMemory<Byte>, String, String, Boolean, Nullable<DateTimeOffset>, EntityTagHeaderValue)",
        "Challenge(AuthenticationProperties, IList<String>)",
        "Content(String, MediaTypeHeaderValue)",
        "Content(String, String, Encoding, Nullable<Int32>)",
        "Content(String, String, Encoding)",
        "Created()",
        "Created(String)",
        "Created(Uri)",
        "Created<TValue>(String, TValue)",
        "Created<TValue>(Uri, TValue)",
        "CreatedAtRoute(String, Object)",
        "CreatedAtRoute(String, RouteValueDictionary)",
        "CreatedAtRoute<TValue>(TValue, String, Object)",
        "CreatedAtRoute<TValue>(TValue, String, RouteValueDictionary)",
        "File(Byte[], String, String, Boolean, Nullable<DateTimeOffset>, EntityTagHeaderValue)",
        "File(Stream, String, String, Nullable<DateTimeOffset>, EntityTagHeaderValue, Boolean)",
        "Forbid(AuthenticationProperties, IList<String>)",
        "Json<TValue>(TValue, JsonSerializerContext, String, Nullable<Int32>)",
        "Json<TValue>(TValue, JsonSerializerOptions, String, Nullable<Int32>)",
        "Json<TValue>(TValue, JsonTypeInfo<TValue>, String, Nullable<Int32>)",
        "LocalRedirect(String, Boolean, Boolean)",
        "NoContent()",
        "Ok()",
        "Ok<TValue>(TValue)",
        "PhysicalFile(String, String, String, Nullable<DateTimeOffset>, EntityTagHeaderValue, Boolean)",
        "Redirect(String, Boolean, Boolean)",
        "RedirectToRoute(String, Object, Boolean, Boolean, String)",
        "RedirectToRoute(String, RouteValueDictionary, Boolean, Boolean, String)",
        "ServerSentEvents(IAsyncEnumerable<String>, String)",
        "ServerSentEvents<T>(IAsyncEnumerable<SseItem<T>>)",
        "ServerSentEvents<T>(IAsyncEnumerable<T>, String)",
        "SignIn(ClaimsPrincipal, AuthenticationProperties, String)",
        "SignOut(AuthenticationProperties, IList<String>)",
        "StatusCode(Int32)",
        "Stream(Func<Stream, Task>, String, String, Nullable<DateTimeOffset>, EntityTagHeaderValue)",
        "Stream(PipeReader, String, String, Nullable<DateTimeOffset>, EntityTagHeaderValue, Boolean)",
        "Stream(Stream, String, String, Nullable<DateTimeOffset>, EntityTagHeaderValue, Boolean)",
        "Text(ReadOnlySpan<Byte>, String, Nullable<Int32>)",
        "Text(String, String, Encoding, Nullable<Int32>)",
        "Text(String, String, Encoding)",
        "VirtualFile(String, String, String, Nullable<DateTimeOffset>, EntityTagHeaderValue, Boolean)",
    ];

    private static string TypeName(Type type) =>
        type.IsGenericParameter ? type.Name
        : type.IsGenericType
            ? type.Name[..type.Name.IndexOf('`')] + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">"
        : type.Name;

    private static string Signature(MethodInfo method) =>
        method.Name
        + (method.IsGenericMethod ? "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">" : "")
        + "(" + string.Join(", ", method.GetParameters().Select(p => TypeName(p.ParameterType))) + ")";

    /// <summary>
    /// The public static methods declared on <see cref="TypedResults"/>. Property accessors are skipped: <c>Empty</c>
    /// and <c>Extensions</c> are not response shapes.
    /// </summary>
    private static MethodInfo[] UpstreamMethods { get; } =
        [.. typeof(TypedResults)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)];

    [Fact]
    public void EveryUpstreamMethodNameHasBeenReviewed()
    {
        var unreviewed = UpstreamMethods
            .Select(m => m.Name)
            .Distinct()
            .Where(name => !WrappedMethods.ContainsKey(name) && !ExcludedFailureShapes.Contains(name))
            .Order()
            .ToArray();

        Assert.True(
            unreviewed.Length == 0,
            "TypedResults has new method(s) that have not been reviewed: " + string.Join(", ", unreviewed) +
            ". Wrap each in ResultToMinimalResultExtension and add it to WrappedMethods/ReviewedSignatures, or add it to ExcludedFailureShapes.");
    }

    [Fact]
    public void WrappedMethodsHaveNoNewOrChangedUpstreamSignatures()
    {
        var actual = UpstreamMethods
            .Where(m => WrappedMethods.ContainsKey(m.Name))
            .Select(Signature)
            .ToHashSet();
        var reviewed = ReviewedSignatures.ToHashSet();

        var added = actual.Except(reviewed).Order().ToArray();
        var removed = reviewed.Except(actual).Order().ToArray();

        Assert.True(
            added.Length == 0 && removed.Length == 0,
            "TypedResults signatures changed since they were last reviewed." +
            (added.Length > 0 ? "\nNew or changed: " + string.Join("; ", added) : "") +
            (removed.Length > 0 ? "\nNo longer present: " + string.Join("; ", removed) : "") +
            "\nWrap or consciously skip each one, then update ReviewedSignatures.");
    }

    [Fact]
    public void ListedNamesStillExistUpstream()
    {
        var upstreamNames = UpstreamMethods.Select(m => m.Name).ToHashSet();

        var stale = WrappedMethods.Keys.Concat(ExcludedFailureShapes)
            .Where(name => !upstreamNames.Contains(name))
            .Order()
            .ToArray();

        Assert.True(
            stale.Length == 0,
            "These names are listed but no longer exist on TypedResults: " + string.Join(", ", stale));
    }

    [Fact]
    public void EveryWrappedMethodHasItsExtensionMethod()
    {
        var extensions = typeof(ResultToMinimalResultExtension)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(m => m.Name)
            .ToHashSet();

        var missing = WrappedMethods
            .SelectMany(pair => pair.Value.Select(extension => (Upstream: pair.Key, Extension: extension)))
            .Where(x => !extensions.Contains(x.Extension))
            .Select(x => $"{x.Upstream} -> {x.Extension}")
            .Order()
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "WrappedMethods maps to extension methods that do not exist: " + string.Join(", ", missing));
    }

    [Fact]
    public void WrappedAndExcludedNamesDoNotOverlap() =>
        Assert.Empty(WrappedMethods.Keys.Intersect(ExcludedFailureShapes));
}
