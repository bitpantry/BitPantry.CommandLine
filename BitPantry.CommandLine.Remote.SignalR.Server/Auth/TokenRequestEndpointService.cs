using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class TokenRequestEndpointService
{
    private ITokenService _tokenSvc;
    private ApiKeyService _keySvc;

    public TokenRequestEndpointService(ITokenService tokenSvc, ApiKeyService keySvc)
    {
        _tokenSvc = tokenSvc;
        _keySvc = keySvc;
    }

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
        var newRefreshToken = request.RefreshToken; // await _tokenSvc.GenerateRefreshToken(clientId);

        return Results.Ok(new TokenResponseModel(newAccessToken, newRefreshToken, null));
    }
}
