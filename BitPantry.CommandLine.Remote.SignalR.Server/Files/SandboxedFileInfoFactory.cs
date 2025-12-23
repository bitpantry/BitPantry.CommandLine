using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// FileInfoFactory wrapper that validates paths through PathValidator before creating IFileInfo instances.
    /// </summary>
    public class SandboxedFileInfoFactory : IFileInfoFactory
    {
        private readonly SandboxedFileSystem _sandboxedFileSystem;
        private readonly IFileInfoFactory _innerFactory;
        private readonly PathValidator _pathValidator;

        public SandboxedFileInfoFactory(SandboxedFileSystem sandboxedFileSystem, IFileInfoFactory innerFactory, PathValidator pathValidator)
        {
            _sandboxedFileSystem = sandboxedFileSystem;
            _innerFactory = innerFactory;
            _pathValidator = pathValidator;
        }

        public IFileSystem FileSystem => _sandboxedFileSystem;

        private string V(string path) => _pathValidator.ValidatePath(path);

        /// <summary>
        /// Creates a new IFileInfo for the specified path after validating it is within the sandbox.
        /// </summary>
        public IFileInfo New(string fileName) => _innerFactory.New(V(fileName));

        /// <summary>
        /// Wraps an existing FileInfo. The path is validated to ensure it's within the sandbox.
        /// </summary>
        public IFileInfo Wrap(FileInfo fileInfo)
        {
            // Validate the path is within the sandbox
            V(fileInfo.FullName);
            return _innerFactory.Wrap(fileInfo);
        }
    }
}
