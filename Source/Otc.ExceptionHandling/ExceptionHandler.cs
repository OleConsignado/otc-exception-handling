using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Otc.DomainBase.Exceptions;
using Otc.ExceptionHandling.Abstractions;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling
{
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly ILogger logger;

        public ExceptionHandler(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory?.CreateLogger<ExceptionHandler>();

            if (logger == null)
                throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task HandleExceptionAsync(Exception exception, HttpContext httpContext)
        {
            if (exception is AggregateException)
            {
                var aggregateException = exception as AggregateException;

                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    await HandleExceptionAsync(innerException, httpContext);
                }
            }
            else if (exception is CoreException)
            {
                await GenerateCoreExceptionResponseAsync(exception as CoreException, httpContext);
            }
            else if (exception is UnauthorizedAccessException)
            {
                await GenerateUnauthorizadeExceptionResponseAsync(httpContext);
            }
            else
            {
                await GenerateInternalErrorResposeAsync(exception, httpContext);
            }
        }

        private async Task GenerateCoreExceptionResponseAsync(CoreException e, HttpContext httpContext)
        {
            logger.LogInformation(e, "Ocorreu um erro de negócio.");

            await GenerateResponse(400, e, httpContext);
        }

        /// <summary>
        /// Retorna um httpStatusCode 401.
        /// </summary>
        /// <param name="e">Inner Exception</param>
        /// <param name="httpContext">HttpContext</param>
        /// <returns></returns>
        private async Task GenerateUnauthorizadeExceptionResponseAsync(HttpContext httpContext)
        {
            logger.LogInformation("Ocorreu um acesso não autorizado.");

            var forbidden = new
            {
                Key = "Forbidden",
                Message = "Access to this resource is forbidden."
            };

            await GenerateResponse(403, forbidden, httpContext);
        }

        private bool IsDevelopmentEnvironment()
            => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        private async Task GenerateInternalErrorResposeAsync(Exception e, HttpContext httpContext)
        {
            var internalError = new InternalError()
            {
                LogEntryId = Guid.NewGuid(),
                Exception = IsDevelopmentEnvironment() ? e : null
            };

            logger.LogError(e, "{LogEntryId}: Ocorreu um erro não esperado.", internalError.LogEntryId);

            await GenerateResponse(500, internalError, httpContext);
        }

        private async Task GenerateResponse(int statusCode, object output, HttpContext httpContext)
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
                Formatting = !IsDevelopmentEnvironment() ? Formatting.None : Formatting.Indented
            };


            var message = JsonConvert.SerializeObject(output, jsonSerializerSettings);

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(message, Encoding.UTF8);
        }
    }
}
