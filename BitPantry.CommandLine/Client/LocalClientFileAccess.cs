using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;

namespace BitPantry.CommandLine.Client
{
    /// <summary>
    /// Local implementation of <see cref="IClientFileAccess"/> that performs direct file I/O
    /// against the local file system. Used when the command is running on the client machine.
    /// </summary>
    public class LocalClientFileAccess : IClientFileAccess
    {
        private const int BufferSize = 81920;

        private readonly IFileSystem _fileSystem;

        public LocalClientFileAccess(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public Task<ClientFile> GetFileAsync(string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default)
        {
            if (!_fileSystem.File.Exists(clientPath))
                throw new FileNotFoundException($"File not found: {clientPath}", clientPath);

            var stream = _fileSystem.FileStream.New(clientPath, FileMode.Open, FileAccess.Read);
            var fileName = _fileSystem.Path.GetFileName(clientPath);
            var length = stream.Length;

            progress?.Report(new FileTransferProgress(length, length));

            return Task.FromResult(new ClientFile(stream, fileName, length));
        }

        public async IAsyncEnumerable<ClientFile> GetFilesAsync(
            string clientGlobPattern,
            IProgress<FileTransferProgress> progress = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var matchedFiles = ExpandGlob(clientGlobPattern);

            foreach (var filePath in matchedFiles)
            {
                ct.ThrowIfCancellationRequested();
                yield return await GetFileAsync(filePath, progress, ct);
            }
        }

        public async Task SaveFileAsync(Stream content, string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default)
        {
            EnsureParentDirectory(clientPath);

            long? totalBytes = null;
            if (content.CanSeek)
                totalBytes = content.Length;

            long bytesWritten = 0;

            using (var fileStream = _fileSystem.FileStream.New(clientPath, FileMode.Create, FileAccess.Write))
            {
                var buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                    bytesWritten += bytesRead;
                    progress?.Report(new FileTransferProgress(bytesWritten, totalBytes));
                }
            }
        }

        public async Task SaveFileAsync(string sourcePath, string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default)
        {
            if (!_fileSystem.File.Exists(sourcePath))
                throw new FileNotFoundException($"Source file not found: {sourcePath}", sourcePath);

            EnsureParentDirectory(clientPath);

            using (var sourceStream = _fileSystem.FileStream.New(sourcePath, FileMode.Open, FileAccess.Read))
            {
                var totalBytes = sourceStream.Length;
                long bytesWritten = 0;

                using (var destStream = _fileSystem.FileStream.New(clientPath, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[BufferSize];
                    int bytesRead;

                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        await destStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                        bytesWritten += bytesRead;
                        progress?.Report(new FileTransferProgress(bytesWritten, totalBytes));
                    }
                }
            }
        }

        /// <summary>
        /// Expands a glob pattern against the local file system and returns matching full paths.
        /// Uses Microsoft.Extensions.FileSystemGlobbing for pattern matching and a regex
        /// post-filter for ? wildcards (which FileSystemGlobbing doesn't support natively).
        /// </summary>
        internal List<string> ExpandGlob(string pattern)
        {
            var (baseDir, searchPattern) = ParseGlobPattern(pattern);

            if (!_fileSystem.Directory.Exists(baseDir))
                return new List<string>();

            var originalPattern = searchPattern;
            var matcherPattern = searchPattern.Replace('?', '*');

            var matcher = new Matcher();
            matcher.AddInclude(matcherPattern);

            string[] allFiles;
            try
            {
                allFiles = _fileSystem.Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories);
            }
            catch (DirectoryNotFoundException)
            {
                return new List<string>();
            }

            var inMemoryDir = new InMemoryDirectoryInfo(baseDir, allFiles);
            var result = matcher.Execute(inMemoryDir);

            var matchedFiles = result.Files
                .Select(f => _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(baseDir, f.Path)))
                .ToList();

            // Apply ? wildcard post-filtering
            if (originalPattern.Contains('?'))
            {
                var regex = GlobPatternToRegex(originalPattern);
                matchedFiles = matchedFiles
                    .Where(f => regex.IsMatch(_fileSystem.Path.GetFileName(f)))
                    .ToList();
            }

            return matchedFiles;
        }

        private (string baseDir, string pattern) ParseGlobPattern(string source)
        {
            var normalizedSource = source.Replace('\\', '/');
            var segments = normalizedSource.Split('/');

            var baseSegments = new List<string>();
            var patternSegments = new List<string>();
            var inPattern = false;

            foreach (var seg in segments)
            {
                if (inPattern || seg.Contains('*') || seg.Contains('?'))
                {
                    inPattern = true;
                    patternSegments.Add(seg);
                }
                else
                {
                    baseSegments.Add(seg);
                }
            }

            var baseDir = baseSegments.Count > 0
                ? string.Join(_fileSystem.Path.DirectorySeparatorChar.ToString(), baseSegments)
                : _fileSystem.Directory.GetCurrentDirectory();

            if (string.IsNullOrWhiteSpace(baseDir))
                baseDir = _fileSystem.Directory.GetCurrentDirectory();

            var resultPattern = string.Join("/", patternSegments);

            return (baseDir, resultPattern);
        }

        private static Regex GlobPatternToRegex(string pattern)
        {
            var segments = pattern.Replace('\\', '/').Split('/');
            var filePattern = segments[^1];

            var regexPattern = Regex.Escape(filePattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".");

            return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase);
        }

        private void EnsureParentDirectory(string filePath)
        {
            var directory = _fileSystem.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !_fileSystem.Directory.Exists(directory))
                _fileSystem.Directory.CreateDirectory(directory);
        }
    }
}
