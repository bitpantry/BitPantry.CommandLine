namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth
{
    public record TokenAuthenticationSettings(
        string Key, 
        string TokenRequestEndpoint, 
        string TokenRefreshEndpoint, 
        TimeSpan AccessTokenLifetime,
        TimeSpan RefreshTokenLifetime,
        TimeSpan TokenValidationClockSkew,
        string TokenFormat = "Bearer {JWT}", 
        string Issuer = "cli-server", 
        string Audience = "cli-clients") { }
}
