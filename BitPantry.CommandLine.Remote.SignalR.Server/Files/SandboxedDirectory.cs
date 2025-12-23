using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// Directory operations wrapper that validates paths through PathValidator before delegating to the inner IDirectory.
    /// All relative paths are resolved relative to StorageRootPath.
    /// Implements IDirectory by delegating all operations to the inner file system after path validation.
    /// </summary>
    public class SandboxedDirectory : IDirectory
    {
        private readonly SandboxedFileSystem _sandboxedFileSystem;
        private readonly IDirectory _innerDirectory;
        private readonly PathValidator _pathValidator;

        public SandboxedDirectory(SandboxedFileSystem sandboxedFileSystem, IDirectory innerDirectory, PathValidator pathValidator)
        {
            _sandboxedFileSystem = sandboxedFileSystem;
            _innerDirectory = innerDirectory;
            _pathValidator = pathValidator;
        }

        public IFileSystem FileSystem => _sandboxedFileSystem;

        private string V(string path) => _pathValidator.ValidatePath(path);

        #region Exists / Create / Delete / Move

        public bool Exists(string? path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try { return _innerDirectory.Exists(V(path)); }
            catch (UnauthorizedAccessException) { throw; }
        }

        public IDirectoryInfo CreateDirectory(string path) => _innerDirectory.CreateDirectory(V(path));

        public IDirectoryInfo CreateDirectory(string path, UnixFileMode unixCreateMode) 
            => _innerDirectory.CreateDirectory(V(path), unixCreateMode);

        public IDirectoryInfo CreateTempSubdirectory(string? prefix = null) 
            => throw new NotSupportedException("CreateTempSubdirectory is not supported in sandboxed file system - use CreateDirectory with a path within the sandbox");

        public void Delete(string path) => _innerDirectory.Delete(V(path));
        public void Delete(string path, bool recursive) => _innerDirectory.Delete(V(path), recursive);

        public void Move(string sourceDirName, string destDirName) 
            => _innerDirectory.Move(V(sourceDirName), V(destDirName));

        #endregion

        #region GetFiles / GetDirectories / GetFileSystemEntries

        public string[] GetFiles(string path) => _innerDirectory.GetFiles(V(path));
        public string[] GetFiles(string path, string searchPattern) => _innerDirectory.GetFiles(V(path), searchPattern);
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) 
            => _innerDirectory.GetFiles(V(path), searchPattern, searchOption);
        public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) 
            => _innerDirectory.GetFiles(V(path), searchPattern, enumerationOptions);

        public string[] GetDirectories(string path) => _innerDirectory.GetDirectories(V(path));
        public string[] GetDirectories(string path, string searchPattern) => _innerDirectory.GetDirectories(V(path), searchPattern);
        public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) 
            => _innerDirectory.GetDirectories(V(path), searchPattern, searchOption);
        public string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) 
            => _innerDirectory.GetDirectories(V(path), searchPattern, enumerationOptions);

        public string[] GetFileSystemEntries(string path) => _innerDirectory.GetFileSystemEntries(V(path));
        public string[] GetFileSystemEntries(string path, string searchPattern) => _innerDirectory.GetFileSystemEntries(V(path), searchPattern);
        public string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption) 
            => _innerDirectory.GetFileSystemEntries(V(path), searchPattern, searchOption);
        public string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) 
            => _innerDirectory.GetFileSystemEntries(V(path), searchPattern, enumerationOptions);

        #endregion

        #region EnumerateFiles / EnumerateDirectories / EnumerateFileSystemEntries

        public IEnumerable<string> EnumerateFiles(string path) => _innerDirectory.EnumerateFiles(V(path));
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => _innerDirectory.EnumerateFiles(V(path), searchPattern);
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) 
            => _innerDirectory.EnumerateFiles(V(path), searchPattern, searchOption);
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) 
            => _innerDirectory.EnumerateFiles(V(path), searchPattern, enumerationOptions);

        public IEnumerable<string> EnumerateDirectories(string path) => _innerDirectory.EnumerateDirectories(V(path));
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) 
            => _innerDirectory.EnumerateDirectories(V(path), searchPattern);
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption) 
            => _innerDirectory.EnumerateDirectories(V(path), searchPattern, searchOption);
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) 
            => _innerDirectory.EnumerateDirectories(V(path), searchPattern, enumerationOptions);

        public IEnumerable<string> EnumerateFileSystemEntries(string path) => _innerDirectory.EnumerateFileSystemEntries(V(path));
        public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) 
            => _innerDirectory.EnumerateFileSystemEntries(V(path), searchPattern);
        public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) 
            => _innerDirectory.EnumerateFileSystemEntries(V(path), searchPattern, searchOption);
        public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) 
            => _innerDirectory.EnumerateFileSystemEntries(V(path), searchPattern, enumerationOptions);

        #endregion

        #region Times

        public DateTime GetCreationTime(string path) => _innerDirectory.GetCreationTime(V(path));
        public DateTime GetCreationTimeUtc(string path) => _innerDirectory.GetCreationTimeUtc(V(path));
        public void SetCreationTime(string path, DateTime creationTime) => _innerDirectory.SetCreationTime(V(path), creationTime);
        public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => _innerDirectory.SetCreationTimeUtc(V(path), creationTimeUtc);

        public DateTime GetLastAccessTime(string path) => _innerDirectory.GetLastAccessTime(V(path));
        public DateTime GetLastAccessTimeUtc(string path) => _innerDirectory.GetLastAccessTimeUtc(V(path));
        public void SetLastAccessTime(string path, DateTime lastAccessTime) => _innerDirectory.SetLastAccessTime(V(path), lastAccessTime);
        public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => _innerDirectory.SetLastAccessTimeUtc(V(path), lastAccessTimeUtc);

        public DateTime GetLastWriteTime(string path) => _innerDirectory.GetLastWriteTime(V(path));
        public DateTime GetLastWriteTimeUtc(string path) => _innerDirectory.GetLastWriteTimeUtc(V(path));
        public void SetLastWriteTime(string path, DateTime lastWriteTime) => _innerDirectory.SetLastWriteTime(V(path), lastWriteTime);
        public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => _innerDirectory.SetLastWriteTimeUtc(V(path), lastWriteTimeUtc);

        #endregion

        #region Current Directory / Root / Parent / Logical Drives

        public string GetCurrentDirectory() => _innerDirectory.GetCurrentDirectory();

        public void SetCurrentDirectory(string path) 
            => throw new NotSupportedException("SetCurrentDirectory is not supported in sandboxed file system");

        public string GetDirectoryRoot(string path) => _innerDirectory.GetDirectoryRoot(V(path));

        public IDirectoryInfo? GetParent(string path) => _innerDirectory.GetParent(V(path));

        public string[] GetLogicalDrives() 
            => throw new NotSupportedException("GetLogicalDrives is not supported in sandboxed file system");

        #endregion

        #region Symbolic Links

        public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget) 
            => _innerDirectory.CreateSymbolicLink(V(path), V(pathToTarget));

        public IFileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget) 
            => _innerDirectory.ResolveLinkTarget(V(linkPath), returnFinalTarget);

        #endregion
    }
}
