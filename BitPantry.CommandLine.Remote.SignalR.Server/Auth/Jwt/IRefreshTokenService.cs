using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> GenerateRefreshToken(string userId);
        Task<RefreshToken> GetStoredRefreshToken(string token);
        Task InvalidateRefreshToken(string token);
    }

}
