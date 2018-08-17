using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Otc.DomainBase.Exceptions;
using Otc.ExceptionHandling.Abstractions;
using System;
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
            else
            {
                await GenerateInternalErrorResposeAsync(exception, httpContext);
            }
        }

        private async Task GenerateCoreExceptionResponseAsync(CoreException e, HttpContext httpContext)
        {
            logger.LogInformation(0, e, "Ocorreu um erro de negócio.");

            await GenerateResponse(400, e, httpContext);
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

            logger.LogError(0, e, "{LogEntryId}: Ocorreu um erro não esperado.", internalError.LogEntryId);

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
