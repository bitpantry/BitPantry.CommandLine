using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;
using Spectre.Console;
using System.Net.Http.Json;
using System.Text.Json;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    [InGroup<ServerGroup>]
    [Command(Name = "connect")]
    [Description("Connects to a remote command line server")]
    public class ConnectCommand : CommandBase
    {
        private IServerProxy _proxy;
        private AccessTokenManager _tokenMgr;
        private IHttpClientFactory _httpClientFactory;
        private IProfileManager _profileManager;
        private IProfileConnectionState _profileConnectionState;
        private string _resolvedProfileName;

        [Argument]
        [Alias('p')]
        [AutoComplete<ProfileNameProvider>]
        [Description("Profile name to use for connection")]
        public string Profile { get; set; }

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
        [Alias('f')]
        [Flag]
        [Description("Force disconnect of any existing connection without confirmation")]
        public bool Force { get; set; }

        public ConnectCommand(
            IServerProxy proxy, 
            AccessTokenManager tokenMgr, 
            IHttpClientFactory httpClientFactory,
            IProfileManager profileManager,
            IProfileConnectionState profileConnectionState)
        {
            _proxy = proxy;
            _tokenMgr = tokenMgr;
            _httpClientFactory = httpClientFactory;
            _profileManager = profileManager;
            _profileConnectionState = profileConnectionState;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // Resolve profile if specified or use default
            await ResolveProfileSettings();

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

            // Track profile connection state after successful connection
            _profileConnectionState.ConnectedProfileName = _resolvedProfileName;
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
                var authority = _proxy.Server.ConnectionUri.Authority;

                if (Force && !Console.Prompt(new ConfirmationPrompt($"A connection to [yellow]{authority}[/] is currently active - do you want to disconnect?")))
                    return;

                await _proxy.Disconnect();
            }
        }

        /// <summary>
        /// Resolves connection settings from profile if specified or from default profile.
        /// Explicit arguments (--uri, --api-key) override profile settings.
        /// </summary>
        private async Task ResolveProfileSettings()
        {
            ServerProfile? profile = null;

            // If --profile specified, load that profile
            if (!string.IsNullOrEmpty(Profile))
            {
                profile = await _profileManager.GetProfileAsync(Profile);
                if (profile == null)
                {
                    Console.MarkupLine($"[red]Profile '{Profile}' not found[/]");
                    return;
                }
            }
            // If no --profile and no --uri, try to use default profile
            else if (string.IsNullOrEmpty(Uri))
            {
                var defaultProfileName = await _profileManager.GetDefaultProfileNameAsync();
                if (!string.IsNullOrEmpty(defaultProfileName))
                {
                    profile = await _profileManager.GetProfileAsync(defaultProfileName);
                }
            }

            // Apply profile settings (explicit arguments override)
            if (profile != null)
            {
                // Use profile URI if no explicit --uri provided
                if (string.IsNullOrEmpty(Uri))
                {
                    Uri = profile.Uri;
                }
                
                // Use profile API key if no explicit --api-key provided
                if (string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(profile.ApiKey))
                {
                    ApiKey = profile.ApiKey;
                }
            }

            // Track which profile was used (if any)
            _resolvedProfileName = profile?.Name;
        }
    }
}
