using BitPantry.CommandLine.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Consolidates shared authentication and connection logic used by both
    /// ConnectCommand (interactive) and SignalRAutoConnectHandler (non-interactive).
    /// </summary>
    public class ConnectionService
    {
        private readonly ILogger<ConnectionService> _logger;
        private readonly AccessTokenManager _tokenMgr;
        private readonly IHttpClientFactory _httpClientFactory;

        public ConnectionService(
            ILogger<ConnectionService> logger,
            AccessTokenManager tokenMgr,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _tokenMgr = tokenMgr;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Connects to the server, automatically handling the 401 → token acquisition → retry
        /// flow when an API key is available.
        /// </summary>
        /// <param name="proxy">The server proxy to connect through</param>
        /// <param name="uri">The server URI</param>
        /// <param name="apiKey">API key for authentication, or null if none available</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="HttpRequestException">If still unauthorized after token acquisition</exception>
        /// <exception cref="InvalidOperationException">If server requires auth but no API key or token endpoint</exception>
        public async Task ConnectWithAuthAsync(IServerProxy proxy, string uri, string apiKey, CancellationToken token = default)
        {
            try
            {
                await proxy.Connect(uri, token);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (string.IsNullOrEmpty(apiKey))
                    throw; // Caller must handle (e.g., prompt user for API key)

                var tokenEndpoint = ExtractTokenEndpoint(ex);
                if (string.IsNullOrEmpty(tokenEndpoint))
                    throw new InvalidOperationException(
                        "Server requires authorization but did not provide a token endpoint", ex);

                _logger.LogDebug("Acquiring access token from {TokenEndpoint}", tokenEndpoint);
                await AcquireAccessTokenAsync(apiKey, uri, tokenEndpoint, token);

                // Retry once after obtaining token
                try
                {
                    await proxy.Connect(uri, token);
                }
                catch (HttpRequestException ex2) when (ex2.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException(
                        "Still unauthorized after obtaining access token", ex2);
                }
            }
        }

        /// <summary>
        /// Acquires an access token from the server using an API key and stores it in the token manager.
        /// </summary>
        /// <param name="apiKey">The API key to authenticate with</param>
        /// <param name="serverUri">The server base URI</param>
        /// <param name="tokenEndpoint">The relative token endpoint path</param>
        /// <param name="token">Cancellation token</param>
        public async Task AcquireAccessTokenAsync(string apiKey, string serverUri, string tokenEndpoint, CancellationToken token = default)
        {
            var baseUri = new Uri(serverUri.Trim().TrimEnd('/'));
            var requestUri = new Uri(baseUri, tokenEndpoint);

            var response = await _httpClientFactory.CreateClient()
                .PostAsJsonAsync(requestUri, new { ApiKey = apiKey }, cancellationToken: token);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new HttpRequestException(
                        "API key is unauthorized — ensure a valid API key is configured",
                        null, HttpStatusCode.Unauthorized);
                else
                    throw new HttpRequestException(
                        $"Failed to obtain access token — server returned {response.StatusCode}",
                        null, response.StatusCode);
            }

            var tokenResp = await response.Content.ReadFromJsonAsync<RequestAccessTokenResponse>(cancellationToken: token);
            await _tokenMgr.SetAccessToken(
                new AccessToken(tokenResp.AccessToken, tokenResp.RefreshToken, tokenResp.RefreshRoute),
                serverUri);

            _logger.LogDebug("Access token acquired and stored for {ServerUri}", serverUri);
        }

        /// <summary>
        /// Extracts the token request endpoint from an HttpRequestException's Data dictionary.
        /// The DefaultHttpMessageHandler stashes the response body there on 401 responses.
        /// </summary>
        /// <param name="ex">The HTTP request exception from a 401 response</param>
        /// <returns>The token endpoint path, or null if not available</returns>
        public static string ExtractTokenEndpoint(HttpRequestException ex)
        {
            try
            {
                var responseBody = ex.Data["responseBody"]?.ToString();
                if (string.IsNullOrEmpty(responseBody))
                    return null;

                var resp = JsonSerializer.Deserialize<UnauthorizedResponse>(responseBody);
                return resp?.TokenRequestEndpoint;
            }
            catch
            {
                return null;
            }
        }
    }
}
