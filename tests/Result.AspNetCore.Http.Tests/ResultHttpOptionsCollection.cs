namespace Davish.Result.AspNetCore.Http.Tests;

/// <summary>
/// Groups every test that touches <c>ResultHttpOptions</c>' shared static state (directly, via
/// <c>ResetForTesting</c>, or indirectly via <c>ToProblemDetail</c>/<c>AddCustomResultErrorTypeMap</c>) into a
/// single xUnit collection so they run sequentially instead of racing each other across test classes.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ResultHttpOptionsCollection
{
    public const string Name = nameof(ResultHttpOptionsCollection);
}
