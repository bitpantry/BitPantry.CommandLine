namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public record RequestAccessTokenResponse(string AccessToken, string RefreshToken, string RefreshRoute) { }
}
