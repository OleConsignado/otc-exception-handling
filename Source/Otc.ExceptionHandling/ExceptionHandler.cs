using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Otc.DomainBase.Exceptions;
using Otc.ExceptionHandling.Abstractions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling
{
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly ILogger logger;
        private readonly IExceptionHandlerConfiguration configuration;

        public ExceptionHandler(ILoggerFactory loggerFactory,
            IExceptionHandlerConfiguration configuration)
        {
            logger = loggerFactory?.CreateLogger<ExceptionHandler>();

            this.configuration = configuration;

            if (logger == null)
                throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task<int> HandleExceptionAsync(Exception exception, HttpContext httpContext)
        {
            ExceptionHandlerBehavior? behavior = null;
            (httpContext.Response.StatusCode, exception, behavior) =
                await ValidateConfigurationsAsync(httpContext.Response.StatusCode, exception);

            if (exception is AggregateException)
            {
                var aggregateException = exception as AggregateException;

                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    await HandleExceptionAsync(innerException, httpContext);
                }
            }
            else
            {
                if (!behavior.HasValue)
                {
                    if (exception is UnauthorizedAccessException)
                    {
                        httpContext.Response.StatusCode = 403;

                        return await GenerateUnauthorizadeExceptionResponseAsync(httpContext);
                    }

                    behavior = await IdentifyBehaviorAsync(exception, httpContext);
                }

                switch (behavior)
                {
                    case ExceptionHandlerBehavior.ClientError:
                        return await GenerateCoreExceptionResponseAsync(exception, httpContext);

                    case ExceptionHandlerBehavior.ServerError:
                        return await GenerateInternalErrorResponseAsync(exception, httpContext);

                }
            }

            return httpContext.Response.StatusCode;
        }

        private async Task<ExceptionHandlerBehavior> IdentifyBehaviorAsync(Exception exception, HttpContext httpContext)
        {
            ExceptionHandlerBehavior behavior;

            if (exception is CoreException)
            {
                behavior = ExceptionHandlerBehavior.ClientError;
                httpContext.Response.StatusCode = 400;
            }
            else
            {
                behavior = ExceptionHandlerBehavior.ServerError;
                httpContext.Response.StatusCode = 500;
            }

            return await Task.FromResult(behavior);
        }

        private async Task<int> GenerateCoreExceptionResponseAsync(Exception e, HttpContext httpContext)
        {
            logger.LogInformation(e, "Ocorreu um erro de negócio.");

            await GenerateResponseAsync(e, httpContext);

            return httpContext.Response.StatusCode;
        }

        /// <summary>
        /// Retorna um httpStatusCode 401.
        /// </summary>
        /// <param name="e">Inner Exception</param>
        /// <param name="httpContext">HttpContext</param>
        /// <returns></returns>
        private async Task<int> GenerateUnauthorizadeExceptionResponseAsync(HttpContext httpContext)
        {
            logger.LogInformation("Ocorreu um acesso não autorizado.");

            var forbidden = new
            {
                Key = "Forbidden",
                Message = "Access to this resource is forbidden."
            };

            await GenerateResponseAsync(forbidden, httpContext);

            return httpContext.Response.StatusCode;
        }

        private bool IsDevelopmentEnvironment()
            => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        private async Task<int> GenerateInternalErrorResponseAsync(Exception e, HttpContext httpContext)
        {
            Exception exception = e;
            Guid logEntryId = Guid.NewGuid();

            logger.LogError(e, "{LogEntryId}: Ocorreu um erro não esperado.", logEntryId);

            var internalError = new InternalError()
            {
                LogEntryId = logEntryId,
                Exception = (IsDevelopmentEnvironment() ? exception.GetBaseException() : null)
            };

            await GenerateResponseAsync(internalError, httpContext);

            return httpContext.Response.StatusCode;
        }

        private async Task GenerateResponseAsync(object output, HttpContext httpContext)
        {
            var serializer = configuration?.Serializer() ?? new ExceptionSerializer();
            var message = await serializer.SerializeAsync(output, httpContext);

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(message, Encoding.UTF8);
        }

        private Task<(int statusCode, Exception exception, ExceptionHandlerBehavior? behavior)> ValidateConfigurationsAsync(int statusCode, Exception e)
        {
            Exception exception = e;
            int finalStatusCode = statusCode;
            ExceptionHandlerBehavior? behavior = null;

            //Executar eventos
            if (configuration != null)
            {
                if (configuration.Events.Any())
                {
                    foreach (var @event in configuration.Events)
                    {
                        if (@event.IsElegible(statusCode, e))
                            (finalStatusCode, exception, behavior) = @event.Intercept(statusCode, e);
                    }
                }

                if (configuration.HasBehaviors)
                {
                    var behaviorResult = configuration.ValidateBehavior(e);

                    if (behaviorResult != null)
                    {
                        behavior = behaviorResult.Behavior;

                        finalStatusCode = behaviorResult.StatusCode;
                    }
                }
            }

            return Task.FromResult((finalStatusCode, exception, behavior));

        }
    }
}
