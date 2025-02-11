using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt
{
    public class JwtTokenValidationParameters
    {
        public string IssuerSigningKey { get; }
        public string ValidIssuer { get; }
        public string ValidAudience { get; }

        public JwtTokenValidationParameters(string issuerSigningKey, string validIssuer, string validAudience)
        {
            IssuerSigningKey = issuerSigningKey;
            ValidIssuer = validIssuer;
            ValidAudience = validAudience;
        }
    }
}
