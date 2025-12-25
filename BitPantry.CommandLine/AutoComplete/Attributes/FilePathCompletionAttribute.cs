using System;

namespace BitPantry.CommandLine.AutoComplete.Attributes;

/// <summary>
/// Shortcut attribute for file path completion.
/// Provides file system completion with file entries.
/// </summary>
/// <remarks>
/// This is equivalent to using [Completion(typeof(FilePathCompletionProvider))].
/// Supports:
/// - Relative and absolute paths
/// - Path separator normalization
/// - Extension filtering via Pattern property
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FilePathCompletionAttribute : CompletionAttribute
{
    /// <summary>
    /// Gets or sets a file pattern filter (e.g., "*.txt", "*.json").
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets whether to include directories in results.
    /// Default is true (for path navigation).
    /// </summary>
    public bool IncludeDirectories { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include hidden files.
    /// Default is false.
    /// </summary>
    public bool IncludeHidden { get; set; }

    /// <summary>
    /// Gets or sets the base directory for relative paths.
    /// If null, uses current working directory.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePathCompletionAttribute"/> class.
    /// </summary>
    public FilePathCompletionAttribute()
        : base(typeof(Providers.FilePathCompletionProvider))
    {
    }
}
