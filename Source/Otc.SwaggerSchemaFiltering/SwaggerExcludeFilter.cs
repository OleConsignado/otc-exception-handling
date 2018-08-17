using Otc.DomainBase.Exceptions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Otc.SwaggerSchemaFiltering
{
    public class SwaggerExcludeFilter : ISchemaFilter
    {
        private static readonly HashSet<string> ignoreProperties = new HashSet<string>(
            typeof(Exception).GetProperties().Where(p => p.Name != nameof(Exception.Message))
                .Select(p => p.Name)
        );

        public void Apply(Schema model, SchemaFilterContext context)
        {
            if (context.SystemType != null && model.Properties != null && model.Properties.Count > 0)
            {
                // Remove as propriedades do tipo base Exception, exceto Message
                if (typeof(Exception).IsAssignableFrom(context.SystemType))
                {
                    foreach (var ignoreProperty in ignoreProperties)
                    {
                        var keyCheck = model.Properties
                            .Keys.Where(k => k.ToLowerInvariant() == ignoreProperty.ToLowerInvariant());

                        if (keyCheck.Any() && keyCheck.Count() == 1)
                        {
                            model.Properties.Remove(keyCheck.Single());
                        }
                    }
                }
                // Remove a propriedade Exception do tipo InternalError
                else if (typeof(InternalError).IsAssignableFrom(context.SystemType))
                {
                    var keyCheck = model.Properties
                        .Keys.Where(k => k.ToLowerInvariant() == nameof(InternalError.Exception).ToLowerInvariant());

                    if (keyCheck.Any() && keyCheck.Count() == 1)
                    {
                        model.Properties.Remove(keyCheck.Single());
                    }
                }
            }
        }
    }
}
