using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt
{
    public static class CommandLineServerOptionsExtensions
    {
        public static CommandLineServerOptions ConfigureJwtAuthenticationService<TCallerAuthLogic>(
            this CommandLineServerOptions svrOpts, 
            string issuerSigningKey, 
            string validIssuer, 
            string validAudience, 
            Action<JwtAuthOptions> optsAct = null) where TCallerAuthLogic : class, IJwtCallerAuthenticationLogic
        {
            AuthenticationSettings.IsUsingAuthentication = true;

            var opt = new JwtAuthOptions();
            optsAct?.Invoke(opt);

            AuthenticationSettings.AuthenticationRoute = opt.AuthenticationRoute;
            AuthenticationSettings.RefreshTokenRoute = opt.RefreshTokenRoute;

            svrOpts.Services.AddSingleton(new JwtTokenValidationParameters(issuerSigningKey, validIssuer, validAudience));
            svrOpts.Services.AddScoped<IJwtCallerAuthenticationLogic, TCallerAuthLogic>();

            return svrOpts;
        }
    }
}
