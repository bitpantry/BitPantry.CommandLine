using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// A file system implementation that wraps operations on a real FileSystem
    /// and validates all paths through PathValidator to confine access to StorageRootPath.
    /// Used for server-side command execution on behalf of remote clients.
    /// 
    /// Design: Commands inject IFileSystem and work identically whether running locally
    /// (with unrestricted FileSystem) or on the server (with SandboxedFileSystem).
    /// </summary>
    public class SandboxedFileSystem : IFileSystem
    {
        private readonly IFileSystem _innerFileSystem;
        private readonly PathValidator _pathValidator;
        private readonly FileSizeValidator? _fileSizeValidator;
        private readonly ExtensionValidator? _extensionValidator;
        
        private readonly SandboxedFile _file;
        private readonly SandboxedDirectory _directory;
        private readonly SandboxedPath _path;
        private readonly SandboxedFileInfoFactory _fileInfoFactory;
        private readonly SandboxedDirectoryInfoFactory _directoryInfoFactory;
        private readonly SandboxedDriveInfoFactory _driveInfoFactory;
        private readonly SandboxedFileStreamFactory _fileStreamFactory;
        private readonly SandboxedFileSystemWatcherFactory _fileSystemWatcherFactory;
        private readonly SandboxedFileVersionInfoFactory _fileVersionInfoFactory;

        /// <summary>
        /// Creates a new SandboxedFileSystem that wraps file operations with path validation.
        /// </summary>
        /// <param name="innerFileSystem">The underlying file system to delegate to</param>
        /// <param name="pathValidator">The path validator that confines access to StorageRootPath</param>
        public SandboxedFileSystem(IFileSystem innerFileSystem, PathValidator pathValidator)
            : this(innerFileSystem, pathValidator, null, null)
        {
        }

        /// <summary>
        /// Creates a new SandboxedFileSystem with full validation support including size and extension restrictions.
        /// </summary>
        /// <param name="innerFileSystem">The underlying file system to delegate to</param>
        /// <param name="pathValidator">The path validator that confines access to StorageRootPath</param>
        /// <param name="fileSizeValidator">Optional validator for file size limits on write operations</param>
        /// <param name="extensionValidator">Optional validator for file extension restrictions on write operations</param>
        public SandboxedFileSystem(
            IFileSystem innerFileSystem, 
            PathValidator pathValidator,
            FileSizeValidator? fileSizeValidator,
            ExtensionValidator? extensionValidator)
        {
            _innerFileSystem = innerFileSystem ?? throw new ArgumentNullException(nameof(innerFileSystem));
            _pathValidator = pathValidator ?? throw new ArgumentNullException(nameof(pathValidator));
            _fileSizeValidator = fileSizeValidator;
            _extensionValidator = extensionValidator;

            _file = new SandboxedFile(this, innerFileSystem.File, pathValidator, fileSizeValidator, extensionValidator);
            _directory = new SandboxedDirectory(this, innerFileSystem.Directory, pathValidator);
            _path = new SandboxedPath(this, innerFileSystem.Path);
            _fileInfoFactory = new SandboxedFileInfoFactory(this, innerFileSystem.FileInfo, pathValidator);
            _directoryInfoFactory = new SandboxedDirectoryInfoFactory(this, innerFileSystem.DirectoryInfo, pathValidator);
            _driveInfoFactory = new SandboxedDriveInfoFactory(this);
            _fileStreamFactory = new SandboxedFileStreamFactory(this, innerFileSystem.FileStream, pathValidator);
            _fileSystemWatcherFactory = new SandboxedFileSystemWatcherFactory(this);
            _fileVersionInfoFactory = new SandboxedFileVersionInfoFactory(this, innerFileSystem.FileVersionInfo, pathValidator);
        }

        /// <inheritdoc />
        public IFile File => _file;

        /// <inheritdoc />
        public IDirectory Directory => _directory;

        /// <inheritdoc />
        public IPath Path => _path;

        /// <inheritdoc />
        public IFileInfoFactory FileInfo => _fileInfoFactory;

        /// <inheritdoc />
        public IDirectoryInfoFactory DirectoryInfo => _directoryInfoFactory;

        /// <inheritdoc />
        public IDriveInfoFactory DriveInfo => _driveInfoFactory;

        /// <inheritdoc />
        public IFileStreamFactory FileStream => _fileStreamFactory;

        /// <inheritdoc />
        public IFileSystemWatcherFactory FileSystemWatcher => _fileSystemWatcherFactory;

        /// <inheritdoc />
        public IFileVersionInfoFactory FileVersionInfo => _fileVersionInfoFactory;

        /// <summary>
        /// Gets the path validator used for path validation
        /// </summary>
        internal PathValidator PathValidator => _pathValidator;

        /// <summary>
        /// Gets the inner file system
        /// </summary>
        internal IFileSystem InnerFileSystem => _innerFileSystem;
    }

    #region Not Supported Factories

    /// <summary>
    /// DriveInfo is not supported in sandboxed context - commands should not access drive information
    /// </summary>
    internal class SandboxedDriveInfoFactory : IDriveInfoFactory
    {
        private readonly IFileSystem _fileSystem;
        public SandboxedDriveInfoFactory(IFileSystem fileSystem) => _fileSystem = fileSystem;
        public IFileSystem FileSystem => _fileSystem;
        public IDriveInfo New(string driveName) => throw new NotSupportedException("DriveInfo is not supported in sandboxed file system");
        public IDriveInfo Wrap(DriveInfo driveInfo) => throw new NotSupportedException("DriveInfo is not supported in sandboxed file system");
        public IDriveInfo[] GetDrives() => throw new NotSupportedException("DriveInfo is not supported in sandboxed file system");
    }

    /// <summary>
    /// FileSystemWatcher is not supported in sandboxed context - commands should not watch for file changes
    /// </summary>
    internal class SandboxedFileSystemWatcherFactory : IFileSystemWatcherFactory
    {
        private readonly IFileSystem _fileSystem;
        public SandboxedFileSystemWatcherFactory(IFileSystem fileSystem) => _fileSystem = fileSystem;
        public IFileSystem FileSystem => _fileSystem;
        public IFileSystemWatcher New() => throw new NotSupportedException("FileSystemWatcher is not supported in sandboxed file system");
        public IFileSystemWatcher New(string path) => throw new NotSupportedException("FileSystemWatcher is not supported in sandboxed file system");
        public IFileSystemWatcher New(string path, string filter) => throw new NotSupportedException("FileSystemWatcher is not supported in sandboxed file system");
        public IFileSystemWatcher Wrap(FileSystemWatcher fileSystemWatcher) => throw new NotSupportedException("FileSystemWatcher is not supported in sandboxed file system");
    }

    #endregion
}
