﻿using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Manages an access token, providing synchronized access to the token, monitoring and refreshing the token, and notifying interested parties when the token changes
    /// </summary>
    public class AccessTokenManager : IDisposable
    {
        private object _lock = new object();

        readonly ProcessGate _gate = new ProcessGate();
        readonly string _monitorTokenLockName = "monitor";
        readonly string _setTokenLockName = "setToken";

        readonly CancellationTokenSource _tokenSource = new();
        readonly ILogger<AccessTokenManager> _logger;
        readonly IHttpClientFactory _httpClientFactory;
        readonly CommandLineClientSettings _clientSettings;
        readonly Task _monitorTask;

        private AccessToken _currentToken;
        private string _currentServerUri;

        /// <summary>
        /// The current token being managed - null if no token is being managed
        /// </summary>
        public AccessToken CurrentToken 
        { 
            get { lock(_lock) return _currentToken; }
            private set { lock(_lock) _currentToken = value; }
        }

        /// <summary>
        /// Subscribers to this event will be notified if and when the token changes - the new access token is passed to the event handler when this event is raised
        /// </summary>
        public event Func<object, AccessToken, Task> OnAccessTokenChanged;

        public AccessTokenManager(ILogger<AccessTokenManager> logger, IHttpClientFactory httpClientFactory, CommandLineClientSettings clientSettings) 
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _clientSettings = clientSettings;

            _monitorTask = MonitorToken(_tokenSource.Token);
        }

        // internal function continuously runs to monitor the access token's status and refresh it if possible
        private async Task MonitorToken(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    using (await _gate.LockAsync(_monitorTokenLockName, cancellationToken))
                    {
                        if (await ProcessAccessToken(cancellationToken))
                            await RaiseOnAccessTokenRefreshed();
                    }
                
                    await Task.Delay(_clientSettings.TokenRefreshMonitorInterval, cancellationToken);
                }
                catch (OperationCanceledException) { /* swallow */ }

            } while(!cancellationToken.IsCancellationRequested);
        }

        private async Task<bool> ProcessAccessToken(CancellationToken token)
        {
            bool isUpdatedToken = false;

            if (CurrentToken != null)
            {
                try
                {
                    if (CurrentToken.ExpirationUtc < DateTime.UtcNow || CurrentToken.ExpirationUtc < DateTime.UtcNow.Add(_clientSettings.TokenRefreshThreshold))
                    {

                        var httpClient = _httpClientFactory.CreateClient();

                        var response = await httpClient.PostAsJsonAsync(new Uri(new Uri(_currentServerUri.TrimEnd('/')), CurrentToken.RefreshRoute), new { CurrentToken.RefreshToken }, cancellationToken: token);

                        var tokenResp = response.IsSuccessStatusCode
                            ? await response.Content.ReadFromJsonAsync<RefreshAccessTokenResponse>(cancellationToken: token)
                            : null;

                        CurrentToken = tokenResp == null
                            ? null
                            : new AccessToken(tokenResp.AccessToken, tokenResp.RefreshToken, CurrentToken.RefreshRoute);

                        isUpdatedToken = true;

                        if (!response.IsSuccessStatusCode)
                            _logger.LogError("Received an unsuccessful server response when requesting a new access token :: {StatusCode}", response.StatusCode);
                        else
                            _logger.LogDebug("Successfully refreshed access token");

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured while refreshing the current access token");
                }
            }

            return isUpdatedToken;
        }

        private async Task RaiseOnAccessTokenRefreshed()
        {
            if (OnAccessTokenChanged != null)
            {
                var handlers = OnAccessTokenChanged.GetInvocationList()
                    .Cast<Func<object, AccessToken, Task>>() 
                    .Select(handler => handler(this, CurrentToken)); 

                await Task.WhenAll(handlers); 
            }
        }

        /// <summary>
        /// Sets the access token being managed by this manager. Setting a new access token using this function raises the <see cref="OnAccessTokenChanged"/> event.
        /// </summary>
        /// <param name="token">The token to be managed. Passing null will pass a null access token as the new token to all parties subscribed to the <see cref="OnAccessTokenChanged"/> event signaling a disconnect</param>
        /// <param name="serverUri">The server URI that the token authorizes access to</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns></returns>
        public async Task SetAccessToken(AccessToken token, string serverUri, CancellationToken cancellationToken = default)
        {
            try
            {
                using (await _gate.LockAsync(_setTokenLockName, cancellationToken))
                {
                    if (CurrentToken == null)
                        _logger.LogDebug("Setting access token - current access token is null");
                    else
                        _logger.LogDebug("Setting access token - replacing current access token");

                    CurrentToken = token;
                    _currentServerUri = serverUri;

                    await ProcessAccessToken(cancellationToken);

                    await RaiseOnAccessTokenRefreshed();
                }
            }
            catch(OperationCanceledException) { /* swallow */ }
        }

        public async void Dispose()
        {
            _tokenSource.Cancel();
            await _monitorTask.ConfigureAwait(false);
        }
    }
}
