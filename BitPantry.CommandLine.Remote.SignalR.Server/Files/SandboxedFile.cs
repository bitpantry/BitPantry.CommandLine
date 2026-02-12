using System.IO.Abstractions;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// File operations wrapper that validates paths through PathValidator before delegating to the inner IFile.
    /// All relative paths are resolved relative to StorageRootPath.
    /// Implements IFile by delegating all operations to the inner file system after path validation.
    /// </summary>
    public class SandboxedFile : IFile
    {
        private readonly SandboxedFileSystem _sandboxedFileSystem;
        private readonly IFile _innerFile;
        private readonly PathValidator _pathValidator;
        private readonly FileSizeValidator _fileSizeValidator;
        private readonly ExtensionValidator _extensionValidator;

        public SandboxedFile(SandboxedFileSystem sandboxedFileSystem, IFile innerFile, PathValidator pathValidator)
            : this(sandboxedFileSystem, innerFile, pathValidator, null, null)
        {
        }

        public SandboxedFile(
            SandboxedFileSystem sandboxedFileSystem, 
            IFile innerFile, 
            PathValidator pathValidator,
            FileSizeValidator fileSizeValidator,
            ExtensionValidator extensionValidator)
        {
            _sandboxedFileSystem = sandboxedFileSystem;
            _innerFile = innerFile;
            _pathValidator = pathValidator;
            _fileSizeValidator = fileSizeValidator;
            _extensionValidator = extensionValidator;
        }

        public IFileSystem FileSystem => _sandboxedFileSystem;

        private string V(string path) => _pathValidator.ValidatePath(path);
        private string? VN(string? path) => path == null ? null : _pathValidator.ValidatePath(path);

        /// <summary>
        /// Validates extension and size for write operations if validators are configured.
        /// </summary>
        private void ValidateForWrite(string path, long? contentSize = null)
        {
            _extensionValidator?.ValidateExtension(path);
            if (contentSize.HasValue)
            {
                _fileSizeValidator?.ValidateStreamingBytes(contentSize.Value);
            }
        }

        #region Exists / Delete / Copy / Move

        public bool Exists(string? path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try { return _innerFile.Exists(V(path)); }
            catch (UnauthorizedAccessException) { throw; }
        }

        public void Delete(string path) => _innerFile.Delete(V(path));

        public void Copy(string sourceFileName, string destFileName) 
            => _innerFile.Copy(V(sourceFileName), V(destFileName));

        public void Copy(string sourceFileName, string destFileName, bool overwrite) 
            => _innerFile.Copy(V(sourceFileName), V(destFileName), overwrite);

        public void Move(string sourceFileName, string destFileName) 
            => _innerFile.Move(V(sourceFileName), V(destFileName));

        public void Move(string sourceFileName, string destFileName, bool overwrite) 
            => _innerFile.Move(V(sourceFileName), V(destFileName), overwrite);

        public void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName) 
            => _innerFile.Replace(V(sourceFileName), V(destinationFileName), VN(destinationBackupFileName));

        public void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors) 
            => _innerFile.Replace(V(sourceFileName), V(destinationFileName), VN(destinationBackupFileName), ignoreMetadataErrors);

        #endregion

        #region Read Operations

        public string ReadAllText(string path) => _innerFile.ReadAllText(V(path));
        public string ReadAllText(string path, Encoding encoding) => _innerFile.ReadAllText(V(path), encoding);
        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) 
            => _innerFile.ReadAllTextAsync(V(path), cancellationToken);
        public Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) 
            => _innerFile.ReadAllTextAsync(V(path), encoding, cancellationToken);

        public byte[] ReadAllBytes(string path) => _innerFile.ReadAllBytes(V(path));
        public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) 
            => _innerFile.ReadAllBytesAsync(V(path), cancellationToken);

        public string[] ReadAllLines(string path) => _innerFile.ReadAllLines(V(path));
        public string[] ReadAllLines(string path, Encoding encoding) => _innerFile.ReadAllLines(V(path), encoding);
        public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default) 
            => _innerFile.ReadAllLinesAsync(V(path), cancellationToken);
        public Task<string[]> ReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) 
            => _innerFile.ReadAllLinesAsync(V(path), encoding, cancellationToken);

        public IEnumerable<string> ReadLines(string path) => _innerFile.ReadLines(V(path));
        public IEnumerable<string> ReadLines(string path, Encoding encoding) => _innerFile.ReadLines(V(path), encoding);
        public IAsyncEnumerable<string> ReadLinesAsync(string path, CancellationToken cancellationToken = default) 
            => _innerFile.ReadLinesAsync(V(path), cancellationToken);
        public IAsyncEnumerable<string> ReadLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) 
            => _innerFile.ReadLinesAsync(V(path), encoding, cancellationToken);

        #endregion

        #region Write Operations

        public void WriteAllText(string path, string? contents)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, contents?.Length);
            EnsureParentDirectoryExists(validatedPath);
            _innerFile.WriteAllText(validatedPath, contents);
        }

        public void WriteAllText(string path, string? contents, Encoding encoding)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, contents != null ? encoding.GetByteCount(contents) : null);
            EnsureParentDirectoryExists(validatedPath);
            _innerFile.WriteAllText(validatedPath, contents, encoding);
        }

        public async Task WriteAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, contents?.Length);
            EnsureParentDirectoryExists(validatedPath);
            await _innerFile.WriteAllTextAsync(validatedPath, contents, cancellationToken);
        }

        public async Task WriteAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, contents != null ? encoding.GetByteCount(contents) : null);
            EnsureParentDirectoryExists(validatedPath);
            await _innerFile.WriteAllTextAsync(validatedPath, contents, encoding, cancellationToken);
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, bytes?.Length);
            EnsureParentDirectoryExists(validatedPath);
            _innerFile.WriteAllBytes(validatedPath, bytes);
        }

        public async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, bytes?.Length);
            EnsureParentDirectoryExists(validatedPath);
            await _innerFile.WriteAllBytesAsync(validatedPath, bytes, cancellationToken);
        }

        public void WriteAllLines(string path, IEnumerable<string> contents)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, null); // Extension only - can't pre-compute size for IEnumerable
            EnsureParentDirectoryExists(validatedPath);
            _innerFile.WriteAllLines(validatedPath, contents);
        }

        public void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, null); // Extension only
            EnsureParentDirectoryExists(validatedPath);
            _innerFile.WriteAllLines(validatedPath, contents, encoding);
        }

        public void WriteAllLines(string path, string[] contents)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, null); // Extension only - size would need byte calculation
            EnsureParentDirectoryExists(validatedPath);
            _innerFile.WriteAllLines(validatedPath, contents);
        }

        public void WriteAllLines(string path, string[] contents, Encoding encoding)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, null); // Extension only
            EnsureParentDirectoryExists(validatedPath);
            _innerFile.WriteAllLines(validatedPath, contents, encoding);
        }

        public async Task WriteAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, null); // Extension only
            EnsureParentDirectoryExists(validatedPath);
            await _innerFile.WriteAllLinesAsync(validatedPath, contents, cancellationToken);
        }

        public async Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            var validatedPath = V(path);
            ValidateForWrite(path, null); // Extension only
            EnsureParentDirectoryExists(validatedPath);
            await _innerFile.WriteAllLinesAsync(validatedPath, contents, encoding, cancellationToken);
        }

        #endregion

        #region Append Operations

        public void AppendAllText(string path, string? contents) => _innerFile.AppendAllText(V(path), contents);
        public void AppendAllText(string path, string? contents, Encoding encoding) => _innerFile.AppendAllText(V(path), contents, encoding);
        public Task AppendAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default) 
            => _innerFile.AppendAllTextAsync(V(path), contents, cancellationToken);
        public Task AppendAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default) 
            => _innerFile.AppendAllTextAsync(V(path), contents, encoding, cancellationToken);

        public void AppendAllLines(string path, IEnumerable<string> contents) => _innerFile.AppendAllLines(V(path), contents);
        public void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding) => _innerFile.AppendAllLines(V(path), contents, encoding);
        public Task AppendAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default) 
            => _innerFile.AppendAllLinesAsync(V(path), contents, cancellationToken);
        public Task AppendAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default) 
            => _innerFile.AppendAllLinesAsync(V(path), contents, encoding, cancellationToken);

        public StreamWriter AppendText(string path) => _innerFile.AppendText(V(path));

        #endregion

        #region Stream / Open Operations

        public FileSystemStream Create(string path)
        {
            var validatedPath = V(path);
            EnsureParentDirectoryExists(validatedPath);
            return _innerFile.Create(validatedPath);
        }

        public FileSystemStream Create(string path, int bufferSize)
        {
            var validatedPath = V(path);
            EnsureParentDirectoryExists(validatedPath);
            return _innerFile.Create(validatedPath, bufferSize);
        }

        public FileSystemStream Create(string path, int bufferSize, FileOptions options)
        {
            var validatedPath = V(path);
            EnsureParentDirectoryExists(validatedPath);
            return _innerFile.Create(validatedPath, bufferSize, options);
        }

        public StreamWriter CreateText(string path)
        {
            var validatedPath = V(path);
            EnsureParentDirectoryExists(validatedPath);
            return _innerFile.CreateText(validatedPath);
        }

        public FileSystemStream Open(string path, FileMode mode) => _innerFile.Open(V(path), mode);
        public FileSystemStream Open(string path, FileMode mode, FileAccess access) => _innerFile.Open(V(path), mode, access);
        public FileSystemStream Open(string path, FileMode mode, FileAccess access, FileShare share) => _innerFile.Open(V(path), mode, access, share);
        public FileSystemStream Open(string path, FileStreamOptions options) => _innerFile.Open(V(path), options);

        public FileSystemStream OpenRead(string path) => _innerFile.OpenRead(V(path));
        public StreamReader OpenText(string path) => _innerFile.OpenText(V(path));
        public FileSystemStream OpenWrite(string path) => _innerFile.OpenWrite(V(path));

        #endregion

        #region Attributes / Times

        public FileAttributes GetAttributes(string path) => _innerFile.GetAttributes(V(path));
        public FileAttributes GetAttributes(SafeFileHandle fileHandle) => _innerFile.GetAttributes(fileHandle);
        public void SetAttributes(string path, FileAttributes fileAttributes) => _innerFile.SetAttributes(V(path), fileAttributes);
        public void SetAttributes(SafeFileHandle fileHandle, FileAttributes fileAttributes) => _innerFile.SetAttributes(fileHandle, fileAttributes);

        public DateTime GetCreationTime(string path) => _innerFile.GetCreationTime(V(path));
        public DateTime GetCreationTime(SafeFileHandle fileHandle) => _innerFile.GetCreationTime(fileHandle);
        public DateTime GetCreationTimeUtc(string path) => _innerFile.GetCreationTimeUtc(V(path));
        public DateTime GetCreationTimeUtc(SafeFileHandle fileHandle) => _innerFile.GetCreationTimeUtc(fileHandle);
        public void SetCreationTime(string path, DateTime creationTime) => _innerFile.SetCreationTime(V(path), creationTime);
        public void SetCreationTime(SafeFileHandle fileHandle, DateTime creationTime) => _innerFile.SetCreationTime(fileHandle, creationTime);
        public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => _innerFile.SetCreationTimeUtc(V(path), creationTimeUtc);
        public void SetCreationTimeUtc(SafeFileHandle fileHandle, DateTime creationTimeUtc) => _innerFile.SetCreationTimeUtc(fileHandle, creationTimeUtc);

        public DateTime GetLastAccessTime(string path) => _innerFile.GetLastAccessTime(V(path));
        public DateTime GetLastAccessTime(SafeFileHandle fileHandle) => _innerFile.GetLastAccessTime(fileHandle);
        public DateTime GetLastAccessTimeUtc(string path) => _innerFile.GetLastAccessTimeUtc(V(path));
        public DateTime GetLastAccessTimeUtc(SafeFileHandle fileHandle) => _innerFile.GetLastAccessTimeUtc(fileHandle);
        public void SetLastAccessTime(string path, DateTime lastAccessTime) => _innerFile.SetLastAccessTime(V(path), lastAccessTime);
        public void SetLastAccessTime(SafeFileHandle fileHandle, DateTime lastAccessTime) => _innerFile.SetLastAccessTime(fileHandle, lastAccessTime);
        public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => _innerFile.SetLastAccessTimeUtc(V(path), lastAccessTimeUtc);
        public void SetLastAccessTimeUtc(SafeFileHandle fileHandle, DateTime lastAccessTimeUtc) => _innerFile.SetLastAccessTimeUtc(fileHandle, lastAccessTimeUtc);

        public DateTime GetLastWriteTime(string path) => _innerFile.GetLastWriteTime(V(path));
        public DateTime GetLastWriteTime(SafeFileHandle fileHandle) => _innerFile.GetLastWriteTime(fileHandle);
        public DateTime GetLastWriteTimeUtc(string path) => _innerFile.GetLastWriteTimeUtc(V(path));
        public DateTime GetLastWriteTimeUtc(SafeFileHandle fileHandle) => _innerFile.GetLastWriteTimeUtc(fileHandle);
        public void SetLastWriteTime(string path, DateTime lastWriteTime) => _innerFile.SetLastWriteTime(V(path), lastWriteTime);
        public void SetLastWriteTime(SafeFileHandle fileHandle, DateTime lastWriteTime) => _innerFile.SetLastWriteTime(fileHandle, lastWriteTime);
        public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => _innerFile.SetLastWriteTimeUtc(V(path), lastWriteTimeUtc);
        public void SetLastWriteTimeUtc(SafeFileHandle fileHandle, DateTime lastWriteTimeUtc) => _innerFile.SetLastWriteTimeUtc(fileHandle, lastWriteTimeUtc);

        #endregion

        #region Unix File Mode

        public UnixFileMode GetUnixFileMode(string path) => _innerFile.GetUnixFileMode(V(path));
        public UnixFileMode GetUnixFileMode(SafeFileHandle fileHandle) => _innerFile.GetUnixFileMode(fileHandle);
        public void SetUnixFileMode(string path, UnixFileMode mode) => _innerFile.SetUnixFileMode(V(path), mode);
        public void SetUnixFileMode(SafeFileHandle fileHandle, UnixFileMode mode) => _innerFile.SetUnixFileMode(fileHandle, mode);

        #endregion

        #region Encryption (Windows-specific)

        public void Encrypt(string path) => _innerFile.Encrypt(V(path));
        public void Decrypt(string path) => _innerFile.Decrypt(V(path));

        #endregion

        #region Symbolic Links

        public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget) 
            => _innerFile.CreateSymbolicLink(V(path), V(pathToTarget));

        public IFileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget) 
            => _innerFile.ResolveLinkTarget(V(linkPath), returnFinalTarget);

        #endregion

        #region Helper Methods

        private void EnsureParentDirectoryExists(string validatedPath)
        {
            var directory = Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrEmpty(directory) && !_sandboxedFileSystem.Directory.Exists(directory))
            {
                _sandboxedFileSystem.Directory.CreateDirectory(directory);
            }
        }

        #endregion
    }
}
