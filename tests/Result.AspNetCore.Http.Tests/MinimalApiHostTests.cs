using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Davish.Result.AspNetCore.Http.Tests;

/// <summary>
/// Success shapes that need a real routing/authentication pipeline to be verified end to end: route-based
/// <c>Location</c> generation, and options (<c>fragment</c>, range processing, <c>ETag</c>/<c>Last-Modified</c>,
/// non-default authentication schemes, content encoding) actually reaching the underlying <c>TypedResults</c>.
/// </summary>
public sealed class MinimalApiHostTests : IAsyncLifetime
{
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes("hello");
    private static readonly DateTimeOffset LastModified = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private sealed record Item(int Id);

    private IHost host = null!;
    private HttpClient client = null!;

    private static ClaimsPrincipal Principal() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "test"));

    public async Task InitializeAsync()
    {
        host = await new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddDataProtection();
                    services.AddAuthentication("a")
                        .AddCookie("a", o =>
                        {
                            o.Cookie.Name = "cookie-a";
                            o.LoginPath = "/login-a";
                            o.AccessDeniedPath = "/denied-a";
                        })
                        .AddCookie("b", o =>
                        {
                            o.Cookie.Name = "cookie-b";
                            o.LoginPath = "/login-b";
                            o.AccessDeniedPath = "/denied-b";
                        });
                    services.AddResultAspNetCore(o => o.AddMinimalApiResult());
                });

                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        var ok = Result.Success();
                        var item = Result.Success(new Item(1));
                        var rvd = () => new RouteValueDictionary { ["id"] = 1 };

                        endpoints.MapGet("/items/{id:int}", (int id) => new Item(id)).WithName("GetItem");

                        // Created / Accepted at route
                        endpoints.MapGet("/created-rvd", () => ok.ToCreated("GetItem", rvd()));
                        endpoints.MapGet("/accepted-route", () => ok.ToAcceptedAtRoute("GetItem", new { id = 1 }));
                        endpoints.MapGet("/accepted-route-rvd", () => ok.ToAcceptedAtRoute("GetItem", rvd()));
                        endpoints.MapGet("/accepted-route-t", () => item.ToAcceptedAtRoute("GetItem", i => new { id = i.Id }));
                        endpoints.MapGet("/accepted-route-t-rvd", () => item.ToAcceptedAtRoute("GetItem", rvd()));

                        // Redirect to route
                        endpoints.MapGet("/redirect-route", () => ok.ToRedirectToRoute("GetItem", new { id = 1 }));
                        endpoints.MapGet("/redirect-route-fragment", () =>
                            ok.ToRedirectToRoute("GetItem", new { id = 1 }, fragment: "top"));
                        endpoints.MapGet("/redirect-route-permanent", () =>
                            ok.ToRedirectToRoute("GetItem", new { id = 1 }, permanent: true, preserveMethod: true));
                        endpoints.MapGet("/redirect-route-rvd", () =>
                            ok.ToRedirectToRoute("GetItem", rvd(), fragment: "top"));
                        endpoints.MapGet("/redirect-route-t", () =>
                            item.ToRedirectToRoute("GetItem", i => new { id = i.Id }, fragment: "top"));
                        endpoints.MapGet("/redirect-route-t-rvd", () =>
                            item.ToRedirectToRoute("GetItem", rvd(), fragment: "top"));

                        // File options
                        endpoints.MapGet("/bytes-range", () => ok.ToBytes(Payload, "text/plain", enableRangeProcessing: true));
                        endpoints.MapGet("/bytes-no-range", () => ok.ToBytes(Payload, "text/plain"));
                        endpoints.MapGet("/bytes-headers", () =>
                            ok.ToBytes(Payload, "text/plain", lastModified: LastModified, entityTag: new EntityTagHeaderValue("\"v1\"")));
                        endpoints.MapGet("/file-stream-range", () =>
                            ok.ToFile(new MemoryStream(Payload), "text/plain", enableRangeProcessing: true));

                        // Text encoding
                        endpoints.MapGet("/text-latin1", () => ok.ToText("é", "text/plain", Encoding.Latin1));

                        // Authentication schemes
                        endpoints.MapGet("/signin-b", () =>
                            ok.ToSignIn(Principal(), authenticationScheme: "b"));
                        endpoints.MapGet("/signin-default", () => ok.ToSignIn(Principal()));
                        endpoints.MapGet("/challenge-default", () => ok.ToChallenge());
                        endpoints.MapGet("/challenge-b", () => ok.ToChallenge(authenticationSchemes: ["b"]));
                        endpoints.MapGet("/challenge-redirect", () =>
                            ok.ToChallenge(new AuthenticationProperties { RedirectUri = "/back" }, ["b"]));
                        endpoints.MapGet("/forbid-default", () => ok.ToForbid());
                        endpoints.MapGet("/forbid-b", () => ok.ToForbid(authenticationSchemes: ["b"]));
                        endpoints.MapGet("/signout-b", () => ok.ToSignOut(authenticationSchemes: ["b"]));
                    });
                });
            })
            .StartAsync();

        client = host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await host.StopAsync();
        host.Dispose();
    }

    private async Task<HttpResponseMessage> GetAsync(string path, Action<HttpRequestMessage>? configure = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        configure?.Invoke(request);
        return await client.SendAsync(request);
    }

    // ---- Route-based Location ----

    [Theory]
    [InlineData("/created-rvd", HttpStatusCode.Created)]
    [InlineData("/accepted-route", HttpStatusCode.Accepted)]
    [InlineData("/accepted-route-rvd", HttpStatusCode.Accepted)]
    [InlineData("/accepted-route-t", HttpStatusCode.Accepted)]
    [InlineData("/accepted-route-t-rvd", HttpStatusCode.Accepted)]
    public async Task RouteBasedShapes_WriteLocationOfTheNamedRoute(string path, HttpStatusCode expected)
    {
        using var response = await GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("/items/1", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task ToRedirectToRoute_RedirectsToTheNamedRoute()
    {
        using var response = await GetAsync("/redirect-route");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/items/1", response.Headers.Location?.AbsolutePath);
    }

    [Theory]
    [InlineData("/redirect-route-fragment")]
    [InlineData("/redirect-route-rvd")]
    [InlineData("/redirect-route-t")]
    [InlineData("/redirect-route-t-rvd")]
    public async Task ToRedirectToRoute_AppendsTheFragment(string path)
    {
        using var response = await GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/items/1", response.Headers.Location?.AbsolutePath);
        Assert.Equal("#top", response.Headers.Location?.Fragment);
    }

    [Fact]
    public async Task ToRedirectToRoute_PermanentAndPreserveMethod_Writes308()
    {
        using var response = await GetAsync("/redirect-route-permanent");

        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
    }

    // ---- File options ----

    [Fact]
    public async Task ToBytes_WithEnableRangeProcessing_HonorsRangeRequests()
    {
        using var response = await GetAsync("/bytes-range", r => r.Headers.Range = new(0, 1));

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal("he", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ToBytes_WithoutEnableRangeProcessing_IgnoresRangeRequests()
    {
        using var response = await GetAsync("/bytes-no-range", r => r.Headers.Range = new(0, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("hello", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ToFile_Stream_WithEnableRangeProcessing_HonorsRangeRequests()
    {
        using var response = await GetAsync("/file-stream-range", r => r.Headers.Range = new(0, 1));

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal("he", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ToBytes_WithLastModifiedAndEntityTag_WritesTheHeaders()
    {
        using var response = await GetAsync("/bytes-headers");

        Assert.Equal("\"v1\"", response.Headers.ETag?.Tag);
        Assert.Equal(LastModified, response.Content.Headers.LastModified);
    }

    [Fact]
    // Like the native TypedResults.Text, the encoding is only honored when a contentType is also given.
    public async Task ToText_WithContentTypeAndContentEncoding_UsesThatEncodingForTheBodyAndCharset()
    {
        using var response = await GetAsync("/text-latin1");

        Assert.Equal("iso-8859-1", response.Content.Headers.ContentType?.CharSet);
        Assert.Equal([0xE9], await response.Content.ReadAsByteArrayAsync());
    }

    // ---- Authentication schemes ----

    private static string SetCookie(HttpResponseMessage response) =>
        string.Join("; ", response.Headers.TryGetValues(HeaderNames.SetCookie, out var values) ? values : []);

    [Fact]
    public async Task ToSignIn_WithAuthenticationScheme_UsesThatSchemesHandler()
    {
        using var response = await GetAsync("/signin-b");

        var setCookie = SetCookie(response);
        Assert.Contains("cookie-b=", setCookie);
        Assert.DoesNotContain("cookie-a=", setCookie);
    }

    [Fact]
    public async Task ToSignIn_WithoutAuthenticationScheme_UsesTheDefaultScheme()
    {
        using var response = await GetAsync("/signin-default");

        var setCookie = SetCookie(response);
        Assert.Contains("cookie-a=", setCookie);
        Assert.DoesNotContain("cookie-b=", setCookie);
    }

    [Fact]
    public async Task ToSignOut_WithAuthenticationSchemes_OnlyExpiresThoseSchemesCookies()
    {
        using var response = await GetAsync("/signout-b");

        var setCookie = SetCookie(response);
        Assert.Contains("cookie-b=;", setCookie);
        Assert.DoesNotContain("cookie-a=", setCookie);
    }

    [Fact]
    public async Task ToChallenge_WithoutAuthenticationSchemes_UsesTheDefaultSchemesLoginPath()
    {
        using var response = await GetAsync("/challenge-default");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login-a", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task ToChallenge_WithAuthenticationSchemes_UsesThoseSchemesLoginPath()
    {
        using var response = await GetAsync("/challenge-b");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login-b", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task ToChallenge_WithRedirectUri_RedirectsBackToItAfterLogin()
    {
        using var response = await GetAsync("/challenge-redirect");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("ReturnUrl=%2Fback", response.Headers.Location?.Query);
    }

    [Fact]
    public async Task ToForbid_UsesTheSchemesAccessDeniedPath()
    {
        using var defaultScheme = await GetAsync("/forbid-default");
        using var schemeB = await GetAsync("/forbid-b");

        Assert.Equal(HttpStatusCode.Redirect, defaultScheme.StatusCode);
        Assert.Equal("/denied-a", defaultScheme.Headers.Location?.AbsolutePath);
        Assert.Equal("/denied-b", schemeB.Headers.Location?.AbsolutePath);
    }
}
