using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using Spectre.Console;
using System.Net.Http.Json;
using System.Text.Json;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    [Command(Group = typeof(ServerGroup), Name = "connect")]
    [Description("Connects to a remote command line server")]
    public class ConnectCommand : CommandBase
    {
        private IServerProxy _proxy;
        private AccessTokenManager _tokenMgr;
        private IHttpClientFactory _httpClientFactory;

        [Argument]
        [Alias('u')]
        [Description("The remote URI to connect to")]
        public string Uri { get; set; }

        [Argument]
        [Alias('k')]
        [Description("The API Key to use for authentication")]
        public string ApiKey { get; set; }

        [Argument]
        [Alias('e')]
        [Description("The URI to request access tokens at")]
        public string TokenRequestEndpoint { get; set; }

        [Argument]
        [Alias('d')]
        [Description("If present any existing connection will be disconnected without confirmation")]
        public Option ConfirmDisconnect { get; set; }

        public ConnectCommand(
            IServerProxy proxy, 
            AccessTokenManager tokenMgr, 
            IHttpClientFactory httpClientFactory)
        {
            _proxy = proxy;
            _tokenMgr = tokenMgr;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // is uri valid

            if (string.IsNullOrEmpty(Uri))
            {
                Console.MarkupLine($"[red]Uri is required[/]");
                return;
            }

            // validate api key and access token end point

            var getAccessTokenFirst = false;

            if(!string.IsNullOrEmpty(ApiKey) || !string.IsNullOrEmpty(TokenRequestEndpoint))
            {
                if(string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(TokenRequestEndpoint))
                {
                    Console.MarkupLineInterpolated($"[red]If {nameof(ApiKey)} or {nameof(TokenRequestEndpoint)} are provided, both arguments are required[/]");
                    return;
                }

                getAccessTokenFirst = true;
            }

            // check current connection

            await CheckCurrentConnection();

            // get access token if arguments provided

            if (!string.IsNullOrEmpty(ApiKey))
            {
                if(!await GetAccessToken(ApiKey, TokenRequestEndpoint))
                    return;
            }

            // connect

            await Connect(ctx, getAccessTokenFirst);
        }

        private async Task Connect(CommandExecutionContext ctx, bool hasObtainedAccessToken)
        {
            try // attempt to connect to the remote server
            {
                await _proxy.Connect(Uri);
            }
            catch (HttpRequestException ex) // handle http exceptions
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized) // the request is unauthorized
                {
                    if (!hasObtainedAccessToken) // if it hasn't already tried to obtain an access token, try now
                    {
                        // attempt to extract access token request information from the unauthorized response body

                        string responseBody = null;
                        try { responseBody = ex.Data["responseBody"].ToString(); }
                        catch (KeyNotFoundException) 
                        {
                            Console.WriteLine($"The connection requires an access token, but the server did not provide the end-point " +
                                $"information required to obtain an access token. Use the {nameof(TokenRequestEndpoint)} argument to supply the endpoint.");
                            return;
                        }

                        var resp = JsonSerializer.Deserialize<UnauthorizedResponse>(responseBody);

                        // prompt the user for an API key

                        Console.MarkupLine("[yellow]The server requires authorization[/]");
                        var key = Console.Prompt(new TextPrompt<string>("API Key: ").Validate(input =>
                        {
                            if (string.IsNullOrEmpty(input))
                                return ValidationResult.Error("API Key is required");

                            return ValidationResult.Success();
                        })
                        .Secret());

                        // attempt to obtain an access token and retry the connect

                        if(await GetAccessToken(key, resp.TokenRequestEndpoint))
                            await Connect(ctx, true);
                    }
                    else
                    {
                        throw new HttpRequestException("Client is still unauthorized after obtaining access token from server", null, System.Net.HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task<bool> GetAccessToken(string key, string tokenRequestEndpoint)
        {
            // request token

            var response = await _httpClientFactory.CreateClient().PostAsJsonAsync(new Uri(new Uri(Uri.Trim().TrimEnd('/')), tokenRequestEndpoint), new { ApiKey = key });
            if (!response.IsSuccessStatusCode)
            {
                if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.MarkupLine("Requesting token with API key is unathorized - make sure you are using a valid API key");
                    return false;
                }
                else
                {
                    throw new Exception($"Received an unsuccessful server response when requesting a new access token :: {response.StatusCode}");
                }
            }

            // store token

            var tokenResp = await response.Content.ReadFromJsonAsync<RequestAccessTokenResponse>();
            await _tokenMgr.SetAccessToken(new AccessToken(tokenResp.AccessToken, tokenResp.RefreshToken, tokenResp.RefreshRoute), Uri);

            return true;
        }

        private async Task CheckCurrentConnection()
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Disconnected)
            {
                var authority = _proxy.ConnectionUri.Authority;

                if (ConfirmDisconnect.IsPresent && !Console.Prompt(new ConfirmationPrompt($"A connection to [yellow]{authority}[/] is currently active - do you want to disconnect?")))
                    return;

                await _proxy.Disconnect();
            }
        }
    }
}
