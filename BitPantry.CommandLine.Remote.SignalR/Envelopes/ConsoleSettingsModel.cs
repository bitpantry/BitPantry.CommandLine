using BitPantry.CommandLine.Remote.SignalR.Serialization;
using Spectre.Console;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ConsoleSettingsModel
{
    [JsonInclude]
    public ColorSystem ColorSystem { get; private set; }

    [JsonInclude]
    public bool Interactive { get; private set; }

    [JsonInclude]
    public bool Ansi { get; private set; }

    [JsonInclude]
    public string EncodingName { get; private set; }

    public ConsoleSettingsModel() { }

    public ConsoleSettingsModel(IAnsiConsole console)
    {
        if (console == null) throw new ArgumentNullException(nameof(console));

        var profile = console.Profile;

        ColorSystem = profile.Capabilities.ColorSystem;
        Interactive = profile.Capabilities.Interactive;
        Ansi = profile.Capabilities.Ansi;
        EncodingName = profile.Encoding?.WebName ?? Encoding.UTF8.WebName;
    }
}
