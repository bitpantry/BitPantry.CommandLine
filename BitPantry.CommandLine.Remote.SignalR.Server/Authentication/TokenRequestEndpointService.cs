using Microsoft.AspNetCore.Http;
using System.Security.Claims;

/// <summary>
/// Contains access token request and refresh end points configured as HTTP services for clients
/// </summary>
public class TokenRequestEndpointService
{
    private ITokenService _tokenSvc;
    private ApiKeyService _keySvc;

    public TokenRequestEndpointService(ITokenService tokenSvc, ApiKeyService keySvc)
    {
        _tokenSvc = tokenSvc;
        _keySvc = keySvc;
    }

    /// <summary>
    /// Handles a token HTTP request from the client.
    /// </summary>
    /// <param name="request">The token request, containing an API key for authentication</param>
    /// <param name="refreshRoute">The refresh route to be used to refresh a token if acquired</param>
    /// <returns>An HTTP result</returns>
    public async Task<IResult> HandleTokenRequest(TokenRequestModel request, string refreshRoute)
    {
        if (string.IsNullOrEmpty(request.ApiKey))
            return Results.BadRequest("API key is required.");

        // Validate the api key

        if (!await _keySvc.ValidateKey(request.ApiKey, out var clientId) || string.IsNullOrEmpty(clientId))
            return Results.Unauthorized();

        // Generate and return the token

        var accessToken = _tokenSvc.GenerateAccessToken(clientId);
        var refreshToken = await _tokenSvc.GenerateRefreshToken(clientId);

        return Results.Ok(new TokenResponseModel(accessToken, refreshToken, refreshRoute));
    }

    /// <summary>
    /// Handles a refresh token HTTP request from the client
    /// </summary>
    /// <param name="request">The request containing a refresh token</param>
    /// <returns>An HTTP result</returns>
    public async Task<IResult> HandleTokenRefreshRequest(TokenRefreshRequestModel request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return Results.BadRequest("Refresh token is required.");

        // Validate the refresh token

        var validationResult = await _tokenSvc.ValidateToken(request.RefreshToken);
        if (!validationResult.Item1)
            return Results.Unauthorized();

        var clientId = validationResult.Item2.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(clientId))
            return Results.Unauthorized();

        // Generate and return the new tokens

        var newAccessToken = _tokenSvc.GenerateAccessToken(clientId);
        var newRefreshToken = request.RefreshToken; // forces new log in after refresh token expires 

        return Results.Ok(new TokenResponseModel(newAccessToken, newRefreshToken, null));
    }
}
