using BitPantry.CommandLine.Remote.SignalR.Client;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Helpers
{
    public static class JwtServiceExtensions
    {
        public static AccessToken GenerateAccessToken(this JwtTokenService svc)
        {
            var accessToken = svc.GenerateAccessToken("1");
            var refreshToken = svc.GenerateRefreshToken("1").Result;
            return new AccessToken(accessToken, refreshToken, "/cli-auth/token-refresh");
        }
    }
}
