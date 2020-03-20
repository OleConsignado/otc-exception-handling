using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling
{
    public interface IExceptionHandler
    {
        Task HandleExceptionAsync(Exception exception);
    }
}
