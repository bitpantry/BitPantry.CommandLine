using BitPantry.CommandLine.Remote.SignalR.Server.Authentication;
using System.Collections.Concurrent;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment;

public class TestRefreshTokenStore : IRefreshTokenStore
{
    // Static ConcurrentDictionary shared across all transient instances
    // Thread-safe to handle parallel test execution and concurrent HTTP requests
    private static readonly ConcurrentDictionary<string, string> _refreshTokens = new ConcurrentDictionary<string, string>();

    public Task StoreRefreshTokenAsync(string clientId, string refreshToken)
    {
        _refreshTokens[clientId] = refreshToken;
        return Task.CompletedTask;
    }

    public Task<bool> TryGetRefreshTokenAsync(string clientId, out string refreshToken)
    {
        refreshToken = null;

        if (_refreshTokens.TryGetValue(clientId, out var token))
        {
            refreshToken = token;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task RevokeRefreshTokenAsync(string clientId)
    {
        _refreshTokens.TryRemove(clientId, out _);
        return Task.CompletedTask;
    }
}
