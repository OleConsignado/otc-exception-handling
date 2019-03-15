using Otc.ExceptionHandling.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling.Abstractions
{
    public interface IExceptionHandlerConfigurationExpression
    {
        /// <summary>
        /// Get events registered on ExceptionHandler pipeline
        /// </summary>
        List<IExceptionHandlerEvent> Events { get; }

        /// <summary>
        /// Get behaviors registered on ExceptionHandler pipeline
        /// </summary>
        Dictionary<string, ForExceptionBehavior> Behaviors { get; }

        /// <summary>
        /// Add events to intercept exception and will execute on ExceptionHandler pipeline.
        /// </summary>
        /// <param name="event">New instance of an event class that implements IExceptionHandlerEvent</param>
        IExceptionHandlerConfigurationExpression AddEvent(IExceptionHandlerEvent @event);
        /// <summary>
        /// Add events to intercept exception and will execute on ExceptionHandler pipeline.
        /// </summary>
        /// <typeparam name="TEvent">Event class that implements IExceptionHandlerEvent</typeparam>
        IExceptionHandlerConfigurationExpression AddEvent<TEvent>() where TEvent : IExceptionHandlerEvent, new();

        /// <summary>
        /// Add an custom behavior for an exception that occurs on application.
        /// </summary>
        /// <typeparam name="TException">Type of the exception that will have custom behaviors.</typeparam>
        /// <param name="statusCode">Status code that will be returned for that exception.</param>
        /// <param name="behavior">Behavior for that exception. 
        /// Expose - Log and returns the entire exception.
        /// Suppress - Log and returns only the base exception.
        /// Ignore - Ignore the entire exception.</param>
        IExceptionHandlerConfigurationExpression ForException<TException>(int statusCode, ExceptionHandlerBehavior behavior = ExceptionHandlerBehavior.ClientError) 
            where TException : Exception;

        /// <summary>
        /// Add an custom behavior for an exception that occurs on application.
        /// </summary>
        /// <param name="exception">Type of the exception that will have custom behaviors.</param>
        /// <param name="statusCode">Status code that will be returned for that exception.</param>
        /// <param name="behavior">Behavior for that exception. 
        /// Expose - Log and returns the entire exception.
        /// Suppress - Log and returns only the base exception.
        /// Ignore - Ignore the entire exception.</param>
        IExceptionHandlerConfigurationExpression ForException(string exception, int statusCode, ExceptionHandlerBehavior behavior = ExceptionHandlerBehavior.ClientError);

    }
}
