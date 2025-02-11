using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt
{
    public class RefreshToken
    {
        public string Token { get; set; }  // The actual refresh token
        public string UserId { get; set; }  // User associated with this token
        public DateTime Expires { get; set; }  // Expiration date
        public bool IsRevoked { get; set; }  // Marks if token was used or invalidated
    }
}
