using BitPantry.CommandLine;
using BitPantry.CommandLine.Remote.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SandboxClient.Commands;

var builder = new CommandLineApplicationBuilder();

// Register explicit autocomplete handlers for DI resolution
// (handlers bound via [AutoComplete<THandler>] attribute)
builder.Services.AddTransient<EnvironmentHandler>();
builder.Services.AddTransient<CityHandler>();
builder.Services.AddTransient<FilePathHandler>();

var app = builder
    .ConfigureSignalRClient()
    .RegisterCommands(typeof(TaskCommand))
    .Build();

await app.Run();
