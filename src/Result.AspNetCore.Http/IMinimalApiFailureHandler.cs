using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>The Minimal API specialization of <see cref="IFailureHandler{TResult}"/>.</summary>
public interface IMinimalApiFailureHandler : IFailureHandler<IResult>;

