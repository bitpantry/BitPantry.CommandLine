namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Used to deserialize an access token refresh response
    /// </summary>
    /// <param name="AccessToken">The access token received from the server</param>
    /// <param name="RefreshToken">The refresh token received from the server</param>
    public record RefreshAccessTokenResponse (string AccessToken, string RefreshToken) { }
}
