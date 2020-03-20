using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Otc.ExceptionHandling.Tests
{   
    public class ExceptionHandlerTests
    {
        private readonly IExceptionHandler exceptionHandler;
        private readonly Mock<IHttpResponseWriter> httpResponseWriterMock;

        public ExceptionHandlerTests()
        {
            httpResponseWriterMock = new Mock<IHttpResponseWriter>();
            exceptionHandler = new ExceptionHandler(new LoggerFactory(), httpResponseWriterMock.Object);
        }

        [SuppressMessage("Blocker Code Smell",
            "S2699:Tests should include assertions",
            Justification = "Assertion on ExceptionHandlerTestHelper")]
        [Theory]
        [InlineData(typeof(Exception), 500)]
        [InlineData(typeof(TestCoreException), 400)]
        [InlineData(typeof(UnauthorizedAccessException), 403)]
        public async Task RegularException_Success(Type exceptionType, int expectedStatusCode) 
        {
            await ExceptionHandlerTestHelper((Exception)Activator.CreateInstance(exceptionType), expectedStatusCode);
        }

        [SuppressMessage("Blocker Code Smell",
            "S2699:Tests should include assertions",
            Justification = "Assertion on ExceptionHandlerTestHelper")]
        [Fact]
        public async Task AggException_500_Success()
        {
            var aggException = new AggregateException(new Exception());
            await ExceptionHandlerTestHelper(aggException, 500);
        }

        [SuppressMessage("Blocker Code Smell",
            "S2699:Tests should include assertions",
            Justification = "Assertion on ExceptionHandlerTestHelper")]
        [Fact]
        public async Task AggException_400_Success()
        {
            var aggException = new AggregateException(new TestCoreException());
            await ExceptionHandlerTestHelper(aggException, 400);
        }

        private async Task ExceptionHandlerTestHelper(Exception exception, int expectedStatusCode)
        {
            await exceptionHandler.HandleExceptionAsync(exception);

            httpResponseWriterMock.SetupSet(t => t.StatusCode = It.IsAny<int>()).Callback<int>(statusCode =>
            {
                Assert.Equal(expectedStatusCode, statusCode);
            });

            httpResponseWriterMock.VerifySet(t => t.StatusCode = It.IsAny<int>(), Times.Once);
        }
    }
}

