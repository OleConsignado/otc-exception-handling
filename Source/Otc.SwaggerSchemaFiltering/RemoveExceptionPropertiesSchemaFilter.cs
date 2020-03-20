using Microsoft.OpenApi.Models;
using Otc.DomainBase.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Otc.SwaggerSchemaFiltering
{
    internal class RemoveExceptionPropertiesSchemaFilter : ISchemaFilter
    {
        private static readonly IEnumerable<string> ignoreProperties = new List<string>(
            typeof(Exception).GetProperties().Where(p => p.Name != nameof(Exception.Message))
                .Select(p => p.Name)
        );

        // Sanetize Exception and InternalError types. 
        // For Exception, only Message property is being kept.
        // For InternalError, Exception property is being removed.
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type != null && schema.Properties != null && schema.Properties.Count > 0)
            {
                // Remove all Exception properties, but Message
                if (typeof(Exception).IsAssignableFrom(context.Type))
                {
                    foreach (var ignoreProperty in ignoreProperties)
                    {
                        var keyCheck = schema.Properties
                            .Keys.Where(k => k.ToLowerInvariant() == ignoreProperty.ToLowerInvariant());

                        if (keyCheck.Any() && keyCheck.Count() == 1)
                        {
                            schema.Properties.Remove(keyCheck.Single());
                        }
                    }
                }
                // Remove Exception property from InternalError
                else if (typeof(InternalError).IsAssignableFrom(context.Type))
                {
                    var keyCheck = schema.Properties
                        .Keys.Where(k => k.ToLowerInvariant() == nameof(InternalError.Exception).ToLowerInvariant());

                    if (keyCheck.Any() && keyCheck.Count() == 1)
                    {
                        schema.Properties.Remove(keyCheck.Single());
                    }
                }
            }
        }
    }
}
