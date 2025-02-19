using BitPantry.CommandLine.Remote.SignalR.Server.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtTokenService : ITokenService
{
    private ILogger<JwtTokenService> _logger;
    private TokenAuthenticationSettings _tokenAuthSettings;
    private IRefreshTokenStore _refreshTokenStore;

    public JwtTokenService(ILogger<JwtTokenService> logger, TokenAuthenticationSettings tokenAuthSettings, IRefreshTokenStore refreshTokenStore) 
    {
        _logger = logger;
        _tokenAuthSettings = tokenAuthSettings;
        _refreshTokenStore = refreshTokenStore;
    }

    public string GenerateAccessToken(string clientId, List<Claim> claims = null)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));  

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenAuthSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        claims ??= new List<Claim>();
            
        claims.AddRange([
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim("role", "cli")
        ]);

        var token = new JwtSecurityToken(
            issuer: _tokenAuthSettings.Issuer,
            audience: _tokenAuthSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_tokenAuthSettings.AccessTokenLifetime),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshToken(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_tokenAuthSettings.Key);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, clientId),
                new Claim("token_type", "refresh")
            ]),
            Expires = DateTime.UtcNow.Add(_tokenAuthSettings.RefreshTokenLifetime),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _tokenAuthSettings.Issuer,
            Audience = _tokenAuthSettings.Audience,
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = tokenHandler.WriteToken(token);

        await _refreshTokenStore.StoreRefreshTokenAsync(clientId, refreshToken);

        return refreshToken;
    }

    public async Task<Tuple<bool, ClaimsPrincipal>> ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return new Tuple<bool, ClaimsPrincipal>(false, null);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenAuthSettings.Key));

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _tokenAuthSettings.Issuer,
                ValidAudience = _tokenAuthSettings.Audience,
                ClockSkew = _tokenAuthSettings.TokenValidationClockSkew,
                IssuerSigningKey = key
            }, out SecurityToken validatedToken);

            var tokenTypeClaim = principal.FindFirst("token_type")?.Value;
            if (tokenTypeClaim == "refresh")
            {
                var clientId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if(!await _refreshTokenStore.TryGetRefreshTokenAsync(clientId, out var storedToken) || storedToken != token)
                    return new Tuple<bool, ClaimsPrincipal>(false, null);
            }

            return new Tuple<bool, ClaimsPrincipal>(true, principal);
        }
        catch(SecurityTokenExpiredException)
        {
            return Tuple.Create(false, (ClaimsPrincipal)null);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occured while validating a token");
            return Tuple.Create(false, (ClaimsPrincipal)null);
        }
    }

    public async Task RevokeRefreshTokenAsync(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));

        await _refreshTokenStore.RevokeRefreshTokenAsync(clientId);
    }

    public async Task<string> RotateRefreshTokenAsync(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));

        await RevokeRefreshTokenAsync(clientId);
        return await GenerateRefreshToken(clientId);
    }
}
