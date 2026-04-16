using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Client
{
    /// <summary>
    /// Provides location-transparent access to files on the client machine.
    /// Commands inject this service to read or write client-side files regardless of
    /// whether the command is running locally or remotely.
    /// </summary>
    public interface IClientFileAccess
    {
        /// <summary>
        /// Opens a file on the client machine for reading.
        /// </summary>
        /// <param name="clientPath">The path to the file on the client.</param>
        /// <param name="progress">Optional progress callback for transfer reporting.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="ClientFile"/> providing stream access to the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        Task<ClientFile> GetFileAsync(string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default);

        /// <summary>
        /// Saves a stream to a file on the client machine.
        /// Creates parent directories if they do not exist.
        /// </summary>
        /// <param name="content">The stream containing the file content.</param>
        /// <param name="clientPath">The destination path on the client.</param>
        /// <param name="progress">Optional progress callback for transfer reporting.</param>
        /// <param name="ct">Cancellation token.</param>
        Task SaveFileAsync(Stream content, string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default);

        /// <summary>
        /// Copies a source file to a destination path on the client machine.
        /// Creates parent directories if they do not exist.
        /// </summary>
        /// <param name="sourcePath">The path of the source file on the client.</param>
        /// <param name="clientPath">The destination path on the client.</param>
        /// <param name="progress">Optional progress callback for transfer reporting.</param>
        /// <param name="ct">Cancellation token.</param>
        Task SaveFileAsync(string sourcePath, string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default);
    }
}
