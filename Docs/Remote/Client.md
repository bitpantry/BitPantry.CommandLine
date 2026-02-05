# Command Line Server Client

[← Back to Server Configuration](CommandLineServer.md)

Using the `BitPantry.CommandLine.Remote.SignalR.Client` package you can configure the core CommandLine application to connect to and execute remote commands.

```
NuGet\Install-Package BitPantry.CommandLine.Remote.SignalR.Client
```

See how to [configure the server](CommandLineServer.md) to host remote commands.

## Table of Contents

- [Setup](#setup)
- [Client Options](#client-options)
- [Connecting to the Server](#connecting-to-the-server)
  - [Connect Command](#connect-command)
  - [Disconnect Command](#disconnect-command)
- [See Also](#see-also)

## Setup

[Start by configuring the core command line application](../readme.md) and use the ```ConfigureSignalRClient``` extension to the [CommandLineApplicationBuilder](../CommandLine/CommandLineApplicationBuilder.md).

```cs
internal class Program
{
    static async Task Main(string[] args)
    {
        var cli = new CommandLineApplicationBuilder()
            .ConfigureSignalRClient()   <-- new extension provided by the SignalR.Client package 
            .Build();

        await cli.Run();
    }
}
```

The ```ConfigureSignalRClient``` accepts an optional action for configuring the client. By default the client should connect to the default server configuration, but if the server has customized configurations (like end points), the client will need to be configured accordingly.

```
internal class Program
{
    static async Task Main(string[] args)
    {
        var cli = new CommandLineApplicationBuilder()
            .ConfigureSignalRClient(opt =>
            {
                // configure
            })
            .Build();

        await cli.Run();

    }
}
```

The ```HttpClientFactory``` and ```HttpMessageHandler``` options are used to configure the client for unit / integration testing using Microsoft TestServer (Microsoft.AspNetCore.TestHost).

```
public class SignalRClientOptions
{
    /// <summary>
    /// The HttpClient factory used by the client
    /// </summary>
    public IHttpClientFactory HttpClientFactory { get; set; } = new DefaultHttpClientFactory();

    /// <summary>
    /// The HttpMessageHandler used by the cient
    /// </summary>
    public IHttpMessageHandlerFactory HttpMessageHandlerFactory { get; set; } = new DefaultHttpMessageHandlerFactory();

    /// <summary>
    /// How often the <see cref="AccessTokenManager"/> should see if the current access token needs to be refreshed and attempt to refresh it
    /// </summary>
    public TimeSpan TokenRefreshMonitorInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long before the access token expires can the <see cref="AccessTokenManager"/> begin attempting to refresh it
    /// </summary>
    public TimeSpan TokenRefreshThreshold { get; set; } = TimeSpan.FromMinutes(5);
}
```

For detailed client configuration options, see [SignalRClientOptions](SignalRClientOptions.md).

## Connecting to the Server

When the client is configured, two new commands are automatically registered.

## Connect Command

The connect command connects the client to a remote command line server.

```BitPantry.CommandLine.Remote.SignalR.Client.ConnectCommand```

Use the following syntax to execute the command.

```server.connect --Uri|-u <REMOTE SERVER URI> [--ApiKey|-k] <API KEY> [--TokenReqeustEndpoint|-e] <TOKEN REQUEST ENDPOINT> [--Force|-f]```

The only required parameter is the URI of the remote server (by default at /cli). If no other arguments are provided, and the server returns an unauthorized response (meaning JWT authentication is configured on the server), you'll be prompted to enter an API key. Once an API key is provided, the client will request an access token and attempt to reconnect.

If a connection is already active, you'll be promted to confirm the disconnect from the current server before attempting to connect to the server specified by the Uri argument.

If connecting using a script, values for the *ApiKey*, *TokenRequestEndpoint*, and *Force* arguments can be provided to bypass any required user interaction.

Once the connection is established, any commands hosted remotely will be available. Use the ```ListCommands|ls``` command to list the available commands.

## Disconnect Command

The disconnect command disconnects the current client connection.

```BitPantry.CommandLine.Remote.SignalR.Client.DisconnectCommand```

Use the following syntax to execute the command.

```server.disconnect```

---

## See Also

- [Configuring the core command line application](../readme.md)
- [SignalRClientOptions](SignalRClientOptions.md) - Detailed client configuration options
- [CommandLineServer](CommandLineServer.md) - Server setup guide
- [Troubleshooting](Troubleshooting.md) - Common issues and solutions
