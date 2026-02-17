# Remote Console I/O

Server-side commands write output and read input as if they were running locally. The SignalR infrastructure transparently relays all console operations between server and client.

---

## Output — `SignalRAnsiConsole`

On the server, commands receive a `SignalRAnsiConsole` as their `Console` property. This is a Spectre.Console `IAnsiConsole` implementation that forwards all ANSI output to the client over the SignalR connection.

The client receives the ANSI stream and renders it through its local `IAnsiConsole`, preserving colors, markup, tables, progress bars, and all other Spectre.Console output.

```csharp
// This runs on the server, but output appears on the client
[Command(Name = "server-info")]
public class ServerInfoCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine("[bold green]Server is healthy[/]");

        var table = new Table();
        table.AddColumn("Metric");
        table.AddColumn("Value");
        table.AddRow("Uptime", "14 days");
        table.AddRow("Connections", "3");
        Console.Write(table);
    }
}
```

---

## Input — `SignalRAnsiInput`

When a server command reads input (e.g., `Console.ReadKey()`), the `SignalRAnsiInput` sends an RPC request to the client. The client captures the keystroke from the local console and sends the response back to the server.

This enables interactive server commands — prompts, confirmations, and key-driven UI — to work over the remote connection.

---

## Transparency

From the command author's perspective, there is no difference between local and remote console I/O. The same `Console` property, the same Spectre.Console API, and the same output. The transport is handled entirely by the infrastructure.

---

## See Also

- [Remote Execution](index.md)
- [The IServerProxy Interface](server-proxy.md)
- [Console Configuration](../building/console-configuration.md)
- [BitPantry.VirtualConsole](../virtual-console/index.md)
- [Error Handling](../commands/error-handling.md)
