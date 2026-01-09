using BitPantry.CommandLine;
using BitPantry.CommandLine.Remote.SignalR.Client;

var app = new CommandLineApplicationBuilder()
    .ConfigureSignalRClient()
    .Build();

await app.Run();
