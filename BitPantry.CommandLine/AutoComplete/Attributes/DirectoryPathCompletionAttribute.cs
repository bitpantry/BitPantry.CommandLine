using System;

namespace BitPantry.CommandLine.AutoComplete.Attributes;

/// <summary>
/// Shortcut attribute for directory path completion.
/// Provides file system completion with directory entries only.
/// </summary>
/// <remarks>
/// This is equivalent to using [Completion(typeof(DirectoryPathCompletionProvider))].
/// Supports:
/// - Relative and absolute paths
/// - Path separator normalization
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DirectoryPathCompletionAttribute : CompletionAttribute
{
    /// <summary>
    /// Gets or sets whether to include hidden directories.
    /// Default is false.
    /// </summary>
    public bool IncludeHidden { get; set; }

    /// <summary>
    /// Gets or sets the base directory for relative paths.
    /// If null, uses current working directory.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryPathCompletionAttribute"/> class.
    /// </summary>
    public DirectoryPathCompletionAttribute()
        : base(typeof(Providers.DirectoryPathCompletionProvider))
    {
    }
}
