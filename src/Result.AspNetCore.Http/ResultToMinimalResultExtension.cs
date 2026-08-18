using Microsoft.AspNetCore.Http;
using static Davish.Result.ResultHttpOptions;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace Davish.Result;

/// <summary>
/// Converts a <see cref="Result"/> or <see cref="Result{TValue}"/> into a Minimal API <see cref="IResult"/>.
/// </summary>
public static class ResultToMinimalResultExtension
{
    extension(Result result)
    {
        /// <summary>Converts a successful result to <c>200 OK</c>, or the error to a problem result.</summary>
        public IResult ToOk()
        {
            return !result.IsSuccess ? result.ToProblemDetail() : Ok();
        }

        /// <summary>Converts a successful result to <c>204 No Content</c>, or the error to a problem result.</summary>
        public IResult ToNoContent()
        {
            return !result.IsSuccess ? result.ToProblemDetail() : NoContent();
        }

        /// <summary>Converts a successful result to <c>201 Created</c> at the given route, or the error to a problem result.</summary>
        public IResult ToCreated(string routeName, object? routeValues = null)
        {
            return !result.IsSuccess ? result.ToProblemDetail() : CreatedAtRoute(routeName, routeValues);
        }

        /// <summary>Converts a successful result to <c>202 Accepted</c>, or the error to a problem result.</summary>
        public IResult ToAccepted(string? uri = null)
        {
            return !result.IsSuccess ? result.ToProblemDetail() : Accepted(uri);
        }

        /// <summary>Converts a failed result's <see cref="Error"/> into a problem (or validation problem) result.</summary>
        public IResult ToProblemDetail()
        {
            if (result.Error.Fields.Count > 0)
                return result.ToValidationProblemDetail();

            var error = result.Error;

            return Problem(
                title: error.Code,
                detail: error.Description,
                statusCode: ResolveStatusCode(error.Type)
            );
        }

        /// <summary>Converts a failed result's per-field <see cref="Error"/> messages into a <c>400</c> validation problem result.</summary>
        public IResult ToValidationProblemDetail()
        {
            var error = result.Error;
            var errors = error.Fields
                .Select(x => new KeyValuePair<string, string[]>(x.Key, x.Value.ToArray()))
                .AsEnumerable();

            return ValidationProblem(
                title: error.Code,
                detail: error.Description,
                errors: errors);
        }
    }

    extension<T>(Result<T> result) where T : notnull
    {
        /// <summary>Converts a successful result to <c>200 OK</c> with its value, or the error to a problem result.</summary>
        public IResult ToOk()
        {
            return !result.IsSuccess ? result.ToProblemDetail() : TypedResults.Ok(result.Value);
        }

        /// <summary>Converts a successful result to <c>201 Created</c> at the given route with its value, or the error to a problem result.</summary>
        public IResult ToCreated(string routeName, Func<T, object?>? routeValues = null)
        {
            return !result.IsSuccess ? result.ToProblemDetail() : CreatedAtRoute(result.Value, routeName, routeValues?.Invoke(result.Value));
        }

        /// <summary>Converts a successful result to <c>202 Accepted</c> with its value, or the error to a problem result.</summary>
        public IResult ToAccepted(string? uri = null)
        {
            return !result.IsSuccess ? result.ToProblemDetail() : Accepted(uri, result.Value);
        }

        /// <summary>Converts a successful result to <c>204 No Content</c>, or the error to a problem result.</summary>
        public IResult ToNoContent()
        {
            return !result.IsSuccess ? result.ToProblemDetail() : NoContent();
        }
    }
}
