using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Otc.DomainBase.Exceptions;
using Otc.ExceptionHandling.Abstractions;
using Otc.ExceptionHandling.Tests;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Otc.ExceptionHandling.Tests
{
    public class ExceptionHandlerEventTest
    {
        IServiceProvider serviceProvider;
        HttpContext httpContext;
        public ExceptionHandlerEventTest()
        {
            var httpContextMock = new Mock<HttpContext>();
            var response = new Mock<Response>();

            response.Setup(_ => _.Body.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((byte[] data, int offset, int length, CancellationToken token) =>
            {

            })
            .Returns(() => Task.CompletedTask);

            httpContextMock.Setup(x => x.Response).Returns(() => response.Object);

            this.httpContext = httpContextMock.Object;
        }

        [Fact]
        [Trait("ForException", "Success")]
        public async Task ForException_Typed_Success()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddExceptionHandling();
            serviceCollection.AddExceptionHandlingConfiguration(config =>
                config.ForException<NullReferenceException>(405, Abstractions.Enums.ExceptionHandlerBehavior.Suppress)
                );

            serviceCollection.AddScoped<ILoggerFactory>(ctx => new LoggerFactory());

            serviceProvider = serviceCollection.BuildServiceProvider();

            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            var statusCode = await exceptionHandler.HandleExceptionAsync(new NullReferenceException(), httpContext);

            Assert.Equal(405, statusCode);
        }

        [Fact]
        [Trait("ForException", "Success")]
        public async Task ForException_String_Success()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddExceptionHandling();
            serviceCollection.AddExceptionHandlingConfiguration(config =>
                config.ForException("NullReferenceException", 411, Abstractions.Enums.ExceptionHandlerBehavior.Suppress)
                );

            serviceCollection.AddScoped<ILoggerFactory>(ctx => new LoggerFactory());

            serviceProvider = serviceCollection.BuildServiceProvider();

            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            var statusCode = await exceptionHandler.HandleExceptionAsync(new NullReferenceException(), httpContext);

            Assert.Equal(411, statusCode);
        }

        [Fact]
        [Trait("AddEvent", "Success")]
        public async Task AddEvent_String_Success()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddExceptionHandling();
            serviceCollection.AddExceptionHandlingConfiguration(config =>
                    config.AddEvent<ExceptionEvent>()
                );

            serviceCollection.AddScoped<ILoggerFactory>(ctx => new LoggerFactory());

            serviceProvider = serviceCollection.BuildServiceProvider();

            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            var statusCode = await exceptionHandler.HandleExceptionAsync(new NullReferenceException(), httpContext);

            Assert.Equal(200, statusCode);
        }

        [Fact]
        [Trait("ExceptionHandler_Unauthorized", "Success")]
        public async Task ExceptionHandler_Unauthorized_Success()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddExceptionHandling();

            serviceCollection.AddScoped<ILoggerFactory>(ctx => new LoggerFactory());

            serviceProvider = serviceCollection.BuildServiceProvider();

            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            var statusCode = await exceptionHandler.HandleExceptionAsync(new UnauthorizedAccessException(), httpContext);

            Assert.Equal(403, statusCode);
        }

        [Fact]
        [Trait("ExceptionHandler_CoreException", "Success")]
        public async Task ExceptionHandler_CoreException_Success()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddExceptionHandling();

            serviceCollection.AddScoped<ILoggerFactory>(ctx => new LoggerFactory());

            serviceProvider = serviceCollection.BuildServiceProvider();

            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            var statusCode = await exceptionHandler.HandleExceptionAsync(new DomainException(), httpContext);

            Assert.Equal(400, statusCode);
        }

        [Fact]
        [Trait("ExceptionHandler_Exception", "Success")]
        public async Task ExceptionHandler_Exception_Success()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddExceptionHandling();

            serviceCollection.AddScoped<ILoggerFactory>(ctx => new LoggerFactory());

            serviceProvider = serviceCollection.BuildServiceProvider();

            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            var statusCode = await exceptionHandler.HandleExceptionAsync(new Exception(), httpContext);

            Assert.Equal(500, statusCode);
        }


        [Fact]
        [Trait("ExceptionHandler_AggregateException", "Success")]
        public async Task ExceptionHandler_AggregateException_Success()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddExceptionHandling();

            serviceCollection.AddScoped<ILoggerFactory>(ctx => new LoggerFactory());

            serviceProvider = serviceCollection.BuildServiceProvider();

            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            var aggEx = new AggregateException(new Exception(), new DomainException());

            var statusCode = await exceptionHandler.HandleExceptionAsync(aggEx, httpContext);

            Assert.Equal(400, statusCode);
        }
    }

    public class DomainException : CoreException
    {
        public DomainException() : base("erro")
        {

        }
        public override string Key => "DomainException";
    }

    public class ExceptionEvent : IExceptionHandlerEvent
    {
        public (int statusCode, Exception exception) Intercept(int statusCode, Exception exception)
        {
            return (200, new Exception("nova exception"));
        }

        public bool IsElegible(int statusCode, Exception exception)
        {
            return exception is NullReferenceException;
        }
    }

    public class Response : HttpResponse
    {
        public override HttpContext HttpContext { get; }

        private int statusCode = 500;
        public override int StatusCode { get => statusCode; set => statusCode = value; }

        public override IHeaderDictionary Headers { get; }

        public override Stream Body { get; set; }
        public override long? ContentLength { get; set; }
        public override string ContentType { get; set; }

        public override IResponseCookies Cookies {get;}

        public override bool HasStarted => true;

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public override void Redirect(string location, bool permanent)
        {
            throw new NotImplementedException();
        }

    }

   
}

namespace Otc.ExceptionHandling
{
    public static class HttpResponseWritingExtensions
    {
        public static Task WriteAsync(this Response response, string text, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}
