using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHttpResponseWriter
    {
        int StatusCode { get; set; }
        string ContentType { get; set; }
        Task WriteAsync(string message, Encoding encoding);
    }
}
