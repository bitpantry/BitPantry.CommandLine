using System.IdentityModel.Tokens.Jwt;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Represents an access token with associated refresh token information
    /// </summary>
    public class AccessToken
    {

        /// <summary>
        /// The access token
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// The refresh token
        /// </summary>
        public string RefreshToken { get; }

        /// <summary>
        /// The refresh token route on the server
        /// </summary>
        public string RefreshRoute { get; }

        /// <summary>
        /// True if the access token is expired, otherwise false
        /// </summary>
        public bool IsExpired => ExpirationUtc < DateTime.UtcNow;

        /// <summary>
        /// The expiration UTC datetime of the access token
        /// </summary>
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
