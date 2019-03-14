using Otc.ExceptionHandling;
using Otc.ExceptionHandling.Abstractions;
using Otc.ExceptionHandling.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OtcExceptionHandlingServiceCollectionExtensions
    {
        public static IServiceCollection AddExceptionHandling(this IServiceCollection services, Action<IExceptionHandlerConfigurationExpression> configuration = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration != null)
                services.AddExceptionHandlingConfiguration(configuration);

            services.AddScoped<IExceptionHandler, ExceptionHandler>();

            return services;
        }

        public static IServiceCollection AddExceptionHandlingConfiguration(this IServiceCollection services, Action<IExceptionHandlerConfigurationExpression> configuration = null)
        {
            services.AddSingleton<IExceptionHandlerConfiguration>(cfg => new ExceptionHandlerConfiguration(configuration));

            return services;
        }
    }
}