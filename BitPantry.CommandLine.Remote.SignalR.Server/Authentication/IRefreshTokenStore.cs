namespace BitPantry.CommandLine.Remote.SignalR.Server.Authentication
{
    /// <summary>
    /// Defines the refresh token store - to be implemented by the developer
    /// </summary>
    public interface IRefreshTokenStore
    {
        /// <summary>
        /// Stores a refresh token
        /// </summary>
        /// <param name="clientId">The client id the token should be associated to</param>
        /// <param name="refreshToken">The refresh token to store</param>
        Task StoreRefreshTokenAsync(string clientId, string refreshToken);

        /// <summary>
        /// Tries to get a refresh token by the given client id
        /// </summary>
        /// <param name="clientId">The client id to attempt to get a refresh token for</param>
        /// <param name="refreshToken">The refresh token retrieved for the given client id</param>
        /// <returns>True if a refresh token was found for the given <paramref name="clientId"/>, otherwise false - if false
        /// the value of <paramref name="refreshToken"/> should be considered invalid.</returns>
        Task<bool> TryGetRefreshTokenAsync(string clientId, out string refreshToken);
        Task RevokeRefreshTokenAsync(string clientId);
    }
}
