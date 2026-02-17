# Custom Attribute Handlers

Attribute handlers provide per-argument autocomplete by decorating a property with `[AutoComplete<THandler>]`.

---

## Defining a Handler

Implement `IAutoCompleteHandler`:

```csharp
public interface IAutoCompleteHandler
{
    Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default);
}
```

```csharp
public class EnvironmentHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        var environments = new[] { "development", "staging", "production" };

        var options = environments
            .Where(e => e.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
            .Select(e => new AutoCompleteOption(e))
            .ToList();

        return Task.FromResult(options);
    }
}
```

---

## Binding to an Argument

Apply `[AutoComplete<THandler>]` to the property:

```csharp
[Command(Name = "deploy")]
public class DeployCommand : CommandBase
{
    [Argument(Name = "environment", IsRequired = true)]
    [AutoComplete<EnvironmentHandler>]
    public string Environment { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Deploying to [bold]{Environment}[/]");
    }
}
```

---

## DI Registration

Handlers are resolved from the DI container. Register them as services:

```csharp
builder.Services.AddTransient<EnvironmentHandler>();
```

This enables constructor injection in handlers:

```csharp
public class EnvironmentHandler : IAutoCompleteHandler
{
    private readonly IConfigStore _config;

    public EnvironmentHandler(IConfigStore config) => _config = config;

    public async Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        var envs = await _config.GetEnvironmentsAsync();
        return envs
            .Where(e => e.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
            .Select(e => new AutoCompleteOption(e))
            .ToList();
    }
}
```

---

## AutoCompleteContext

The context provides full information about the current input state:

| Property | Type | Description |
|----------|------|-------------|
| `QueryString` | `string` | The current partial value being typed |
| `FullInput` | `string` | The complete input string |
| `CursorPosition` | `int` | 1-based cursor position |
| `ArgumentInfo` | `ArgumentInfo` | Metadata about the argument being completed |
| `ProvidedValues` | `IReadOnlyDictionary<ArgumentInfo, string>` | Values already provided for other arguments |
| `CommandInfo` | `CommandInfo` | Metadata about the matched command |

---

## Context-Aware Suggestions

Use `ProvidedValues` to filter suggestions based on other argument values:

```csharp
public class CityHandler : IAutoCompleteHandler
{
    private static readonly Dictionary<string, string[]> _cities = new()
    {
        ["usa"] = new[] { "New York", "Los Angeles", "Chicago" },
        ["uk"] = new[] { "London", "Manchester", "Birmingham" },
    };

    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        var country = context.ProvidedValues
            .FirstOrDefault(kv => kv.Key.Name.Equals("country",
                StringComparison.OrdinalIgnoreCase))
            .Value ?? "";

        var cities = _cities.GetValueOrDefault(country.ToLower(), Array.Empty<string>());

        var options = cities
            .Where(c => c.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
            .Select(c => new AutoCompleteOption(c))
            .ToList();

        return Task.FromResult(options);
    }
}
```

```csharp
[Command(Name = "travel")]
public class TravelCommand : CommandBase
{
    [Argument(Name = "country")]
    public string Country { get; set; } = "";

    [Argument(Name = "city")]
    [AutoComplete<CityHandler>]
    public string City { get; set; } = "";
}
```

---

## Priority

Attribute handlers have the **highest** priority. They override both type handlers and built-in handlers for the decorated argument.

---

## See Also

- [Autocomplete](index.md)
- [Built-in Handlers](built-in-handlers.md)
- [Arguments](../commands/arguments.md)
- [Core Attributes](../api-reference/attributes.md)
