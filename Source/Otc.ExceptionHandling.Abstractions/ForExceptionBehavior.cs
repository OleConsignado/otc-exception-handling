using Otc.ExceptionHandling.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Otc.ExceptionHandling.Abstractions
{
    public class ForExceptionBehavior
    {
        public int StatusCode { get; set; }
        public ExceptionHandlerBehavior Behavior { get; set; }
    }
}
