using System.IO.Abstractions;
using Microsoft.Win32.SafeHandles;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// FileStreamFactory wrapper that validates paths through PathValidator before creating FileSystemStream instances.
    /// </summary>
    public class SandboxedFileStreamFactory : IFileStreamFactory
    {
        private readonly SandboxedFileSystem _sandboxedFileSystem;
        private readonly IFileStreamFactory _innerFactory;
        private readonly PathValidator _pathValidator;

        public SandboxedFileStreamFactory(SandboxedFileSystem sandboxedFileSystem, IFileStreamFactory innerFactory, PathValidator pathValidator)
        {
            _sandboxedFileSystem = sandboxedFileSystem;
            _innerFactory = innerFactory;
            _pathValidator = pathValidator;
        }

        public IFileSystem FileSystem => _sandboxedFileSystem;

        private string V(string path) => _pathValidator.ValidatePath(path);

        #region New with path

        public FileSystemStream New(string path, FileMode mode) => _innerFactory.New(V(path), mode);
        public FileSystemStream New(string path, FileMode mode, FileAccess access) => _innerFactory.New(V(path), mode, access);
        public FileSystemStream New(string path, FileMode mode, FileAccess access, FileShare share) => _innerFactory.New(V(path), mode, access, share);
        public FileSystemStream New(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) => _innerFactory.New(V(path), mode, access, share, bufferSize);
        public FileSystemStream New(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) => _innerFactory.New(V(path), mode, access, share, bufferSize, useAsync);
        public FileSystemStream New(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) => _innerFactory.New(V(path), mode, access, share, bufferSize, options);

        public FileSystemStream New(string path, FileStreamOptions options) => _innerFactory.New(V(path), options);

        #endregion

        #region New with SafeFileHandle

        public FileSystemStream New(SafeFileHandle handle, FileAccess access) => _innerFactory.New(handle, access);
        public FileSystemStream New(SafeFileHandle handle, FileAccess access, int bufferSize) => _innerFactory.New(handle, access, bufferSize);
        public FileSystemStream New(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) => _innerFactory.New(handle, access, bufferSize, isAsync);

        #endregion

        #region Wrap

        public FileSystemStream Wrap(FileStream fileStream) => _innerFactory.Wrap(fileStream);

        #endregion
    }
}
