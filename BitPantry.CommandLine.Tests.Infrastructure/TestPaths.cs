using System.Runtime.InteropServices;

namespace BitPantry.CommandLine.Tests.Infrastructure;

/// <summary>
/// Cross-platform path constants and helpers for tests using MockFileSystem.
/// Use these instead of hardcoded Windows paths (C:\storage, C:\work, etc.)
/// to ensure tests pass on both Windows and Linux CI.
/// </summary>
public static class TestPaths
{
    /// <summary>
    /// A valid rooted path for use as MockFileSystem storage root.
    /// Windows: C:\storage  |  Linux: /storage
    /// </summary>
    public static string StorageRoot { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\storage" : "/storage";

    /// <summary>
    /// A second isolated root for PathValidator tests needing a distinct root.
    /// Windows: C:\ServerStorage  |  Linux: /ServerStorage
    /// </summary>
    public static string ServerStorageRoot { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\ServerStorage" : "/ServerStorage";

    /// <summary>
    /// Working directory root for autocomplete / CWD-based tests.
    /// Uses two levels of depth so ../ doesn't hit filesystem root on Linux.
    /// Windows: C:\testroot\work  |  Linux: /testroot/work
    /// </summary>
    public static string WorkDir { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\testroot\work" : "/testroot/work";

    /// <summary>
    /// Platform directory separator character.
    /// </summary>
    public static char Sep => Path.DirectorySeparatorChar;

    /// <summary>
    /// A rooted path guaranteed to be OUTSIDE any test storage root.
    /// For testing path-traversal rejection.
    /// Windows: C:\Windows\System32\file.txt  |  Linux: /etc/passwd
    /// </summary>
    public static string OutsidePath { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Windows\System32\file.txt"
            : "/etc/passwd";

    /// <summary>
    /// A rooted directory OUTSIDE storage root for MockFileSystem traversal tests.
    /// Windows: C:\secret  |  Linux: /secret
    /// </summary>
    public static string OutsideDir { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\secret" : "/secret";

    /// <summary>
    /// File system root for SetCurrentDirectory calls in tests.
    /// Windows: C:\  |  Linux: /
    /// </summary>
    public static string FileSystemRoot { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\" : "/";

    /// <summary>
    /// Builds a platform-correct path under a given root.
    /// Converts backslashes in relative parts to the platform separator.
    /// Usage: TestPaths.Combine(TestPaths.StorageRoot, "subfolder", "file.txt")
    /// </summary>
    public static string Combine(string root, params string[] parts)
    {
        var combined = root;
        foreach (var part in parts)
            combined = Path.Combine(combined, part.Replace('\\', Sep));
        return combined;
    }

    /// <summary>
    /// Converts a relative Windows-style path to platform-native separators
    /// and prepends the given root. Shorthand for common test pattern.
    /// Usage: TestPaths.P(TestPaths.StorageRoot, @"subdir\file.txt")
    ///   Windows → C:\storage\subdir\file.txt
    ///   Linux   → /storage/subdir/file.txt
    /// </summary>
    public static string P(string root, string relativePath) =>
        Path.Combine(root, relativePath.Replace('\\', Sep));
}
