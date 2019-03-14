using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Otc.DomainBase.Exceptions;
using Otc.ExceptionHandling.Abstractions;
using Otc.ExceptionHandling.Abstractions.Enums;
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
            bool hasConfiguration = false;
            (httpContext.Response.StatusCode, exception, hasConfiguration) = await ValidateConfigurationsAsync(httpContext.Response.StatusCode, exception);

            if (exception is AggregateException)
            {
                var aggregateException = exception as AggregateException;

                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    await HandleExceptionAsync(innerException, httpContext);
                }

                return httpContext.Response.StatusCode;
            }
            else if (exception is CoreException)
            {
                if (!hasConfiguration)
                    httpContext.Response.StatusCode = 400;

                return await GenerateCoreExceptionResponseAsync(exception as CoreException, httpContext);
            }
            else if (exception is UnauthorizedAccessException)
            {
                if (!hasConfiguration)
                    httpContext.Response.StatusCode = 403;

                return await GenerateUnauthorizadeExceptionResponseAsync(httpContext);
            }
            else
            {
                if (!hasConfiguration)
                    httpContext.Response.StatusCode = 500;

                return await GenerateInternalErrorResponseAsync(exception, httpContext);
            }
        }

        private async Task<int> GenerateCoreExceptionResponseAsync(CoreException e, HttpContext httpContext)
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
                Exception = (IsDevelopmentEnvironment() ? exception : null)
            };

            await GenerateResponseAsync( internalError, httpContext);

            return httpContext.Response.StatusCode;
        }

        private async Task GenerateResponseAsync(object output, HttpContext httpContext)
        {
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

        private Task<(int statusCode, Exception exception, bool hasConfiguration)> ValidateConfigurationsAsync(int statusCode, Exception e)
        {
            Exception exception = e;
            int finalStatusCode = statusCode;
            bool hasConfiguration = false;
            //Executar eventos
            if (configuration != null)
            {
                if (configuration.Events.Any())
                {
                    hasConfiguration = true;

                    foreach (var @event in configuration.Events)
                    {
                        if (@event.IsElegible(statusCode, e))
                            (finalStatusCode, exception) = @event.Intercept(statusCode, e);
                    }
                }

                if (configuration.HasBehaviors)
                {
                    hasConfiguration = true;

                    var behaviorResult = configuration.ValidateBehavior(e);

                    if (behaviorResult != null)
                    {
                        switch (behaviorResult.Behavior)
                        {
                            case ExceptionHandlerBehavior.ServerError:
                                exception = e.GetBaseException();
                                break;
                            case ExceptionHandlerBehavior.ClientError:
                            default:
                                break;
                        }

                        finalStatusCode = behaviorResult.StatusCode;
                    }
                }
            }

            return Task.FromResult((finalStatusCode, exception, hasConfiguration));

        }
    }
}
