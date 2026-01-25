using Microsoft.AspNetCore.TestHost;

namespace BitPantry.CommandLine.Tests.Infrastructure.Http
{
    public class TestHttpMessageHandlerFactory : CommandLine.Remote.SignalR.Client.IHttpMessageHandlerFactory
    {
        private TestServer _svr;

        public TestHttpMessageHandlerFactory(TestServer svr) { _svr = svr; }

        public HttpMessageHandler CreateHandler(HttpMessageHandler handler)
            => new TestHttpMessageHandler(_svr.CreateHandler());
    }
}
