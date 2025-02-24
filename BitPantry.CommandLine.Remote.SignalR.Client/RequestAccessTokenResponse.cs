namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Models an access token request response from the server
    /// </summary>
    /// <param name="AccessToken">The access token received from the server</param>
    /// <param name="RefreshToken">The refresh token received from the server</param>
    /// <param name="RefreshRoute">The refresh route provided by the server</param>
    public record RequestAccessTokenResponse(string AccessToken, string RefreshToken, string RefreshRoute) { }
}
