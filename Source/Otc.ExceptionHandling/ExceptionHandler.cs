using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Otc.DomainBase.Exceptions;
using Otc.ExceptionHandling.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            if (exception is AggregateException)
            {
                var aggregateException = exception as AggregateException;
                int statusCode = 400;

                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    statusCode = await HandleExceptionAsync(innerException, httpContext);
                }

                return statusCode;
            }
            else if (exception is CoreException)
            {
                return await GenerateCoreExceptionResponseAsync(exception as CoreException, httpContext);
            }
            else if (exception is UnauthorizedAccessException)
            {
                return await GenerateUnauthorizadeExceptionResponseAsync(httpContext);
            }
            else
            {
                return await GenerateInternalErrorResposeAsync(exception, httpContext);
            }
        }

        private async Task<int> GenerateCoreExceptionResponseAsync(CoreException e, HttpContext httpContext)
        {
            int statusCode = 400;

            logger.LogInformation(e, "Ocorreu um erro de negócio.");

            await GenerateResponseAsync(statusCode, e, httpContext);

            return statusCode;
        }

        /// <summary>
        /// Retorna um httpStatusCode 401.
        /// </summary>
        /// <param name="e">Inner Exception</param>
        /// <param name="httpContext">HttpContext</param>
        /// <returns></returns>
        private async Task<int> GenerateUnauthorizadeExceptionResponseAsync(HttpContext httpContext)
        {
            int statusCode = 403;
            logger.LogInformation("Ocorreu um acesso não autorizado.");

            var forbidden = new
            {
                Key = "Forbidden",
                Message = "Access to this resource is forbidden."
            };

            await GenerateResponseAsync(statusCode, forbidden, httpContext);

            return statusCode;
        }

        private bool IsDevelopmentEnvironment()
            => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        private async Task<int> GenerateInternalErrorResposeAsync(Exception e, HttpContext httpContext)
        {
            int statusCode = 500;
            Exception exception = e;
            Guid logEntryId = Guid.NewGuid();

            (statusCode, exception) = await ValidateConfigurationsAsync(statusCode, e);

            logger.LogError(e, "{LogEntryId}: Ocorreu um erro não esperado.", logEntryId);           

            var internalError = new InternalError()
            {
                LogEntryId = logEntryId,
                Exception = (IsDevelopmentEnvironment() ? exception : null)
            };

            await GenerateResponseAsync(statusCode, internalError, httpContext);

            return statusCode;
        }

        private async Task GenerateResponseAsync(int statusCode, object output, HttpContext httpContext)
        {
            httpContext.Response.StatusCode = statusCode;

            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver =
                    new CoreExceptionJsonContractResolver()
                    {
                        IgnoreSerializableInterface = true
                    },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MaxDepth = 10,
                Formatting = !IsDevelopmentEnvironment() ? Formatting.None : Formatting.Indented
            };

            var message = JsonConvert.SerializeObject(output, jsonSerializerSettings);

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(message, Encoding.UTF8);
        }

        private Task<(int statusCode, Exception exception)> ValidateConfigurationsAsync(int statusCode, Exception e)
        {
            Exception exception = e;
            int finalStatusCode = statusCode;
            //Executar eventos
            if (configuration != null)
            {
                if (configuration.Events.Any())
                    foreach (var @event in configuration.Events)
                    {
                        if (@event.IsElegible(statusCode, e))
                            (finalStatusCode, exception) = @event.Intercept(statusCode, e);
                    }

                if (configuration.HasBehaviors)
                {
                    var behaviorResult = configuration.ValidateBehavior(e);

                    if (behaviorResult != null)
                    {
                        switch (behaviorResult.Behavior)
                        {
                            case Abstractions.Enums.ExceptionHandlerBehavior.Suppress:
                                exception = e.GetBaseException();
                                break;
                            case Abstractions.Enums.ExceptionHandlerBehavior.Ignore:
                                exception = null;
                                break;
                            case Abstractions.Enums.ExceptionHandlerBehavior.Expose:
                            default:
                                break;
                        }

                        finalStatusCode = behaviorResult.StatusCode;
                    }
                }
            }

            return Task.FromResult((finalStatusCode, exception));

        }
    }
}
