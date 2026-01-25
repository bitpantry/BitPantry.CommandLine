using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Remote.SignalR.Client;
using Moq;

namespace BitPantry.CommandLine.Tests.Infrastructure.Helpers
{
    /// <summary>
    /// Factory for creating Mock&lt;IServerProxy&gt; instances with standard test configuration.
    /// Consolidates the common 4-line ServerCapabilities setup used across all test files.
    /// </summary>
    public static class TestServerProxyFactory
    {
        /// <summary>
        /// Creates a connected server proxy mock with default capabilities.
        /// </summary>
        /// <param name="baseUrl">Base URL for the server. Defaults to https://localhost:5000</param>
        /// <param name="maxUploadSize">Max upload size in bytes. Defaults to 100MB.</param>
        /// <returns>A configured Mock&lt;IServerProxy&gt; in Connected state.</returns>
        public static Mock<IServerProxy> CreateConnected(
            string baseUrl = "https://localhost:5000",
            long maxUploadSize = 100 * 1024 * 1024)
        {
            var proxyMock = new Mock<IServerProxy>();
            ConfigureConnected(proxyMock, baseUrl, maxUploadSize);
            return proxyMock;
        }

        /// <summary>
        /// Configures an existing proxy mock as connected with standard capabilities.
        /// Use this when you need to configure additional setups on the mock.
        /// </summary>
        /// <param name="proxyMock">The proxy mock to configure.</param>
        /// <param name="baseUrl">Base URL for the server. Defaults to https://localhost:5000</param>
        /// <param name="maxUploadSize">Max upload size in bytes. Defaults to 100MB.</param>
        public static void ConfigureConnected(
            Mock<IServerProxy> proxyMock,
            string baseUrl = "https://localhost:5000",
            long maxUploadSize = 100 * 1024 * 1024)
        {
            proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            proxyMock.Setup(p => p.Server).Returns(new ServerCapabilities(
                new Uri(baseUrl),
                "test-connection-id",
                new List<CommandInfo>(),
                maxUploadSize));
        }

        /// <summary>
        /// Creates a disconnected server proxy mock.
        /// </summary>
        /// <returns>A configured Mock&lt;IServerProxy&gt; in Disconnected state.</returns>
        public static Mock<IServerProxy> CreateDisconnected()
        {
            var proxyMock = new Mock<IServerProxy>();
            proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            return proxyMock;
        }

        /// <summary>
        /// Creates a connecting server proxy mock.
        /// </summary>
        /// <returns>A configured Mock&lt;IServerProxy&gt; in Connecting state.</returns>
        public static Mock<IServerProxy> CreateConnecting()
        {
            var proxyMock = new Mock<IServerProxy>();
            proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connecting);
            return proxyMock;
        }
    }
}
