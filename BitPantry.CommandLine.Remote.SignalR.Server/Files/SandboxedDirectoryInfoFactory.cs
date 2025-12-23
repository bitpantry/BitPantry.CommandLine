using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// DirectoryInfoFactory wrapper that validates paths through PathValidator before creating IDirectoryInfo instances.
    /// </summary>
    public class SandboxedDirectoryInfoFactory : IDirectoryInfoFactory
    {
        private readonly SandboxedFileSystem _sandboxedFileSystem;
        private readonly IDirectoryInfoFactory _innerFactory;
        private readonly PathValidator _pathValidator;

        public SandboxedDirectoryInfoFactory(SandboxedFileSystem sandboxedFileSystem, IDirectoryInfoFactory innerFactory, PathValidator pathValidator)
        {
            _sandboxedFileSystem = sandboxedFileSystem;
            _innerFactory = innerFactory;
            _pathValidator = pathValidator;
        }

        public IFileSystem FileSystem => _sandboxedFileSystem;

        private string V(string path) => _pathValidator.ValidatePath(path);

        /// <summary>
        /// Creates a new IDirectoryInfo for the specified path after validating it is within the sandbox.
        /// </summary>
        public IDirectoryInfo New(string path) => _innerFactory.New(V(path));

        /// <summary>
        /// Wraps an existing DirectoryInfo. The path is validated to ensure it's within the sandbox.
        /// </summary>
        public IDirectoryInfo Wrap(DirectoryInfo directoryInfo)
        {
            // Validate the path is within the sandbox
            V(directoryInfo.FullName);
            return _innerFactory.Wrap(directoryInfo);
        }
    }
}
