using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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

            if (ignoreProperties.Contains(member.Name))
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
