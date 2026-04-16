using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

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

        private void EnsureParentDirectory(string filePath)
        {
            var directory = _fileSystem.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !_fileSystem.Directory.Exists(directory))
                _fileSystem.Directory.CreateDirectory(directory);
        }
    }
}
