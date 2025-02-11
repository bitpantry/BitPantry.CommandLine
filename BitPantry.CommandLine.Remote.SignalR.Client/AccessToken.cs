using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class AccessToken
    {
        public string Token { get; }
        
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

        public AccessToken(string token)
        {
            Token = token;
        }

    }
}
