using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using BitPantry.CommandLine.Remote.SignalR.Server.Authentication;
using System;
using System.IO;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class TestStartup
    {
        private TestEnvironmentOptions _opts;

        private static string JwtSecret { get; } = "somereallylongstringwithsomenumbersattheend1234567890-";

        public TestStartup(TestEnvironmentOptions opts)
        {
            _opts = opts;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();

            var testLoggerOutput = new TestLoggerOutput();

            services.AddSingleton(testLoggerOutput);

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddFilter((provider, category, logLevel) =>
                {
                    if (category.StartsWith("BitPantry"))
                        return logLevel >= LogLevel.Debug;
                    return false;
                });
                loggingBuilder.AddConsole();

                var logSvcs = loggingBuilder.Services.BuildServiceProvider();
                loggingBuilder.AddProvider(new TestLoggerProvider(logSvcs.GetRequiredService<ILoggerFactory>(), testLoggerOutput));
            });

            services.AddCommandLineHub(opt =>
            {
                opt.RegisterCommands(typeof(TestStartup));

                // Configure file transfer options
                var storagePath = _opts.ServerStorageRoot;
                opt.FileTransferOptions.StorageRootPath = storagePath;
                opt.FileTransferOptions.MaxFileSizeBytes = _opts.MaxFileSizeBytes;
                opt.FileTransferOptions.AllowedExtensions = _opts.AllowedExtensions;

                // Ensure the storage directory exists
                if (!Directory.Exists(storagePath))
                    Directory.CreateDirectory(storagePath);

                if (_opts.UseAuthentication)
                {
                    // Register as singleton so all requests share the same token store instance
                    var refreshTokenStore = new TestRefreshTokenStore();
                    opt.AddJwtAuthentication<TestApiKeyStore, TestRefreshTokenStore>(JwtSecret, tokenOpts =>
                    {
                        tokenOpts.AccessTokenLifetime = _opts.AccessTokenLifetime;
                        tokenOpts.RefreshTokenLifetime = _opts.RefreshTokenLifetime;
                        tokenOpts.RefreshTokenStoreImplementationFactory = _ => refreshTokenStore;
                    });
                }
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.ConfigureCommandLineHub();
        }
    }
}
