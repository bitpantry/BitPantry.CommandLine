using System;
using System.IO;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Client
{
    /// <summary>
    /// A disposable handle to a client-side file, providing stream access and metadata.
    /// </summary>
    public sealed class ClientFile : IAsyncDisposable
    {
        public Stream Stream { get; }
        public string FileName { get; }
        public long Length { get; }

        private readonly Func<ValueTask> _cleanupAsync;

        public ClientFile(Stream stream, string fileName, long length, Func<ValueTask> cleanupAsync = null)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Length = length;
            _cleanupAsync = cleanupAsync;
        }

        public async ValueTask DisposeAsync()
        {
            await Stream.DisposeAsync();
            if (_cleanupAsync != null)
                await _cleanupAsync();
        }
    }
}
