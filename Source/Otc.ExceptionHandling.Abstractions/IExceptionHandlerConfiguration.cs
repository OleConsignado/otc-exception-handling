using System;
using System.Collections.Generic;

namespace Otc.ExceptionHandling.Abstractions
{
    public interface IExceptionHandlerConfiguration
    {
        /// <summary>
        /// Registered events on configuration
        /// </summary>
        List<IExceptionHandlerEvent> Events { get; }

        /// <summary>
        /// Lambda function that returns an instance of <see cref="IExceptionSerializer"/>.
        /// </summary>
        Func<IExceptionSerializer> Serializer { get; }

        /// <summary>
        /// Validate if there is configuration for specific exceptions on exception handling setup
        /// </summary>
        /// <param name="ex">Exception to validate.</param>
        /// <returns>Object containing status code and the expected behavior when that exception occurs.</returns>
        ForExceptionBehavior ValidateBehavior(Exception ex);

        /// <summary>
        /// Validate if there is behaviors registereds.
        /// </summary>
        bool HasBehaviors { get; }
    }
}
