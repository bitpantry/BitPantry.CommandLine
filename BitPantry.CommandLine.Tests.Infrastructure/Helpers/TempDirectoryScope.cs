namespace BitPantry.CommandLine.Tests.Infrastructure.Helpers
{
    /// <summary>
    /// A disposable helper that manages temporary directory creation and cleanup.
    /// Eliminates try/finally boilerplate in tests that work with temp directories.
    /// </summary>
    /// <example>
    /// <code>
    /// using (var tempDir = new TempDirectoryScope())
    /// {
    ///     // tempDir.Path is a unique temp directory that doesn't exist yet
    ///     Directory.CreateDirectory(tempDir.Path);
    ///     // ... test code ...
    /// }
    /// // Directory is automatically deleted when scope ends
    /// </code>
    /// </example>
    public sealed class TempDirectoryScope : IDisposable
    {
        /// <summary>
        /// Gets the full path to the temporary directory.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates a new temporary directory scope with a unique path that does not exist.
        /// The directory is NOT created - use this when testing directory creation behavior.
        /// </summary>
        public TempDirectoryScope()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"cli-storage-test-{Guid.NewGuid():N}");
        }

        /// <summary>
        /// Creates a new temporary directory scope with a created directory.
        /// </summary>
        /// <param name="createDirectory">If true, the directory is created immediately.</param>
        public TempDirectoryScope(bool createDirectory) : this()
        {
            if (createDirectory)
            {
                Directory.CreateDirectory(Path);
            }
        }

        /// <summary>
        /// Checks if the temp directory exists.
        /// </summary>
        public bool Exists => Directory.Exists(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                try
                {
                    Directory.Delete(Path, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }
    }
}
