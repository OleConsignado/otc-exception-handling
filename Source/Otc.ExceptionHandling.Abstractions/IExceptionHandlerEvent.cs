using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Otc.ExceptionHandling.Abstractions
{
    public interface IExceptionHandlerEvent
    {
        bool IsElegible(int statusCode, Exception exception);
        (int statusCode, Exception exception) Intercept(int statusCode, Exception exception);
    }
}
