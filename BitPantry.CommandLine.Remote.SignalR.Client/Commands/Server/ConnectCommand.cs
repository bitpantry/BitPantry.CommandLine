using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    [InGroup<ServerGroup>]
    [Command(Name = "connect")]
    [Description("Connects to a remote command line server")]
    public class ConnectCommand : CommandBase
    {
        private IServerProxy _proxy;
        private ConnectionService _connectionService;
        private IProfileManager _profileManager;
        private IProfileConnectionState _profileConnectionState;
        private string _resolvedProfileName;

        [Argument(Position = 0, Name = "name")]
        [Alias('n')]
        [AutoComplete<ProfileNameProvider>]
        [Description("Profile name to use for connection")]
        public string ProfileName { get; set; }

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
            ConnectionService connectionService,
            IProfileManager profileManager,
            IProfileConnectionState profileConnectionState)
        {
            _proxy = proxy;
            _connectionService = connectionService;
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

            if(!string.IsNullOrEmpty(ApiKey) || !string.IsNullOrEmpty(TokenRequestEndpoint))
            {
                if(string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(TokenRequestEndpoint))
                {
                    Console.MarkupLineInterpolated($"[red]If {nameof(ApiKey)} or {nameof(TokenRequestEndpoint)} are provided, both arguments are required[/]");
                    return;
                }
            }

            // check current connection

            await CheckCurrentConnection();

            // If API key and explicit token endpoint provided, pre-acquire token
            if (!string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(TokenRequestEndpoint))
            {
                try
                {
                    await _connectionService.AcquireAccessTokenAsync(ApiKey, Uri, TokenRequestEndpoint);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.MarkupLine("Requesting token with API key is unauthorized - make sure you are using a valid API key");
                    return;
                }
            }

            // connect (with auth handling)
            await Connect(ctx);

            // Track profile connection state after successful connection
            _profileConnectionState.ConnectedProfileName = _resolvedProfileName;
        }

        private async Task Connect(CommandExecutionContext ctx)
        {
            // If we have an API key (from args or profile), use non-interactive auth flow
            if (!string.IsNullOrEmpty(ApiKey))
            {
                try
                {
                    await _connectionService.ConnectWithAuthAsync(_proxy, Uri, ApiKey);
                }
                catch (InvalidOperationException ex)
                {
                    Console.MarkupLine($"[red]{ex.Message}[/]");
                }
                return;
            }

            // No API key — try connecting, handle 401 interactively
            try
            {
                await _proxy.Connect(Uri);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Extract token endpoint from unauthorized response
                var tokenEndpoint = ConnectionService.ExtractTokenEndpoint(ex);
                if (string.IsNullOrEmpty(tokenEndpoint))
                {
                    Console.WriteLine($"The connection requires an access token, but the server did not provide the end-point " +
                        $"information required to obtain an access token. Use the {nameof(TokenRequestEndpoint)} argument to supply the endpoint.");
                    return;
                }

                // Prompt the user for an API key
                Console.MarkupLine("[yellow]The server requires authorization[/]");
                var key = Console.Prompt(new TextPrompt<string>("API Key: ").Validate(input =>
                {
                    if (string.IsNullOrEmpty(input))
                        return ValidationResult.Error("API Key is required");

                    return ValidationResult.Success();
                })
                .Secret());

                // Acquire token and retry
                try
                {
                    await _connectionService.AcquireAccessTokenAsync(key, Uri, tokenEndpoint);
                    await _proxy.Connect(Uri);
                }
                catch (HttpRequestException ex2) when (ex2.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.MarkupLine("[red]Unauthorized — make sure you are using a valid API key[/]");
                }
            }
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
        /// Resolves connection settings from the specified profile.
        /// Explicit arguments (--uri, --api-key) override profile settings.
        /// A profile name must be explicitly specified via --name when using profile-based connection.
        /// </summary>
        private async Task ResolveProfileSettings()
        {
            ServerProfile? profile = null;

            // If --name specified, load that profile
            if (!string.IsNullOrEmpty(ProfileName))
            {
                profile = await _profileManager.GetProfileAsync(ProfileName);
                if (profile == null)
                {
                    Console.MarkupLine($"[red]Profile '{ProfileName}' not found[/]");
                    return;
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
