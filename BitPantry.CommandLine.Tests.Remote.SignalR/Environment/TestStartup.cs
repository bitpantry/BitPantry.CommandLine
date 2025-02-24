using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using BitPantry.CommandLine.Remote.SignalR.Server.Authentication;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class TestStartup
    {
        private TestEnvironmentOptions _opts;

        private static string JwtSecret { get; } = "somereallylongstringwithrandomstuffattheend1234567890-";

        public TestStartup(TestEnvironmentOptions opts)
        {
            _opts = opts;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddLogging();

            services.AddSingleton(typeof(ILogger<>), typeof(TestLogger<>));

            services.AddCommandLineHub(opt =>
            {
                opt.RegisterCommands(typeof(TestStartup));
                if (_opts.UseAuthentication)
                    opt.AddJwtAuthentication<TestApiKeyStore, TestRefreshTokenStore>(JwtSecret, tokenOpts =>
                    {
                        tokenOpts.AccessTokenLifetime = _opts.AccessTokenLifetime;
                        tokenOpts.RefreshTokenLifetime = _opts.RefreshTokenLifetime;
                    });
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.ConfigureCommandLineHub();
        }
    }
}
