namespace BitPantry.CommandLine.Tests.Remote.SignalR.Helpers
{
    /// <summary>
    /// A disposable helper that manages temporary file creation and cleanup.
    /// Eliminates try/finally boilerplate in tests that work with temp files.
    /// </summary>
    /// <example>
    /// <code>
    /// using (var tempFile = new TempFileScope())
    /// {
    ///     await service.DownloadFile("remote.txt", tempFile.Path, CancellationToken.None);
    ///     File.ReadAllText(tempFile.Path).Should().Be(expectedContent);
    /// }
    /// // File is automatically deleted when scope ends
    /// </code>
    /// </example>
    public sealed class TempFileScope : IDisposable
    {
        /// <summary>
        /// Gets the full path to the temporary file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates a new temporary file scope with an empty temp file.
        /// </summary>
        public TempFileScope()
        {
            Path = System.IO.Path.GetTempFileName();
        }

        /// <summary>
        /// Creates a new temporary file scope with the specified initial content.
        /// </summary>
        /// <param name="initialContent">Content to write to the temp file.</param>
        public TempFileScope(string initialContent) : this()
        {
            File.WriteAllText(Path, initialContent);
        }

        /// <summary>
        /// Creates a new temporary file scope with the specified initial bytes.
        /// </summary>
        /// <param name="initialContent">Bytes to write to the temp file.</param>
        public TempFileScope(byte[] initialContent) : this()
        {
            File.WriteAllBytes(Path, initialContent);
        }

        /// <summary>
        /// Creates a temp file scope without creating the actual file.
        /// Useful when testing file creation behavior.
        /// </summary>
        /// <returns>A TempFileScope where the file does not exist.</returns>
        public static TempFileScope WithoutFile()
        {
            var scope = new TempFileScope();
            File.Delete(scope.Path);
            return scope;
        }

        /// <summary>
        /// Reads all text from the temp file.
        /// </summary>
        public string ReadAllText() => File.ReadAllText(Path);

        /// <summary>
        /// Reads all bytes from the temp file.
        /// </summary>
        public byte[] ReadAllBytes() => File.ReadAllBytes(Path);

        /// <summary>
        /// Checks if the temp file exists.
        /// </summary>
        public bool Exists => File.Exists(Path);

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                try
                {
                    File.Delete(Path);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }
    }
}
