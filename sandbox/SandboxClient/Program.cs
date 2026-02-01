using BitPantry.CommandLine;
using BitPantry.CommandLine.Remote.SignalR.Client;
using SandboxClient.Commands;

var app = new CommandLineApplicationBuilder()
    .ConfigureSignalRClient()
    .RegisterCommands(typeof(TaskCommand))
    .Build();

await app.Run();
