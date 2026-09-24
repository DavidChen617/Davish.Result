using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Davish.Result.AspNetCore.Http.Tests;

public class ExceptionMapperDispatcherTests
{
    private class BaseException : Exception;
    private sealed class DerivedException : BaseException;
    private sealed class UnrelatedException : Exception;

    private static (IExceptionMapperDispatcher Dispatcher, HttpContext Context) BuildDispatcher(
        Action<ResultAspNetCoreOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddResultAspNetCore(configure);
        var provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<IExceptionMapperDispatcher>(), new DefaultHttpContext { RequestServices = provider });
    }

    [Fact]
    public async Task GivenExactTypeMapperRegistered_WhenMatchingExceptionThrown_ThenReturnsMappedResult()
    {
        var expected = Result.Failure(new Error("Base.Error", "from base mapper"));
        var (dispatcher, context) = BuildDispatcher(o =>
            o.MapExceptionToResult<BaseException>((_, _) => expected));

        var result = await dispatcher.MapAsync(context, new BaseException(), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GivenMapperRegisteredForBaseType_WhenSubclassExceptionThrown_ThenBaseTypeMapperStillMatches()
    {
        var expected = Result.Failure(new Error("Base.Error", "from base mapper"));
        var (dispatcher, context) = BuildDispatcher(o =>
            o.MapExceptionToResult<BaseException>((_, _) => expected));

        var result = await dispatcher.MapAsync(context, new DerivedException(), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GivenMappersRegisteredForBothBaseAndDerived_WhenDerivedExceptionThrown_ThenMostSpecificMapperWins()
    {
        var baseResult = Result.Failure(new Error("Base.Error", "from base mapper"));
        var derivedResult = Result.Failure(new Error("Derived.Error", "from derived mapper"));
        var (dispatcher, context) = BuildDispatcher(o =>
        {
            o.MapExceptionToResult<BaseException>((_, _) => baseResult);
            o.MapExceptionToResult<DerivedException>((_, _) => derivedResult);
        });

        var result = await dispatcher.MapAsync(context, new DerivedException(), CancellationToken.None);

        Assert.Equal(derivedResult, result);
    }

    [Fact]
    public async Task GivenNoMatchingMapperAnywhereInChain_WhenExceptionThrown_ThenReturnsNull()
    {
        var (dispatcher, context) = BuildDispatcher(o =>
            o.MapExceptionToResult<BaseException>((_, _) => Result.Failure(new Error("Base.Error", "n/a"))));

        var result = await dispatcher.MapAsync(context, new UnrelatedException(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GivenSameExceptionTypeResolvedTwice_WhenMapAsyncCalledAgain_ThenStillReturnsCorrectResult()
    {
        var expected = Result.Failure(new Error("Base.Error", "from base mapper"));
        var (dispatcher, context) = BuildDispatcher(o =>
            o.MapExceptionToResult<BaseException>((_, _) => expected));

        var first = await dispatcher.MapAsync(context, new DerivedException(), CancellationToken.None);
        var second = await dispatcher.MapAsync(context, new DerivedException(), CancellationToken.None);

        Assert.Equal(expected, first);
        Assert.Equal(expected, second);
    }

    [Fact]
    public async Task GivenAsyncDelegateMapper_WhenMapAsyncCalled_ThenReturnsExpectedResult()
    {
        var expected = Result.Failure(new Error("Base.Error", "async"));
        var (dispatcher, context) = BuildDispatcher(o =>
            o.MapExceptionToResult<BaseException>((_, _, _) => ValueTask.FromResult(expected)));

        var result = await dispatcher.MapAsync(context, new BaseException(), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    private sealed class StubMapper : IExceptionMapper<BaseException>
    {
        public ValueTask<Result> MapAsync(HttpContext context, BaseException source, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Failure(new Error("Base.Error", "from DI mapper")));
    }

    [Fact]
    public async Task GivenDiBasedMapper_WhenMapAsyncCalled_ThenTMapperResolvedFromRequestServicesAndInvoked()
    {
        var (dispatcher, context) = BuildDispatcher(o =>
            o.MapExceptionToResult<BaseException, StubMapper>());

        var result = await dispatcher.MapAsync(context, new BaseException(), CancellationToken.None);

        Assert.Equal("Base.Error", result?.Error.Code);
    }

    [Fact]
    public async Task GivenDiBasedMapper_WhenResolvedTwice_ThenNewTransientInstanceEachTime()
    {
        var services = new ServiceCollection();
        services.AddResultAspNetCore(o => o.MapExceptionToResult<BaseException, StubMapper>());
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<StubMapper>();
        var second = provider.GetRequiredService<StubMapper>();

        Assert.NotSame(first, second);
    }
}
