namespace BitPantry.CommandLine.Remote.SignalR.Server.Authentication
{
    /// <summary>
    /// Configured as a service to make the command line server authentication settings available
    /// </summary>
    /// <param name="Key">The secret used to generate new tokens</param>
    /// <param name="TokenRequestEndpoint">The URL end point for requesting new access tokens</param>
    /// <param name="TokenRefreshEndpoint">The URL end point used for refreshing access tokens</param>
    /// <param name="AccessTokenLifetime">The <see cref="TimeSpan"/> from token creation, after which, the token will expire</param>
    /// <param name="RefreshTokenLifetime">The <see cref="TimeSpan"/> from token creation, after which, the token will expire</param>
    /// <param name="TokenValidationClockSkew">The grace period after token expiration during which the server will consider the token valid</param>
    /// <param name="TokenFormat">The token format</param>
    /// <param name="Issuer">The token issuer</param>
    /// <param name="Audience">The token audience</param>
    public record TokenAuthenticationSettings(
        string Key,
        string TokenRequestEndpoint,
        string TokenRefreshEndpoint,
        TimeSpan AccessTokenLifetime,
        TimeSpan RefreshTokenLifetime,
        TimeSpan TokenValidationClockSkew,
        string TokenFormat = "Bearer {JWT}",
        string Issuer = "cli-server",
        string Audience = "cli-clients")
    { }
}
