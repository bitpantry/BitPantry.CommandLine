using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// Path operations wrapper that delegates to the inner IPath.
    /// Path operations are pure string manipulations that don't access the file system,
    /// so they don't need path validation (validation happens when the path is used for I/O).
    /// </summary>
    public class SandboxedPath : IPath
    {
        private readonly SandboxedFileSystem _sandboxedFileSystem;
        private readonly IPath _innerPath;

        public SandboxedPath(SandboxedFileSystem sandboxedFileSystem, IPath innerPath)
        {
            _sandboxedFileSystem = sandboxedFileSystem;
            _innerPath = innerPath;
        }

        public IFileSystem FileSystem => _sandboxedFileSystem;

        #region Characters and Separators

        public char AltDirectorySeparatorChar => _innerPath.AltDirectorySeparatorChar;
        public char DirectorySeparatorChar => _innerPath.DirectorySeparatorChar;
        public char PathSeparator => _innerPath.PathSeparator;
        public char VolumeSeparatorChar => _innerPath.VolumeSeparatorChar;

        public char[] GetInvalidFileNameChars() => _innerPath.GetInvalidFileNameChars();
        public char[] GetInvalidPathChars() => _innerPath.GetInvalidPathChars();

        #endregion

        #region Combine

        public string Combine(string path1, string path2) => _innerPath.Combine(path1, path2);
        public string Combine(string path1, string path2, string path3) => _innerPath.Combine(path1, path2, path3);
        public string Combine(string path1, string path2, string path3, string path4) => _innerPath.Combine(path1, path2, path3, path4);
        public string Combine(params string[] paths) => _innerPath.Combine(paths);

        #endregion

        #region Join

        public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2) => _innerPath.Join(path1, path2);
        public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3) => _innerPath.Join(path1, path2, path3);
        public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4) => _innerPath.Join(path1, path2, path3, path4);
        public string Join(string? path1, string? path2) => _innerPath.Join(path1, path2);
        public string Join(string? path1, string? path2, string? path3) => _innerPath.Join(path1, path2, path3);
        public string Join(string? path1, string? path2, string? path3, string? path4) => _innerPath.Join(path1, path2, path3, path4);
        public string Join(params string?[] paths) => _innerPath.Join(paths);

        public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten) 
            => _innerPath.TryJoin(path1, path2, destination, out charsWritten);
        public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten) 
            => _innerPath.TryJoin(path1, path2, path3, destination, out charsWritten);

        #endregion

        #region Get Parts

        public string? GetDirectoryName(string? path) => _innerPath.GetDirectoryName(path);
        public ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path) => _innerPath.GetDirectoryName(path);

        public string? GetFileName(string? path) => _innerPath.GetFileName(path);
        public ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path) => _innerPath.GetFileName(path);

        public string? GetFileNameWithoutExtension(string? path) => _innerPath.GetFileNameWithoutExtension(path);
        public ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path) => _innerPath.GetFileNameWithoutExtension(path);

        public string? GetExtension(string? path) => _innerPath.GetExtension(path);
        public ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path) => _innerPath.GetExtension(path);

        public string? GetPathRoot(string? path) => _innerPath.GetPathRoot(path);
        public ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path) => _innerPath.GetPathRoot(path);

        #endregion

        #region Change / Check / Full Path

        public string ChangeExtension(string? path, string? extension) => _innerPath.ChangeExtension(path, extension);

        public bool HasExtension([NotNullWhen(true)] string? path) => _innerPath.HasExtension(path);
        public bool HasExtension(ReadOnlySpan<char> path) => _innerPath.HasExtension(path);

        public bool IsPathRooted([NotNullWhen(true)] string? path) => _innerPath.IsPathRooted(path);
        public bool IsPathRooted(ReadOnlySpan<char> path) => _innerPath.IsPathRooted(path);

        public bool IsPathFullyQualified(string path) => _innerPath.IsPathFullyQualified(path);
        public bool IsPathFullyQualified(ReadOnlySpan<char> path) => _innerPath.IsPathFullyQualified(path);

        public string GetFullPath(string path) => _innerPath.GetFullPath(path);
        public string GetFullPath(string path, string basePath) => _innerPath.GetFullPath(path, basePath);

        public string GetRelativePath(string relativeTo, string path) => _innerPath.GetRelativePath(relativeTo, path);

        #endregion

        #region Temp / Random

        public string GetTempPath() => _innerPath.GetTempPath();

        public string GetTempFileName() 
            => throw new NotSupportedException("GetTempFileName is not supported in sandboxed file system - use a path within the sandbox instead");

        public string GetRandomFileName() => _innerPath.GetRandomFileName();

        #endregion

        #region Ends In Directory Separator

        public bool EndsInDirectorySeparator(string path) => _innerPath.EndsInDirectorySeparator(path);
        public bool EndsInDirectorySeparator(ReadOnlySpan<char> path) => _innerPath.EndsInDirectorySeparator(path);

        public string TrimEndingDirectorySeparator(string path) => _innerPath.TrimEndingDirectorySeparator(path);
        public ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) => _innerPath.TrimEndingDirectorySeparator(path);

        #endregion

        #region Exists

        public bool Exists([NotNullWhen(true)] string? path) => _innerPath.Exists(path);

        #endregion
    }
}
