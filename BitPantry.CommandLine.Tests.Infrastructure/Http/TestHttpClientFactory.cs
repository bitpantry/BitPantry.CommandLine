using Microsoft.AspNetCore.TestHost;

namespace BitPantry.CommandLine.Tests.Infrastructure.Http
{
    public class TestHttpClientFactory : CommandLine.Remote.SignalR.Client.IHttpClientFactory
    {
        private TestServer _svr;

        public TestHttpClientFactory(TestServer svr) { _svr = svr; }

        public HttpClient CreateClient()
            => _svr.CreateClient();
    }
}
