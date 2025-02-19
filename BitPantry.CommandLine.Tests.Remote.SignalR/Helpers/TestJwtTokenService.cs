using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Server.Auth;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Helpers
{
    public static class TestJwtTokenService
    {
        public static JwtTokenService Create()
            => Create(TimeSpan.FromMinutes(60), TimeSpan.FromDays(30));

        public static JwtTokenService Create(TimeSpan accessTokenLifetime, TimeSpan refreshTokenLifetime)
        {
            var settings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/cli-auth/token-request",
                "/cli-auth/token-refresh",
                accessTokenLifetime,
                refreshTokenLifetime,
                TimeSpan.Zero,
                "issuer",
                "audience");

            return new JwtTokenService(new Mock<ILogger<JwtTokenService>>().Object, settings, new Mock<IRefreshTokenStore>().Object);
        }

        public static AccessToken GenerateAccessToken()
            => Create().GenerateAccessToken();

        public static AccessToken GenerateAccessToken(TimeSpan accessTokenLifetime, TimeSpan refreshTokenLifetime)
            => Create(accessTokenLifetime, refreshTokenLifetime).GenerateAccessToken();
    }
}
