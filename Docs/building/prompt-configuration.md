# Prompt Configuration

Configure the application prompt displayed in interactive mode, including the the app name, suffix, and multi-segment composition.

---

## Basic Configuration

Use `ConfigurePrompt()` on the builder:

```csharp
var app = new CommandLineApplicationBuilder()
    .ConfigurePrompt(p => p
        .Name("myapp")
        .WithSuffix("> "))
    .Build();
```

```
myapp> _
```

If `Name` is not set, the prompt name is derived from the entry assembly name.

---

## PromptOptions

| Method | Description |
|--------|-------------|
| `Name(string name)` | Set the application name displayed in the prompt |
| `WithSuffix(string suffix)` | Set the suffix after all prompt segments (default: `"> "`) |

Both methods support [Spectre.Console markup](https://spectreconsole.net/markup):

```csharp
builder.ConfigurePrompt(p => p
    .Name("[bold cyan]myapp[/]")
    .WithSuffix("[dim]>[/] "));
```

---

## Multi-Segment Prompts — `IPromptSegment`

The prompt is composed of one or more segments via the `CompositePrompt` model. Each segment implements `IPromptSegment`:

```csharp
public interface IPromptSegment
{
    int Order { get; }
    string? GetSegmentText();
}
```

Segments are rendered in ascending `Order` value. A segment returning `null` from `GetSegmentText()` is hidden.

**Built-in segments** (registered by the SignalR client package):

| Segment | Order | Description |
|---------|-------|-------------|
| `ServerConnectionSegment` | 100 | Shows the connected server URI |
| `ProfilePromptSegment` | 200 | Shows the active profile name |

These produce a prompt like:

```
myapp [connected: localhost:5000] [profile: production]> _
```

---

## See Also

- [Building the Application](index.md)
- [Running Commands](../running/index.md)
- [Autocomplete](../autocomplete/index.md)
- [Syntax Highlighting](../syntax-highlighting.md)
