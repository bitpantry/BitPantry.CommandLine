namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// DI service keys for keyed <see cref="IPathEntryProvider"/> registrations.
/// Server* handlers use the <see cref="Server"/> key; Client* handlers use the <see cref="Client"/> key.
/// Each deployment side (client vs. server) registers appropriate implementations under these keys.
/// </summary>
public static class PathEntryProviderKeys
{
    /// <summary>
    /// Key for the provider that browses the <strong>client's</strong> file system.
    /// </summary>
    public const string Client = "client";

    /// <summary>
    /// Key for the provider that browses the <strong>server's</strong> file system.
    /// </summary>
    public const string Server = "server";
}
