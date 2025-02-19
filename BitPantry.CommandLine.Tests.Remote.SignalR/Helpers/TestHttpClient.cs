using Moq;
using Moq.Protected;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Helpers
{
    public static class TestHttpClient
    {
        public static HttpClient Create(HttpResponseMessage returns)
        {
            var handler = new Mock<HttpMessageHandler>();

            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(returns);

            return new HttpClient(handler.Object);
        }

    }
}
