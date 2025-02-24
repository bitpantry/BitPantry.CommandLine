using Microsoft.AspNetCore.TestHost;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    internal static class CommandLineApplicationExtensions
    {
        internal static async Task ConnectToServer(this CommandLineApplication app, TestServer server, string hubPath)
        {
            var hubUri = $"{server.BaseAddress.AbsoluteUri.TrimEnd('/')}/{hubPath.TrimStart('/')}";

            await app.Run($"server.connect -u {hubUri}");
        }

        internal static async Task ConnectToServer(this CommandLineApplication app, TestServer server, string hubPath = "/cli", string tokenRequestPath = "/cli-auth/token-request", string apiKey = "key1")
        {
            var hubUri = $"{server.BaseAddress.AbsoluteUri.TrimEnd('/')}/{hubPath.TrimStart('/')}";
            var tokenRequestUri = $"{server.BaseAddress.AbsoluteUri.TrimEnd('/')}/{tokenRequestPath.TrimStart('/')}";

            await app.Run($"server.connect -u {hubUri} -k {apiKey} -e {tokenRequestPath}");
        }

    }
}
