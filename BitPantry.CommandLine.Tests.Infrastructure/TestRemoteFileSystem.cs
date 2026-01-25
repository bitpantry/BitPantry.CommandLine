using System;
using System.IO;

namespace BitPantry.CommandLine.Tests.Infrastructure
{
    /// <summary>
    /// Manages test file system operations for integration tests.
    /// Provides isolated directories for each test and auto-cleanup.
    /// </summary>
    public class TestRemoteFileSystem : IDisposable
    {
        /// <summary>
        /// The server's storage root directory (where uploaded files go).
        /// </summary>
        public string ServerStorageRoot { get; }

        /// <summary>
        /// Local directory for staging files before upload or as download destination.
        /// Auto-created and cleaned up on Dispose.
        /// </summary>
        public string LocalTestDir { get; }

        /// <summary>
        /// Server-side path prefix for this test. Files uploaded here are isolated from other tests.
        /// </summary>
        public string ServerTestFolderPrefix { get; }

        /// <summary>
        /// Full path to server test directory on disk.
        /// </summary>
        public string ServerTestDir => Path.Combine(ServerStorageRoot, ServerTestFolderPrefix);

        /// <summary>
        /// Gets the local test directory path with a trailing separator for use as download destination.
        /// </summary>
        public string LocalDestination => LocalTestDir + Path.DirectorySeparatorChar;

        public TestRemoteFileSystem(TestServerOptions options)
        {
            ServerStorageRoot = options.StorageRoot;
            LocalTestDir = Path.Combine(Path.GetTempPath(), $"test-{options.EnvironmentId}");
            ServerTestFolderPrefix = $"test-{options.EnvironmentId}";

            // Create local test directory
            Directory.CreateDirectory(LocalTestDir);
        }

        /// <summary>
        /// Creates a file in the local test directory.
        /// </summary>
        /// <param name="relativePath">Path relative to LocalTestDir (e.g., "subdir/file.txt")</param>
        /// <param name="content">Text content to write (optional if size is specified)</param>
        /// <param name="size">Size in bytes for binary file (optional, overrides content)</param>
        /// <returns>Full path to the created file</returns>
        public string CreateLocalFile(string relativePath, string? content = null, long? size = null)
        {
            var fullPath = Path.Combine(LocalTestDir, relativePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (size.HasValue)
            {
                CreateBinaryFile(fullPath, size.Value);
            }
            else
            {
                File.WriteAllText(fullPath, content ?? string.Empty);
            }

            return fullPath;
        }

        /// <summary>
        /// Creates a file directly on the server storage.
        /// </summary>
        /// <param name="relativePath">Path relative to ServerTestPrefix (e.g., "subdir/file.txt")</param>
        /// <param name="content">Text content to write (optional if size is specified)</param>
        /// <param name="size">Size in bytes for binary file (optional, overrides content)</param>
        /// <returns>Server-relative path for use in download commands</returns>
        public string CreateServerFile(string relativePath, string? content = null, long? size = null)
        {
            var fullPath = Path.Combine(ServerTestDir, relativePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (size.HasValue)
            {
                CreateBinaryFile(fullPath, size.Value);
            }
            else
            {
                File.WriteAllText(fullPath, content ?? string.Empty);
            }

            return $"{ServerTestFolderPrefix}/{relativePath}";
        }

        /// <summary>
        /// Gets the full local path for a file in the local test directory.
        /// </summary>
        public string LocalPath(string relativePath) => Path.Combine(LocalTestDir, relativePath);

        /// <summary>
        /// Creates a binary file of the specified size efficiently.
        /// </summary>
        private static void CreateBinaryFile(string fullPath, long sizeInBytes)
        {
            using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            var buffer = new byte[1024 * 1024]; // 1MB buffer
            for (long written = 0; written < sizeInBytes; written += buffer.Length)
            {
                var toWrite = (int)Math.Min(buffer.Length, sizeInBytes - written);
                fs.Write(buffer, 0, toWrite);
            }
        }

        public void Dispose()
        {
            // Clean up test directories
            try
            {
                if (Directory.Exists(LocalTestDir))
                    Directory.Delete(LocalTestDir, recursive: true);
            }
            catch { /* Best effort cleanup */ }

            try
            {
                if (Directory.Exists(ServerTestDir))
                    Directory.Delete(ServerTestDir, recursive: true);
            }
            catch { /* Best effort cleanup */ }
        }
    }
}
