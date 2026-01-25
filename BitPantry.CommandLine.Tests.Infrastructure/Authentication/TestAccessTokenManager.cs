using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.CommandLine.Tests.Infrastructure.Http;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitPantry.CommandLine.Tests.Infrastructure.Authentication
{
    public static class TestAccessTokenManager
    {
        public static AccessTokenManager Create(HttpResponseMessage tokenServiceReturns)
            => Create(TimeSpan.FromMinutes(1), TimeSpan.Zero, tokenServiceReturns);

        public static AccessTokenManager Create(TimeSpan tokenRefreshThreshold, HttpResponseMessage tokenServiceReturns)
            => Create(TimeSpan.FromMinutes(1), tokenRefreshThreshold, tokenServiceReturns);

        public static AccessTokenManager Create(TimeSpan tokenRefreshMonitorInterval, TimeSpan tokenRefreshThreshold, HttpResponseMessage tokenServiceReturns)
            => new(
                new Mock<ILogger<AccessTokenManager>>().Object,
                new SingletonHttpClientFactory(TestHttpClient.Create(tokenServiceReturns)),
                new CommandLineClientSettings(tokenRefreshMonitorInterval, tokenRefreshThreshold));
    }
}



