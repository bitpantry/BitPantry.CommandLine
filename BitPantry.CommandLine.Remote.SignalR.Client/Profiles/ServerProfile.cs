using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Represents a saved server connection profile.
/// </summary>
public class ServerProfile
{
    /// <summary>
    /// Unique profile name (case-insensitive comparison).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Server URI for the remote connection.
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// Decrypted API key. Populated by IProfileManager.GetProfileAsync(),
    /// not persisted to profiles.json.
    /// </summary>
    [JsonIgnore]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Timestamp when the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the profile was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
