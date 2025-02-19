using System.IdentityModel.Tokens.Jwt;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class AccessToken
    {
        public string Token { get; }
        public string RefreshToken { get; }
        public string RefreshRoute { get; }

        public bool IsExpired => ExpirationUtc < DateTime.UtcNow;

        public DateTime ExpirationUtc
        {
            get
            {
                var token = new JwtSecurityTokenHandler().ReadJwtToken(Token);
                if (token == null)
                    return DateTime.MinValue;
                return token.ValidTo;
            }
        }

        public AccessToken(string accessToken, string refreshToken, string refreshRoute)
        {
            Token = accessToken;
            RefreshToken = refreshToken;
            RefreshRoute = refreshRoute;
        }
    }
}
