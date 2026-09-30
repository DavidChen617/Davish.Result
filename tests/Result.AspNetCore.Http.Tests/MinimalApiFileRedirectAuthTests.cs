using System.IO.Pipelines;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Davish.Result.AspNetCore.Http.Tests;

/// <summary>
/// Tests the file/stream, redirect, sign-in/out and bare status-code success shapes. None of the underlying
/// <c>HttpResults</c> types implement <c>IEndpointMetadataProvider</c>, so there is no metadata to assert here.
/// </summary>
public class MinimalApiFileRedirectAuthTests : IDisposable
{
    private static readonly Error NotFoundError = new("Item.NotFound", "Item was not found", ErrorType.NotFound);

    private static readonly byte[] Payload = Encoding.UTF8.GetBytes("hello");

    private readonly string directory = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "davish-result-" + Guid.NewGuid().ToString("N"))).FullName;

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private sealed record Item(int Id);

    private sealed class FakeEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Test";
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(webRoot);
    }

    private async Task<DefaultHttpContext> ExecuteAsync(IResult result)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<IWebHostEnvironment>(new FakeEnvironment(directory));
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        services.AddResultAspNetCore(o => o.AddMinimalApiResult());

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };

        await result.ExecuteAsync(httpContext);
        httpContext.Response.Body.Position = 0;

        return httpContext;
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext httpContext)
    {
        using var reader = new StreamReader(httpContext.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static ClaimsPrincipal Principal(string name) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], CookieAuthenticationDefaults.AuthenticationScheme));

    // ---- Bytes / File ----

    [Fact]
    public async Task GivenSuccessResult_WhenToBytes_ThenWritesBytesWithContentTypeAndFileName()
    {
        var context = await ExecuteAsync(Result.Success().ToBytes(Payload, "text/plain", "a.txt"));

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);
        Assert.Contains("a.txt", context.Response.Headers.ContentDisposition.ToString());
        Assert.Equal("hello", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToBytesReadOnlyMemory_ThenWritesBytes()
    {
        var context = await ExecuteAsync(Result.Success().ToBytes(new ReadOnlyMemory<byte>(Payload)));

        Assert.Equal("hello", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToBytesSelectors_ThenBytesAreBuiltFromValue()
    {
        var fromArray = await ExecuteAsync(Result.Success(new Item(1)).ToBytes(i => Encoding.UTF8.GetBytes($"a{i.Id}")));
        var fromMemory = await ExecuteAsync(
            Result.Success(new Item(2)).ToBytes(i => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes($"b{i.Id}")));

        Assert.Equal("a1", await ReadBodyAsync(fromArray));
        Assert.Equal("b2", await ReadBodyAsync(fromMemory));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToFileBytes_ThenWritesBytes()
    {
        var context = await ExecuteAsync(Result.Success().ToFile(Payload, "text/plain"));

        Assert.Equal("hello", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToFileStream_ThenWritesStream()
    {
        var context = await ExecuteAsync(Result.Success().ToFile(new MemoryStream(Payload), "text/plain"));

        Assert.Equal("hello", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToFileSelectors_ThenContentIsBuiltFromValue()
    {
        var fromBytes = await ExecuteAsync(Result.Success(new Item(1)).ToFile(i => Encoding.UTF8.GetBytes($"a{i.Id}")));
        var fromStream = await ExecuteAsync(
            Result.Success(new Item(2)).ToFile(i => (Stream)new MemoryStream(Encoding.UTF8.GetBytes($"b{i.Id}"))));

        Assert.Equal("a1", await ReadBodyAsync(fromBytes));
        Assert.Equal("b2", await ReadBodyAsync(fromStream));
    }

    [Fact]
    public async Task GivenFailedValue_WhenToFileSelector_ThenSelectorIsNotInvokedAndFailureHandlerRuns()
    {
        var invoked = false;

        var context = await ExecuteAsync(Result.Failure<Item>(NotFoundError).ToBytes(_ =>
        {
            invoked = true;
            return Payload;
        }));

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    // ---- PhysicalFile / VirtualFile ----

    [Fact]
    public async Task GivenSuccessResult_WhenToPhysicalFile_ThenWritesFile()
    {
        var path = Path.Combine(directory, "physical.txt");
        await File.WriteAllTextAsync(path, "physical");

        var context = await ExecuteAsync(Result.Success().ToPhysicalFile(path, "text/plain"));

        Assert.Equal("physical", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToPhysicalFileSelector_ThenPathIsBuiltFromValue()
    {
        await File.WriteAllTextAsync(Path.Combine(directory, "item-3.txt"), "three");

        var context = await ExecuteAsync(
            Result.Success(new Item(3)).ToPhysicalFile(i => Path.Combine(directory, $"item-{i.Id}.txt"), "text/plain"));

        Assert.Equal("three", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToVirtualFile_ThenWritesFileFromWebRoot()
    {
        await File.WriteAllTextAsync(Path.Combine(directory, "virtual.txt"), "virtual");

        var context = await ExecuteAsync(Result.Success().ToVirtualFile("virtual.txt", "text/plain"));

        Assert.Equal("virtual", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToVirtualFileSelector_ThenPathIsBuiltFromValue()
    {
        await File.WriteAllTextAsync(Path.Combine(directory, "item-4.txt"), "four");

        var context = await ExecuteAsync(
            Result.Success(new Item(4)).ToVirtualFile(i => $"item-{i.Id}.txt", "text/plain"));

        Assert.Equal("four", await ReadBodyAsync(context));
    }

    // ---- Stream ----

    [Fact]
    public async Task GivenSuccessResult_WhenToStreamOverloads_ThenEachWritesItsContent()
    {
        var fromStream = await ExecuteAsync(Result.Success().ToStream(new MemoryStream(Payload), "text/plain"));
        var fromPipe = await ExecuteAsync(
            Result.Success().ToStream(PipeReader.Create(new MemoryStream(Payload)), "text/plain"));
        var fromCallback = await ExecuteAsync(
            Result.Success().ToStream(body => body.WriteAsync(Payload).AsTask(), "text/plain"));

        Assert.Equal("hello", await ReadBodyAsync(fromStream));
        Assert.Equal("hello", await ReadBodyAsync(fromPipe));
        Assert.Equal("hello", await ReadBodyAsync(fromCallback));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToStreamSelectors_ThenEachWritesContentBuiltFromValue()
    {
        var fromStream = await ExecuteAsync(Result.Success(new Item(1)).ToStream(
            i => (Stream)new MemoryStream(Encoding.UTF8.GetBytes($"s{i.Id}")), "text/plain"));
        var fromPipe = await ExecuteAsync(Result.Success(new Item(2)).ToStream(
            i => PipeReader.Create(new MemoryStream(Encoding.UTF8.GetBytes($"p{i.Id}"))), "text/plain"));
        var fromCallback = await ExecuteAsync(Result.Success(new Item(3)).ToStream(
            (i, body) => body.WriteAsync(Encoding.UTF8.GetBytes($"c{i.Id}")).AsTask(), "text/plain"));

        Assert.Equal("s1", await ReadBodyAsync(fromStream));
        Assert.Equal("p2", await ReadBodyAsync(fromPipe));
        Assert.Equal("c3", await ReadBodyAsync(fromCallback));
    }

    [Fact]
    public async Task GivenFailedResult_WhenToStream_ThenDelegatesToFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure(NotFoundError).ToStream(new MemoryStream(Payload)));

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    // ---- Redirects ----

    [Theory]
    [InlineData(false, false, StatusCodes.Status302Found)]
    [InlineData(true, false, StatusCodes.Status301MovedPermanently)]
    [InlineData(false, true, StatusCodes.Status307TemporaryRedirect)]
    [InlineData(true, true, StatusCodes.Status308PermanentRedirect)]
    public async Task GivenSuccessResult_WhenToRedirect_ThenStatusCodeDependsOnPermanentAndPreserveMethod(
        bool permanent, bool preserveMethod, int expected)
    {
        var httpResult = Result.Success().ToRedirect("https://example.com/next", permanent, preserveMethod);
        var context = await ExecuteAsync(httpResult);

        Assert.Equal(expected, httpResult.StatusCode);
        Assert.Equal(expected, context.Response.StatusCode);
        Assert.Equal("https://example.com/next", context.Response.Headers.Location);
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToLocalRedirect_ThenRedirectsToLocalUrl()
    {
        var context = await ExecuteAsync(Result.Success().ToLocalRedirect("/items/1", permanent: true));

        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/items/1", context.Response.Headers.Location);
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToRedirectAndToLocalRedirectSelectors_ThenUrlIsBuiltFromValue()
    {
        var redirect = await ExecuteAsync(Result.Success(new Item(5)).ToRedirect(i => $"https://example.com/{i.Id}"));
        var local = await ExecuteAsync(Result.Success(new Item(6)).ToLocalRedirect(i => $"/items/{i.Id}"));

        Assert.Equal("https://example.com/5", redirect.Response.Headers.Location);
        Assert.Equal("/items/6", local.Response.Headers.Location);
    }

    [Fact]
    public void GivenSuccessResult_WhenToRedirectToRoute_ThenStatusCodeDependsOnFlags()
    {
        Assert.Equal(StatusCodes.Status302Found, Result.Success().ToRedirectToRoute("GetItem", new { id = 1 }).StatusCode);
        Assert.Equal(
            StatusCodes.Status308PermanentRedirect,
            Result.Success().ToRedirectToRoute("GetItem", new RouteValueDictionary { ["id"] = 1 }, true, true).StatusCode);
        Assert.Equal(
            StatusCodes.Status301MovedPermanently,
            Result.Success(new Item(1)).ToRedirectToRoute("GetItem", i => new { id = i.Id }, permanent: true).StatusCode);
        Assert.Equal(
            StatusCodes.Status307TemporaryRedirect,
            Result.Success(new Item(1))
                .ToRedirectToRoute("GetItem", new RouteValueDictionary { ["id"] = 1 }, preserveMethod: true).StatusCode);
    }

    [Fact]
    public async Task GivenFailedValue_WhenToRedirectSelector_ThenSelectorIsNotInvokedAndFailureHandlerRuns()
    {
        var invoked = false;

        var context = await ExecuteAsync(Result.Failure<Item>(NotFoundError).ToRedirect(i =>
        {
            invoked = true;
            return $"/{i.Id}";
        }));

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public void GivenFailedResult_WhenToRedirect_ThenStatusCodeReflectsErrorType()
    {
        Assert.Equal(StatusCodes.Status404NotFound, Result.Failure(NotFoundError).ToRedirect("/x", permanent: true).StatusCode);
    }

    // ---- SignIn / SignOut ----

    [Fact]
    public async Task GivenSuccessResult_WhenToSignIn_ThenWritesAuthenticationCookie()
    {
        var context = await ExecuteAsync(Result.Success().ToSignIn(Principal("alice")));

        Assert.Contains(".AspNetCore.Cookies", context.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToSignInSelector_ThenPrincipalIsBuiltFromValue()
    {
        var invoked = false;

        var context = await ExecuteAsync(Result.Success(new Item(1)).ToSignIn(i =>
        {
            invoked = true;
            return Principal($"user-{i.Id}");
        }));

        Assert.True(invoked);
        Assert.Contains(".AspNetCore.Cookies", context.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task GivenFailedValue_WhenToSignInSelector_ThenSelectorIsNotInvokedAndNoCookieIsWritten()
    {
        var invoked = false;

        var context = await ExecuteAsync(Result.Failure<Item>(NotFoundError).ToSignIn(_ =>
        {
            invoked = true;
            return Principal("x");
        }));

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(HeaderNames.SetCookie));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToSignOut_ThenExpiresAuthenticationCookie()
    {
        var context = await ExecuteAsync(Result.Success().ToSignOut());

        var setCookie = context.Response.Headers.SetCookie.ToString();
        Assert.Contains(".AspNetCore.Cookies=;", setCookie);
        Assert.Contains("expires=", setCookie);
    }

    [Fact]
    public async Task GivenFailedResult_WhenToSignOut_ThenDelegatesToFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure(NotFoundError).ToSignOut());

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    // ---- SignIn / SignOut: same behavior as the native TypedResults, with and without RedirectUri ----

    [Fact]
    public async Task GivenNoRedirectUri_WhenToSignIn_ThenBehavesLikeNative200WithCookieAndNoLocationOrBody()
    {
        var ours = await ExecuteAsync(Result.Success().ToSignIn(Principal("alice")));
        var native = await ExecuteAsync(TypedResults.SignIn(Principal("alice")));

        foreach (var context in new[] { ours, native })
        {
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.False(context.Response.Headers.ContainsKey(HeaderNames.Location));
            Assert.Contains(".AspNetCore.Cookies", context.Response.Headers.SetCookie.ToString());
            Assert.Equal(string.Empty, await ReadBodyAsync(context));
        }
    }

    [Fact]
    public async Task GivenRedirectUri_WhenToSignIn_ThenBehavesLikeNative302WithLocationAndCookie()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/dashboard" };

        var ours = await ExecuteAsync(Result.Success().ToSignIn(Principal("alice"), properties));
        var native = await ExecuteAsync(TypedResults.SignIn(Principal("alice"), properties));

        foreach (var context in new[] { ours, native })
        {
            Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
            Assert.Equal("/dashboard", context.Response.Headers.Location);
            Assert.Contains(".AspNetCore.Cookies", context.Response.Headers.SetCookie.ToString());
        }
    }

    [Fact]
    public async Task GivenNoRedirectUri_WhenToSignOut_ThenBehavesLikeNative200WithExpiredCookieAndNoLocationOrBody()
    {
        var ours = await ExecuteAsync(Result.Success().ToSignOut());
        var native = await ExecuteAsync(TypedResults.SignOut());

        foreach (var context in new[] { ours, native })
        {
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.False(context.Response.Headers.ContainsKey(HeaderNames.Location));
            Assert.Contains("expires=", context.Response.Headers.SetCookie.ToString());
            Assert.Equal(string.Empty, await ReadBodyAsync(context));
        }
    }

    [Fact]
    public async Task GivenRedirectUri_WhenToSignOut_ThenBehavesLikeNative302WithLocationAndExpiredCookie()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/" };

        var ours = await ExecuteAsync(Result.Success().ToSignOut(properties));
        var native = await ExecuteAsync(TypedResults.SignOut(properties));

        foreach (var context in new[] { ours, native })
        {
            Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
            Assert.Equal("/", context.Response.Headers.Location);
            Assert.Contains("expires=", context.Response.Headers.SetCookie.ToString());
        }
    }

    [Fact]
    public async Task GivenAuthenticationIsNotRegistered_WhenSuccessfulToSignIn_ThenThrowsLikeNativeButFailedResultDoesNot()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResultAspNetCore(o => o.AddMinimalApiResult());

        HttpContext NewContext() => new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Result.Success().ToSignIn(Principal("alice")).ExecuteAsync(NewContext()));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TypedResults.SignIn(Principal("alice")).ExecuteAsync(NewContext()));

        var failedContext = NewContext();
        await Result.Failure(NotFoundError).ToSignIn(Principal("alice")).ExecuteAsync(failedContext);
        Assert.Equal(StatusCodes.Status404NotFound, failedContext.Response.StatusCode);
    }

    // ---- Challenge / Forbid: same behavior as the native TypedResults ----

    [Fact]
    public async Task GivenSuccessResult_WhenToChallenge_ThenBehavesLikeNative()
    {
        var ours = await ExecuteAsync(Result.Success().ToChallenge());
        var native = await ExecuteAsync(TypedResults.Challenge());

        Assert.Equal(native.Response.StatusCode, ours.Response.StatusCode);
        Assert.Equal(native.Response.Headers.Location, ours.Response.Headers.Location);
        Assert.Equal(StatusCodes.Status302Found, ours.Response.StatusCode);
        Assert.Contains("/Account/Login", ours.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToForbid_ThenBehavesLikeNative()
    {
        var ours = await ExecuteAsync(Result.Success().ToForbid());
        var native = await ExecuteAsync(TypedResults.Forbid());

        Assert.Equal(native.Response.StatusCode, ours.Response.StatusCode);
        Assert.Equal(native.Response.Headers.Location, ours.Response.Headers.Location);
        Assert.Equal(StatusCodes.Status302Found, ours.Response.StatusCode);
        Assert.Contains("/Account/AccessDenied", ours.Response.Headers.Location.ToString());
    }

    [Fact]
    public void GivenSuccessResult_WhenAuthenticationShapes_ThenStatusCodeIsUnknown()
    {
        // The real status is decided by the authentication handler at execution time (e.g. 302 for a cookie handler).
        Assert.Null(Result.Success().ToChallenge().StatusCode);
        Assert.Null(Result.Success().ToForbid().StatusCode);
        Assert.Null(Result.Success().ToSignIn(Principal("alice")).StatusCode);
        Assert.Null(Result.Success().ToSignOut().StatusCode);
        Assert.Null(Result.Success(new Item(1)).ToSignIn(_ => Principal("alice")).StatusCode);
    }

    [Fact]
    public void GivenFailedResult_WhenAuthenticationShapes_ThenStatusCodeStillReflectsErrorType()
    {
        Assert.Equal(StatusCodes.Status404NotFound, Result.Failure(NotFoundError).ToChallenge().StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, Result.Failure(NotFoundError).ToSignOut().StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, Result.Failure<Item>(NotFoundError).ToSignIn(_ => Principal("x")).StatusCode);
    }

    [Fact]
    public void GivenSuccessResult_WhenFileShapesWithoutRangeOrConditionalOptions_ThenStatusCodeIs200()
    {
        Assert.Equal(StatusCodes.Status200OK, Result.Success().ToBytes(Payload).StatusCode);
        Assert.Equal(StatusCodes.Status200OK, Result.Success().ToFile(new MemoryStream(Payload)).StatusCode);
        Assert.Equal(StatusCodes.Status200OK, Result.Success().ToPhysicalFile("/x").StatusCode);
        Assert.Equal(StatusCodes.Status200OK, Result.Success().ToVirtualFile("x").StatusCode);
        Assert.Equal(StatusCodes.Status200OK, Result.Success().ToStream(new MemoryStream(Payload)).StatusCode);
        Assert.Equal(StatusCodes.Status200OK, Result.Success().ToStream(_ => Task.CompletedTask).StatusCode);
        Assert.Equal(StatusCodes.Status200OK, Result.Success(new Item(1)).ToBytes(_ => Payload).StatusCode);
    }

    [Fact]
    public void GivenSuccessResult_WhenFileShapesWithRangeOrConditionalOptions_ThenStatusCodeIsUnknown()
    {
        // Range processing can answer 206; ETag/Last-Modified can answer 304 or 412.
        Assert.Null(Result.Success().ToBytes(Payload, enableRangeProcessing: true).StatusCode);
        Assert.Null(Result.Success().ToBytes(Payload, entityTag: new EntityTagHeaderValue("\"v1\"")).StatusCode);
        Assert.Null(Result.Success().ToFile(new MemoryStream(Payload), lastModified: DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Null(Result.Success().ToPhysicalFile("/x", enableRangeProcessing: true).StatusCode);
        Assert.Null(Result.Success().ToVirtualFile("x", entityTag: new EntityTagHeaderValue("\"v1\"")).StatusCode);
        Assert.Null(Result.Success().ToStream(new MemoryStream(Payload), enableRangeProcessing: true).StatusCode);
        Assert.Null(Result.Success().ToStream(_ => Task.CompletedTask, lastModified: DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Null(Result.Success(new Item(1)).ToBytes(_ => Payload, enableRangeProcessing: true).StatusCode);
    }

    [Fact]
    public void GivenFailedResult_WhenFileShapesWithRangeOptions_ThenStatusCodeStillReflectsErrorType() =>
        Assert.Equal(
            StatusCodes.Status404NotFound,
            Result.Failure(NotFoundError).ToBytes(Payload, enableRangeProcessing: true).StatusCode);

    [Fact]
    public async Task GivenFailedResult_WhenToChallengeOrToForbid_ThenDelegatesToFailureHandlerAndSkipsAuthentication()
    {
        var challenge = await ExecuteAsync(Result.Failure(NotFoundError).ToChallenge());
        var forbid = await ExecuteAsync(Result.Failure(NotFoundError).ToForbid());

        Assert.Equal(StatusCodes.Status404NotFound, challenge.Response.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, forbid.Response.StatusCode);
        Assert.False(challenge.Response.Headers.ContainsKey(HeaderNames.Location));
    }

    [Fact]
    public async Task GivenAuthenticationIsNotRegistered_WhenSuccessfulToChallenge_ThenThrowsLikeNative()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResultAspNetCore(o => o.AddMinimalApiResult());

        HttpContext NewContext() => new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Result.Success().ToChallenge().ExecuteAsync(NewContext()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => TypedResults.Challenge().ExecuteAsync(NewContext()));
    }

    // ---- Status ----

    [Fact]
    public async Task GivenSuccessResult_WhenToStatus_ThenWritesThatStatusCode()
    {
        var httpResult = Result.Success().ToStatus(StatusCodes.Status206PartialContent);
        var context = await ExecuteAsync(httpResult);

        Assert.Equal(StatusCodes.Status206PartialContent, httpResult.StatusCode);
        Assert.Equal(StatusCodes.Status206PartialContent, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenFailedResult_WhenToStatus_ThenDelegatesToFailureHandler()
    {
        var httpResult = Result.Failure(NotFoundError).ToStatus(StatusCodes.Status206PartialContent);
        var context = await ExecuteAsync(httpResult);

        Assert.Equal(StatusCodes.Status404NotFound, httpResult.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }
}
