using Davish.Result;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the <see cref="ErrorTypeBase"/> to HTTP status code mapping used by <see cref="ResultToMinimalResultExtension"/>
/// through the familiar <see cref="IServiceCollection"/> registration style.
/// </summary>
public static class ResultHttpOptionsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures the <see cref="ErrorTypeBase"/> to HTTP status code mapping using the built-in defaults.
        /// </summary>
        public IServiceCollection AddCustomResultErrorTypeMap()
        {
            services.AddCustomResultErrorTypeMap(_ => { });
            return services;
        }

        /// <summary>
        /// Configures the <see cref="ErrorTypeBase"/> to HTTP status code mapping.
        /// </summary>
        /// <remarks>
        /// The mapping is process-wide static state (see <see cref="ResultHttpOptions"/>), applied immediately when this
        /// method runs rather than resolved per-request from the DI container. Call it once, e.g. in <c>Program.cs</c>.
        /// </remarks>
        /// <param name="configure">Builds the configuration: whether to seed the built-in mappings, and any custom ones.</param>
        public IServiceCollection AddCustomResultErrorTypeMap(Action<ResultHttpOptionsBuilder> configure)
        {
            ResultHttpOptions.Configure(configure);
            return services;
        }
    }
}
