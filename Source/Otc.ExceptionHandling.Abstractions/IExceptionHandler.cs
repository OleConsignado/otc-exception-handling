﻿using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling.Abstractions
{
    public interface IExceptionHandler
    {
        Task<int> HandleExceptionAsync(Exception exception, HttpContext httpContext);
    }
}
