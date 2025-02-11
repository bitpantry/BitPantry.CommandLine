using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt
{
    public interface IRefreshTokenStore
    {
        Task StoreTokenAsync(RefreshToken token);
        Task<RefreshToken> GetTokenAsync(string token);
        Task InvalidateTokenAsync(string token);
    }

}
