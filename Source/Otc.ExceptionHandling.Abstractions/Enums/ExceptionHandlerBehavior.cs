using System;
using System.Collections.Generic;
using System.Text;

namespace Otc.ExceptionHandling.Abstractions.Enums
{
    /// <summary>
    /// Add behaviors for an exception. 
    /// Expose - Log and returns the entire exception.
    /// Suppress - Log and returns only the base exception.
    /// Ignore - Ignore the entire exception.
    /// </summary>
    public enum ExceptionHandlerBehavior
    {
        Expose = 1,
        Suppress = 2,
        Ignore = 3
    }
}
