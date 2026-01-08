using System;
using BitPantry.CommandLine.AutoComplete.Attributes;

namespace BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;

/// <summary>
/// Shortcut attribute for remote directory path completion.
/// Signals that this argument should receive directory path completions from the remote server,
/// even if the command itself runs locally on the client.
/// </summary>
/// <remarks>
/// Use this attribute for arguments like:
/// - <c>file upload &lt;local-file&gt; &lt;remote-directory&gt;</c> - Destination is a remote directory
/// - Any argument that needs server-side directory completion when connected
/// 
/// Only directories are returned, not files.
/// When disconnected from the server, completion gracefully returns empty results.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RemoteDirectoryPathCompletionAttribute : CompletionAttribute
{
    /// <summary>
    /// Gets whether this completion requires remote server connection.
    /// Always true for remote directory path completion.
    /// </summary>
    public bool RequiresRemote => true;

    /// <summary>
    /// Gets that this attribute returns directories only.
    /// </summary>
    public bool DirectoriesOnly => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteDirectoryPathCompletionAttribute"/> class.
    /// </summary>
    public RemoteDirectoryPathCompletionAttribute()
        : base(typeof(RemoteCompletionProvider))
    {
    }
}
