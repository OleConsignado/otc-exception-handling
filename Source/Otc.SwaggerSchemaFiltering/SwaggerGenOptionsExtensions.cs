using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Otc.SwaggerSchemaFiltering
{
    public static class SwaggerGenOptionsExtensions
    {
        public static void ApplyOtcDomainBaseExceptionSchemaFilter(this SwaggerGenOptions swaggerGenOptions)
        {
            swaggerGenOptions.SchemaFilter<RemoveExceptionPropertiesSchemaFilter>();
            swaggerGenOptions.DocumentFilter<RemoveExceptionPropertiesDocumentFilter>();
        }
    }
}
