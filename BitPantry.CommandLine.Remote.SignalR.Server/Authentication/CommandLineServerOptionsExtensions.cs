using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Authentication
{
    /// <summary>
    /// <see cref="CommandLineServerOptions"/> extensions used to configure authentication for the commeand line server
    /// </summary>
    public static class CommandLineServerOptionsExtensions
    {

        /// <summary>
        /// Adds Jwt authentication to the command line server
        /// </summary>
        /// <typeparam name="TApiKeyStore">The <see cref="IApiKeyStore"/> implementation to use - will be configured as a service</typeparam>
        /// <typeparam name="TRefreshTokenStore">The <see cref="IRefreshTokenStore"/> implementation to use - will be configured as a service</typeparam>
        /// <param name="svrOpts">The server options being extended</param>
        /// <param name="key">The Jwt secret / key</param>
        /// <param name="jwtAuthOptsAction">An action for configuring the server authentication options</param>
        /// <returns></returns>
        public static CommandLineServerOptions AddJwtAuthentication<TApiKeyStore, TRefreshTokenStore>(
            this CommandLineServerOptions svrOpts,
            string key,
            Action<JwtAuthOptions<TApiKeyStore, TRefreshTokenStore>> jwtAuthOptsAction = null)
                where TApiKeyStore : class, IApiKeyStore
                where TRefreshTokenStore : class, IRefreshTokenStore
        {
            // configure options

            var jwtAuthOpts = new JwtAuthOptions<TApiKeyStore, TRefreshTokenStore>();
            jwtAuthOptsAction?.Invoke(jwtAuthOpts);

            // add configure web app hooks

            svrOpts.ConfigurationHooks.ConfigureWebApplication(app => { app.UseMiddleware<TokenValidationMiddleware>(); }, true);

            svrOpts.ConfigurationHooks.ConfigureWebApplication(app =>
                app.UseEndpoints(ep => ep.MapPost(jwtAuthOpts.TokenRequestRoute, async (TokenRequestModel request, TokenRequestEndpointService svc) => await svc.HandleTokenRequest(request, jwtAuthOpts.TokenRefreshRoute))));

            svrOpts.ConfigurationHooks.ConfigureWebApplication(app =>
                app.UseEndpoints(ep => ep.MapPost(jwtAuthOpts.TokenRefreshRoute, async (TokenRefreshRequestModel request, TokenRequestEndpointService svc) => await svc.HandleTokenRefreshRequest(request))));

            // configure services

            svrOpts.Services.AddTransient<TokenRequestEndpointService>();
            svrOpts.Services.AddTransient<ITokenService, JwtTokenService>();
            svrOpts.Services.AddTransient<ApiKeyService>();

            // configure authentication stores

            if (jwtAuthOpts.ApiKeyStoreImplementationFactory == null)
                svrOpts.Services.AddTransient<IApiKeyStore, TApiKeyStore>();
            else
                svrOpts.Services.AddTransient<TApiKeyStore, TApiKeyStore>(jwtAuthOpts.ApiKeyStoreImplementationFactory);

            if (jwtAuthOpts.RefreshTokenStoreImplementationFactory == null)
                svrOpts.Services.AddTransient<IRefreshTokenStore, TRefreshTokenStore>();
            else
                svrOpts.Services.AddTransient<IRefreshTokenStore, TRefreshTokenStore>(jwtAuthOpts.RefreshTokenStoreImplementationFactory);

            // configure settings

            svrOpts.Services.AddSingleton(new TokenAuthenticationSettings(key, jwtAuthOpts.TokenRequestRoute, jwtAuthOpts.TokenRefreshRoute, jwtAuthOpts.AccessTokenLifetime, jwtAuthOpts.RefreshTokenLifetime, jwtAuthOpts.TokenValidationClockSkew, jwtAuthOpts.Issuer, jwtAuthOpts.Audience));


            return svrOpts;
        }


    }
}
