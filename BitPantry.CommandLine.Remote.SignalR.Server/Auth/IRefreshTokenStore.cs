namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth
{
    public interface IRefreshTokenStore
    {
        Task StoreRefreshTokenAsync(string clientId, string refreshToken);
        Task<bool> TryGetRefreshTokenAsync(string clientId, out string refreshToken);
        Task RevokeRefreshTokenAsync(string clientId);
    }
}
