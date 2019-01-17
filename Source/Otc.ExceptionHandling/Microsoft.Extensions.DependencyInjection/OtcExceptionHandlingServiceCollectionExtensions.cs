using Otc.ExceptionHandling;
using Otc.ExceptionHandling.Abstractions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OtcExceptionHandlingServiceCollectionExtensions
    {
        public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddScoped<IExceptionHandler, ExceptionHandler>();

            return services;
        }
    }
}