namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Root configuration object persisted to profiles.json.
/// </summary>
public class ProfileConfiguration
{
    /// <summary>
    /// Name of the default profile (null if none set).
    /// </summary>
    public string DefaultProfile { get; set; }

    /// <summary>
    /// Dictionary of profile name to profile data.
    /// </summary>
    public Dictionary<string, ServerProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Schema version for future migrations.
    /// </summary>
    public int Version { get; set; } = 1;
}
