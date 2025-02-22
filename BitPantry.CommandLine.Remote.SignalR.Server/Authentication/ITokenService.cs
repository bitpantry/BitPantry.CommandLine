using System.Security.Claims;

/// <summary>
/// Defines the token service required by the command line server authentication framework. 
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token for a given client with given claims
    /// </summary>
    /// <param name="clientId">The id of the client to generate the token for</param>
    /// <param name="claims">The claims to include in the token</param>
    /// <returns></returns>
    string GenerateAccessToken(string clientId, List<Claim> claims = null);

    /// <summary>
    /// Generates a refresh token for the given client
    /// </summary>
    /// <param name="clientId">The id of the client to generate a refresh token for</param>
    /// <returns>A new refresh token</returns>
    Task<string> GenerateRefreshToken(string clientId);

    /// <summary>
    /// Validates a token and returns a <see cref="ClaimsPrincipal"/> represented by the token
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>A <see cref="Tuple{bool, ClaimsPrincipal}"/> where Item1 is true if the token is valid and false otherwise. If Item1 
    /// is false, Item2, the <see cref="ClaimsPrincipal"/> should be considered invalid</returns>
    Task<Tuple<bool, ClaimsPrincipal>> ValidateToken(string token);
}