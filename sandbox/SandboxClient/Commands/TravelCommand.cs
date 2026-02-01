using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

/// <summary>
/// Context-aware handler that reads ProvidedValues to filter by country.
/// </summary>
public class CityHandler : IAutoCompleteHandler
{
    private static readonly Dictionary<string, string[]> CitiesByCountry = new()
    {
        ["USA"] = new[] { "New York", "Los Angeles", "Chicago" },
        ["UK"] = new[] { "London", "Manchester", "Edinburgh" },
        ["France"] = new[] { "Paris", "Lyon", "Marseille" }
    };

    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        // Read the "country" argument from provided values
        var country = context.ProvidedValues
            .FirstOrDefault(kv => kv.Key.Name.Equals("country", StringComparison.OrdinalIgnoreCase)).Value ?? "";

        var cities = CitiesByCountry.TryGetValue(country, out var c) ? c : Array.Empty<string>();

        return Task.FromResult(cities
            .Where(x => x.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .Select(x => new AutoCompleteOption(x))
            .ToList());
    }
}

/// <summary>
/// Tests context-aware autocomplete using ProvidedValues.
/// Features: FR-027
/// </summary>
[Command(Name = "travel")]
public class TravelCommand : CommandBase
{
    [Argument(Name = "country")]
    public string Country { get; set; } = "";

    [Argument(Name = "city")]
    [AutoComplete<CityHandler>]
    public string City { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Traveling to {City}, {Country}");
}
