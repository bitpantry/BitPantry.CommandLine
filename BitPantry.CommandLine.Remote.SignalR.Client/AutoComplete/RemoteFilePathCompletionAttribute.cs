using System;
using BitPantry.CommandLine.AutoComplete.Attributes;

namespace BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;

/// <summary>
/// Shortcut attribute for remote file path completion.
/// Signals that this argument should receive file path completions from the remote server,
/// even if the command itself runs locally on the client.
/// </summary>
/// <remarks>
/// Use this attribute for arguments like:
/// - <c>file download &lt;remote-path&gt;</c> - Source is a remote file path
/// - Any argument that needs server-side path completion when connected
/// 
/// When disconnected from the server, completion gracefully returns empty results.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RemoteFilePathCompletionAttribute : CompletionAttribute
{
    /// <summary>
    /// Gets or sets whether to include directories in results.
    /// Default is true (for path navigation).
    /// </summary>
    public bool IncludeDirectories { get; set; } = true;

    /// <summary>
    /// Gets or sets a file pattern filter (e.g., "*.txt", "*.json").
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets whether this completion requires remote server connection.
    /// Always true for remote file path completion.
    /// </summary>
    public bool RequiresRemote => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFilePathCompletionAttribute"/> class.
    /// </summary>
    public RemoteFilePathCompletionAttribute()
        : base(typeof(RemoteCompletionProvider))
    {
    }
}
