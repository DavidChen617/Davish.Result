using System.Runtime.CompilerServices;

// Grants the test project access to internal members — ResultHttpOptions.Configure/ResolveStatusCode
// (only reachable externally via ResultAspNetCoreOptions.ConfigureStatusCodes) and the test-only
// ResetForTesting hook. It does not affect what's public to real consumers of this package.
[assembly: InternalsVisibleTo("Result.AspNetCore.Http.Tests")]
