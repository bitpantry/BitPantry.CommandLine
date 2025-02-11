using BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenStore _tokenStore;

    public RefreshTokenService(IRefreshTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public async Task<RefreshToken> GenerateRefreshToken(string userId)
    {
        var refreshToken = new RefreshToken
        {
            Token = GenerateSecureToken(),
            UserId = userId,
            Expires = DateTime.UtcNow.AddDays(7), // Token expires in 7 days
            IsRevoked = false
        };

        await _tokenStore.StoreTokenAsync(refreshToken);
        return refreshToken;
    }

    public async Task<RefreshToken?> GetStoredRefreshToken(string token)
    {
        return await _tokenStore.GetTokenAsync(token);
    }

    public async Task InvalidateRefreshToken(string token)
    {
        await _tokenStore.InvalidateTokenAsync(token);
    }

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}
