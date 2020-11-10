using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Otc.ExceptionHandling.Abstractions;
using System;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling
{
    public class ExceptionSerializer : IExceptionSerializer
    {
        public Task<string> SerializeAsync(object output, HttpContext httpContext)
        {
            var jsonSerializerSettings = GetJsonSerializerSettings();
            jsonSerializerSettings.ContractResolver = GetContractResolver();
            var message = JsonConvert.SerializeObject(output, jsonSerializerSettings);

            return Task.FromResult(message);
        }

        protected virtual JsonSerializerSettings GetJsonSerializerSettings()
        {
            return new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MaxDepth = 10,
                Formatting = IsDevelopmentEnvironment() ? Formatting.Indented : Formatting.None
            };
        }

        protected virtual CamelCasePropertyNamesContractResolver GetContractResolver()
        {
            return new CoreExceptionJsonContractResolver()
            {
                IgnoreSerializableInterface = true
            };
        }

        protected bool IsDevelopmentEnvironment()
            => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
}
