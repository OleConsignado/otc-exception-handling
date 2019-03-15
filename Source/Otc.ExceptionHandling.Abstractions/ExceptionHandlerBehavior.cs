using System;
using System.Collections.Generic;
using System.Text;

namespace Otc.ExceptionHandling.Abstractions
{
    /// <summary>
    /// Add behaviors for an exception.
    /// </summary>
    public enum ExceptionHandlerBehavior
    {
        /// <summary>
        /// Expose entire exception on HttpOutput and log it as information level.
        /// </summary>
        ClientError = 1,
        /// <summary>
        /// Generates an identifier for exception and suppress it on HttpOutput by exposing only the identifier. 
        /// Log it as error level with the same identifier.
        /// </summary>
        ServerError = 2
    }
}
