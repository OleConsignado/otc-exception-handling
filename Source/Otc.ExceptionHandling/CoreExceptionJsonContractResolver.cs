using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;

namespace Otc.ExceptionHandling
{

    /// <summary>
    /// Ignora as propriedades que sao da classe Exception, exceto Message.
    /// </summary>
    internal class CoreExceptionJsonContractResolver : CamelCasePropertyNamesContractResolver
    {
        private static readonly HashSet<string> ignoreProperties = new HashSet<string>(
            typeof(Exception).GetProperties().Where(p => p.Name != nameof(Exception.Message))
                .Select(p => p.Name)
        );

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            
            if (member.MemberType == MemberTypes.Property)
            {
                var type = ((PropertyInfo)member).PropertyType;

                if (type.IsGenericType &&
                    type.GenericTypeArguments.Any(t => typeof(Expression).IsAssignableFrom(t)))
                {
                    property.Ignored = true;
                }
                else
                    if (typeof(Expression).IsAssignableFrom(type))
                        property.Ignored = true;
            }

            if (ignoreProperties.Contains(member.Name))
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
