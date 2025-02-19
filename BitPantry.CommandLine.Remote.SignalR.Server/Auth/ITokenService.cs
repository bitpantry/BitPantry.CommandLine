using System.Security.Claims;

public interface ITokenService
{
    string GenerateAccessToken(string clientId, List<Claim> claims = null);
    Task<string> GenerateRefreshToken(string clientId);
    Task<Tuple<bool, ClaimsPrincipal>> ValidateToken(string token);
}