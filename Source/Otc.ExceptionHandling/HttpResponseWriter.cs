using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling
{
    public class HttpResponseWriter : IHttpResponseWriter
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpResponseWriter(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? 
                throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public int StatusCode { get; set; }
        public string ContentType { get; set; }

        public Task WriteAsync(string message, Encoding encoding)
        {
            var response = httpContextAccessor.HttpContext.Response;
            response.StatusCode = StatusCode;
            response.ContentType = ContentType;
            return response.WriteAsync(message, encoding);
        }
    }
}
