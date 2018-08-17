using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Otc.ExceptionHandling.Abstractions;
using System;

namespace Otc.Mvc.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        private readonly IExceptionHandler exceptionHandler;
        private readonly ILogger logger;

        public ExceptionFilter(IExceptionHandler exceptionHandler, ILoggerFactory loggerFactory)
        {
            this.exceptionHandler = exceptionHandler;
            
            if(exceptionHandler == null)
                throw new ArgumentNullException(nameof(exceptionHandler));

            logger = loggerFactory?.CreateLogger<ExceptionFilter>();

            if (logger == null)
                throw new ArgumentNullException(nameof(loggerFactory));
        }

        public void OnException(ExceptionContext context)
        {
            try
            {
                exceptionHandler.HandleExceptionAsync(context.Exception, context.HttpContext).Wait();
                context.ExceptionHandled = true;
            }
            catch (Exception e)
            {
                logger.LogCritical(0, e, $"Provavelmente existe um BUG na biblioteca que implementa '{typeof(IExceptionHandler).FullName}'. Verifique a excecao logada para obter mais detalhes.");
            }
        }
    }
}