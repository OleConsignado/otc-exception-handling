using Otc.ExceptionHandling.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Otc.ExceptionHandling.Abstractions.Configurations
{
    public class ForExceptionBehavior
    {
        public int StatusCode { get; set; }
        public ExceptionHandlerBehavior Behavior { get; set; }
    }
}
