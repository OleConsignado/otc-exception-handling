using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Otc.DomainBase.Exceptions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling
{
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly ILogger logger;
        private readonly IHttpResponseWriter httpResponseWriter;

        public ExceptionHandler(ILoggerFactory loggerFactory, IHttpResponseWriter httpResponseWriter)
        {
            logger = loggerFactory?.CreateLogger<ExceptionHandler>() ??
                throw new ArgumentNullException(nameof(loggerFactory));
            this.httpResponseWriter = httpResponseWriter ?? 
                throw new ArgumentNullException(nameof(httpResponseWriter));
        }

        public async Task HandleExceptionAsync(Exception exception)
        {
            if (exception is AggregateException)
            {
                var aggregateException = exception as AggregateException;

                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    await HandleExceptionAsync(innerException);
                }
            }
            else
            {
                if (exception is UnauthorizedAccessException)
                {
                    httpResponseWriter.StatusCode = 403;
                    await GenerateUnauthorizadeExceptionResponseAsync();
                }
                else if (exception is CoreException)
                {
                    httpResponseWriter.StatusCode = 400;
                    await GenerateCoreExceptionResponseAsync(exception);
                }
                else
                {
                    httpResponseWriter.StatusCode = 500;
                    await GenerateInternalErrorResponseAsync(exception);
                }
            }
        }

        private async Task GenerateCoreExceptionResponseAsync(Exception e)
        {
            logger.LogInformation(e, "Ocorreu um erro de negócio.");

            await GenerateResponseAsync(e);
        }

        /// <summary>
        /// Retorna um httpStatusCode 401.
        /// </summary>
        /// <param name="e">Inner Exception</param>
        /// <param name="httpContext">HttpContext</param>
        /// <returns></returns>
        private async Task GenerateUnauthorizadeExceptionResponseAsync()
        {
            logger.LogInformation("Ocorreu um acesso não autorizado.");

            var forbidden = new
            {
                Key = "Forbidden",
                Message = "Access to this resource is forbidden."
            };

            await GenerateResponseAsync(forbidden);
        }

        private bool IsDevelopmentEnvironment()
            => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        private async Task GenerateInternalErrorResponseAsync(Exception e)
        {
            Exception exception = e;
            Guid logEntryId = Guid.NewGuid();
            
            logger.LogError(e, "{LogEntryId}: Ocorreu um erro não esperado.", logEntryId);           

            var internalError = new InternalError()
            {
                LogEntryId = logEntryId,
                Exception = (IsDevelopmentEnvironment() ? exception.GetBaseException() : null)
            };

            await GenerateResponseAsync(internalError);
        }

        private async Task GenerateResponseAsync(object output)
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

            httpResponseWriter.ContentType = "application/json";
            await httpResponseWriter.WriteAsync(message, Encoding.UTF8);
        }
    }
}
