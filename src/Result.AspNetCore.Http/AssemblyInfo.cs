using System.Runtime.CompilerServices;

// Grants the test project access to internal test-only hooks (e.g. ResultHttpOptions.ResetForTesting).
// It does not affect what's public to real consumers of this package.
[assembly: InternalsVisibleTo("Result.AspNetCore.Http.UnitTests")]
