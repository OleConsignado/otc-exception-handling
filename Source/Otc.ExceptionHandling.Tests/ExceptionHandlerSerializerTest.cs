using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Otc.DomainBase.Exceptions;
using Otc.ExceptionHandling.Abstractions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Otc.ExceptionHandling.Tests
{
    public class ExceptionHandlerSerializerTest
    {
        IServiceProvider serviceProvider;
        private IExceptionHandler CreateExceptionHandler(
            Action<IServiceCollection> configuration = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddExceptionHandling();
            configuration?.Invoke(serviceCollection);
            serviceCollection.AddScoped<ILoggerFactory>(ctx => new LoggerFactory());
            serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IExceptionHandler>();
        }

        private HttpContext CreateHttpContext(Action<string> asserts)
        {
            var httpContextMock = new Mock<HttpContext>();
            var response = new Mock<Response>();

            response.Setup(_ => _.Body.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback((byte[] data, int offset, int length, CancellationToken token) =>
                    asserts(Encoding.UTF8.GetString(data)))
                .Returns(() => Task.CompletedTask);

            response.SetupSet(p => p.StatusCode = It.IsAny<int>())
                .Callback<int>(c => response.SetupGet(p => p.StatusCode).Returns(c));

            httpContextMock.Setup(x => x.Response).Returns(() => response.Object);

            return httpContextMock.Object;
        }

        [Fact]
        public async Task Default_Serializer_ServerError_Success()
        {
            var exceptionHandler = CreateExceptionHandler();

            var httpContext = CreateHttpContext(output =>
            {
                Assert.Matches("\\{\"logEntryId\":\"[0-9a-f\\-]{36}\"\\}", output);
            });

            await exceptionHandler.HandleExceptionAsync(new NullReferenceException(), httpContext);
        }

        [Fact]
        public async Task Default_Serializer_ClientError_Success()
        {
            var exceptionHandler = CreateExceptionHandler();

            var httpContext = CreateHttpContext(output =>
            {
                Assert.Contains("{\"key\":\"DomainException\"," +
                    "\"errors\":[],\"message\":\"erro\"}", output);
            });

            await exceptionHandler.HandleExceptionAsync(new CustomCoreException(), httpContext);
        }

        [Fact]
        public async Task Custom_Serializer_ServerError_Success()
        {
            var exceptionHandler = CreateExceptionHandler(sc =>
            {
                sc.AddExceptionHandlingConfiguration(config =>
                    config.SetSerializer(() => new CustomExceptionSerializer()));
            });

            var httpContext = CreateHttpContext(output =>
            {
                output = output.Replace("\r", string.Empty);
                Assert.Matches("\\{\\\n  \"logEntryId\": " +
                    "\"[0-9a-f\\-]{36}\",\\\n  \"exception\": null\\\n\\}", output);
            });

            await exceptionHandler.HandleExceptionAsync(new NullReferenceException(), httpContext);
        }

        [Fact]
        public async Task Custom_Serializer_ClientError_Success()
        {
            var exceptionHandler = CreateExceptionHandler(sc =>
            {
                sc.AddExceptionHandlingConfiguration(config =>
                    config.SetSerializer(() => new CustomExceptionSerializer()));
            });

            var httpContext = CreateHttpContext(output =>
            {
                output = output.Replace("\r", string.Empty);
                Assert.Equal("{\n  \"key\": \"DomainException\",\n" +
                    "  \"errors\": [],\n  \"message\": \"erro\"\n}", output);

            });

            await exceptionHandler.HandleExceptionAsync(new CustomCoreException(), httpContext);
        }

        [Fact]
        public async Task Custom_Serializer_ClientError_WithEvent_Success()
        {
            var exceptionHandler = CreateExceptionHandler(sc =>
            {
                sc.AddExceptionHandlingConfiguration(config =>
                    config.SetSerializer(() => new CustomExceptionSerializer())
                        .AddEvent<CustomEvent>());
            });

            var httpContext = CreateHttpContext(output =>
            {
                output = output.Replace("\r", string.Empty);
                Assert.Matches("\\{\\\n  \"logEntryId\": " +
                    "\"[0-9a-f\\-]{36}\",\\\n  \"exception\": null\\\n\\}", output);
            });

            var statusCode = await exceptionHandler
                .HandleExceptionAsync(new CustomCoreException(), httpContext);

            Assert.Equal(200, statusCode);
        }
    }

    public class CustomCoreException : CoreException
    {
        public CustomCoreException() : base("erro")
        {
        }
        public override string Key => "DomainException";
    }

    public class CustomExceptionSerializer : ExceptionSerializer
    {
        protected override JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = base.GetJsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.NullValueHandling = NullValueHandling.Include;
            return settings;
        }
    }

    public class CustomEvent : IExceptionHandlerEvent
    {
        public (int statusCode, Exception exception, ExceptionHandlerBehavior behavior)
            Intercept(int statusCode, Exception exception)
        {
            return (200, new Exception("custom"), ExceptionHandlerBehavior.ServerError);
        }

        public bool IsElegible(int statusCode, Exception exception)
        {
            return exception is CustomCoreException;
        }
    }
}
