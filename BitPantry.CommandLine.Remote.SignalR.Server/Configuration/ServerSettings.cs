namespace BitPantry.CommandLine.Remote.SignalR.Server.Configuration
{
    /// <summary>
    /// Added as an application dependency to make server settings available to other components
    /// </summary>
    /// <param name="HubUrlPattern">The url pattern that the <see cref="CommandLineHub"/> is hosted at</param>
    public record ServerSettings(string HubUrlPattern) { }
}
