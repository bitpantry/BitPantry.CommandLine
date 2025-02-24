using BitPantry.CommandLine.Remote.SignalR.Server.Authentication;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment;

public class TestRefreshTokenStore : IRefreshTokenStore
{
    private static readonly Dictionary<string, string> _refreshTokens = new Dictionary<string, string>();

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
        _refreshTokens.Remove(clientId);
        return Task.CompletedTask;
    }
}
