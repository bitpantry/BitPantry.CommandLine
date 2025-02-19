using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth
{
    public static class CommandLineServerOptionsExtensions
    {
        public static CommandLineServerOptions AddJwtAuthentication<TApiKeyStore>(
            this CommandLineServerOptions svrOpts,
            string key,
            Action<TokenOptions> optsAct = null) where TApiKeyStore : class, IApiKeyStore
                => AddJwtAuthentication<TApiKeyStore, InMemoryRefreshTokenStore>(svrOpts, key, optsAct);

        public static CommandLineServerOptions AddJwtAuthentication<TApiKeyStore, TRefreshTokenStore>(
            this CommandLineServerOptions svrOpts,
            string key,
            Action<TokenOptions> optsAct = null) 
                where TApiKeyStore : class, IApiKeyStore
                where TRefreshTokenStore : class, IRefreshTokenStore
        {
            // configure options

            var opt = new TokenOptions();
            optsAct?.Invoke(opt);

            // add configure web app hooks

            svrOpts.Services.AddTransient<TokenRequestEndpointService>();

            svrOpts.ConfigurationHooks.ConfigureWebApplication(app => { app.UseMiddleware<TokenValidationMiddleware>(); }, true);

            svrOpts.ConfigurationHooks.ConfigureWebApplication(app => 
                app.UseEndpoints(ep => ep.MapPost(opt.TokenRequestRoute, async (TokenRequestModel request, TokenRequestEndpointService svc) => await svc.HandleTokenRequest(request, opt.TokenRefreshRoute))));

            svrOpts.ConfigurationHooks.ConfigureWebApplication(app =>
                app.UseEndpoints(ep => ep.MapPost(opt.TokenRefreshRoute, async (TokenRefreshRequestModel request, TokenRequestEndpointService svc) => await svc.HandleTokenRefreshRequest(request))));

            // configure services

            svrOpts.Services.AddSingleton(new TokenAuthenticationSettings(key, opt.TokenRequestRoute, opt.TokenRefreshRoute, opt.AccessTokenLifetime, opt.RefreshTokenLifetime, opt.TokenValidationClockSkew, opt.Issuer, opt.Audience));

            svrOpts.Services.AddTransient<IApiKeyStore, TApiKeyStore>();
            svrOpts.Services.AddTransient<ITokenService, JwtTokenService>();
            svrOpts.Services.AddTransient<ApiKeyService>();
            svrOpts.Services.AddTransient<IRefreshTokenStore, TRefreshTokenStore>();

            return svrOpts;
        }


    }
}
