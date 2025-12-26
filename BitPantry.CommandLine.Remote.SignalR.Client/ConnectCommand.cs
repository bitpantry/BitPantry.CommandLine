using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
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
        private IProfileManager _profileManager;
        private ICredentialStore _credentialStore;

        [Argument(Position = 0)]
        [Alias('p')]
        [Description("Profile name to use for connection")]
        [Completion(typeof(ProfileNameProvider))]
        public string Profile { get; set; }

        [Argument]
        [Alias('u')]
        [Description("The remote URI to connect to (overrides profile)")]
        public string Uri { get; set; }

        [Argument]
        [Alias('k')]
        [Description("The API Key to use for authentication (overrides profile)")]
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
            IHttpClientFactory httpClientFactory,
            IProfileManager profileManager,
            ICredentialStore credentialStore)
        {
            _proxy = proxy;
            _tokenMgr = tokenMgr;
            _httpClientFactory = httpClientFactory;
            _profileManager = profileManager;
            _credentialStore = credentialStore;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            string uri = Uri;
            string apiKey = ApiKey;

            // Load profile if specified (or use default if no URI provided)
            if (!string.IsNullOrEmpty(Profile))
            {
                var profile = await _profileManager.GetProfileAsync(Profile);
                if (profile == null)
                {
                    Console.MarkupLine($"[red]Profile '{Profile}' not found[/]");
                    return;
                }

                // Profile values are used if not explicitly overridden
                uri = string.IsNullOrEmpty(Uri) ? profile.Uri : Uri;
                
                // Load API key from credential store if not provided
                if (string.IsNullOrEmpty(ApiKey))
                {
                    apiKey = await _credentialStore.RetrieveAsync(profile.Name);
                }
            }
            else if (string.IsNullOrEmpty(Uri))
            {
                // No profile and no URI - try default profile
                var defaultProfileName = await _profileManager.GetDefaultProfileAsync();
                if (!string.IsNullOrEmpty(defaultProfileName))
                {
                    var profile = await _profileManager.GetProfileAsync(defaultProfileName);
                    if (profile != null)
                    {
                        uri = profile.Uri;
                        if (string.IsNullOrEmpty(ApiKey))
                        {
                            apiKey = await _credentialStore.RetrieveAsync(profile.Name);
                        }
                        Console.MarkupLine($"[dim]Using default profile: {profile.Name}[/]");
                    }
                }
            }

            // Validate URI
            if (string.IsNullOrEmpty(uri))
            {
                Console.MarkupLine($"[red]Uri is required. Provide a URI, profile name, or set a default profile.[/]");
                return;
            }

            // validate api key and access token end point

            var getAccessTokenFirst = false;

            if(!string.IsNullOrEmpty(apiKey) || !string.IsNullOrEmpty(TokenRequestEndpoint))
            {
                if(!string.IsNullOrEmpty(apiKey) && string.IsNullOrEmpty(TokenRequestEndpoint))
                {
                    // API key provided but no token endpoint - we'll get it from the server's unauthorized response
                    getAccessTokenFirst = true;
                }
                else if(string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(TokenRequestEndpoint))
                {
                    Console.MarkupLineInterpolated($"[red]If {nameof(TokenRequestEndpoint)} is provided, {nameof(ApiKey)} is also required[/]");
                    return;
                }
                else
                {
                    getAccessTokenFirst = true;
                }
            }

            // check current connection

            await CheckCurrentConnection();

            // get access token if arguments provided

            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(TokenRequestEndpoint))
            {
                if(!await GetAccessToken(apiKey, TokenRequestEndpoint, uri))
                    return;
            }
            else if (!string.IsNullOrEmpty(apiKey))
            {
                // Store the API key for use in the connect retry flow
                _storedApiKey = apiKey;
            }

            // connect

            await Connect(ctx, getAccessTokenFirst && !string.IsNullOrEmpty(TokenRequestEndpoint), uri);
        }

        private string _storedApiKey;

        private async Task Connect(CommandExecutionContext ctx, bool hasObtainedAccessToken, string uri)
        {
            try // attempt to connect to the remote server
            {
                await Console.Status().StartAsync("Connecting ...", async ctx => await _proxy.Connect(uri));
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
                            Console.WriteLine();
                            Console.WriteLine($"The connection requires an access token, but the server did not provide the end-point " +
                                $"information required to obtain an access token. Use the {nameof(TokenRequestEndpoint)} argument to supply the endpoint.");
                            Console.WriteLine();
                            return;
                        }

                        var resp = JsonSerializer.Deserialize<UnauthorizedResponse>(responseBody);

                        // Use stored API key if available, otherwise prompt
                        string key;
                        if (!string.IsNullOrEmpty(_storedApiKey))
                        {
                            key = _storedApiKey;
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.MarkupLine("[yellow]The server requires authorization[/]");
                            key = Console.Prompt(new TextPrompt<string>("API Key: ").Validate(input =>
                            {
                                if (string.IsNullOrEmpty(input))
                                    return ValidationResult.Error("API Key is required");

                                return ValidationResult.Success();
                            })
                            .Secret());
                        }

                        // attempt to obtain an access token and retry the connect

                        if(await GetAccessToken(key, resp.TokenRequestEndpoint, uri))
                            await Connect(ctx, true, uri);
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

        private async Task<bool> GetAccessToken(string key, string tokenRequestEndpoint, string uri)
        {
            // request token

            var response = await _httpClientFactory.CreateClient().PostAsJsonAsync(new Uri(new Uri(uri.Trim().TrimEnd('/')), tokenRequestEndpoint), new { ApiKey = key });
            if (!response.IsSuccessStatusCode)
            {
                if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine();
                    Console.MarkupLine("Requesting token with API key is unathorized - make sure you are using a valid API key");
                    Console.WriteLine();
                    return false;
                }
                else
                {
                    throw new Exception($"Received an unsuccessful server response when requesting a new access token :: {response.StatusCode}");
                }
            }

            // store token

            var tokenResp = await response.Content.ReadFromJsonAsync<RequestAccessTokenResponse>();
            await _tokenMgr.SetAccessToken(new AccessToken(tokenResp.AccessToken, tokenResp.RefreshToken, tokenResp.RefreshRoute), uri);

            return true;
        }

        private async Task CheckCurrentConnection()
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Disconnected)
            {
                var authority = _proxy.ConnectionUri.Authority;

                if (ConfirmDisconnect.IsPresent && !Console.Prompt(new ConfirmationPrompt($"A connection to [yellow]{authority}[/] is currently active - do you want to disconnect?")))
                    return;

                await Console.Status().StartAsync($"Disconnecting from {authority} ...", async ctx => await _proxy.Disconnect());
            }
        }
    }
}
