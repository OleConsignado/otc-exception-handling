using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling.Abstractions
{
    public interface IExceptionSerializer
    {
        Task<string> SerializeAsync(object output, HttpContext httpContext);
    }
}
