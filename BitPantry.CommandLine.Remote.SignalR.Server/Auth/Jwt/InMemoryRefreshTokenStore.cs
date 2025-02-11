using BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();

    public Task StoreTokenAsync(RefreshToken token)
    {
        _tokens[token.Token] = token;
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetTokenAsync(string token)
    {
        _tokens.TryGetValue(token, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task InvalidateTokenAsync(string token)
    {
        if (_tokens.ContainsKey(token))
        {
            _tokens[token].IsRevoked = true;
        }
        return Task.CompletedTask;
    }
}
