namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth
{
    public class TokenOptions
    {
        public string Issuer { get; set; } = "cli-server";
        public string Audience { get; set; } = "cli-clients";
        public string TokenRequestRoute { get; set; } = "/cli-auth/token-request";
        public string TokenRefreshRoute { get; set; } = "/cli-auth/token-refresh";
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
        public TimeSpan TokenValidationClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    }
}
