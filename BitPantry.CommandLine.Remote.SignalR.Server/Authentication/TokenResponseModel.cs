/// <summary>
/// Models a token request response
/// </summary>
/// <param name="AccessToken">The access token</param>
/// <param name="RefreshToken">The refresh token</param>
/// <param name="RefreshRoute">The refresh route to refresh the token</param>
public record TokenResponseModel(string AccessToken, string RefreshToken, string RefreshRoute) { }