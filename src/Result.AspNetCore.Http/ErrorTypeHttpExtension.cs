namespace Davish.Result;

/// <summary>
/// Exposes the configured HTTP status code mapping to consumers writing their own <c>ToXxx()</c> result extensions,
/// without exposing <see cref="ResultHttpOptions"/> itself.
/// </summary>
public static class ErrorTypeHttpExtension
{
    extension(ErrorType errorType)
    {
        /// <summary>
        /// Resolves the HTTP status code configured for this error type via <see cref="ResultHttpOptions.Configure"/>,
        /// or <see cref="Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError"/> if none was registered.
        /// </summary>
        public int ToStatusCode() => ResultHttpOptions.ResolveStatusCode(errorType);
    }
}
