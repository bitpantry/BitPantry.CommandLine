using System.Diagnostics;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// FileVersionInfoFactory wrapper that validates paths through PathValidator before delegating.
    /// </summary>
    public class SandboxedFileVersionInfoFactory : IFileVersionInfoFactory
    {
        private readonly SandboxedFileSystem _sandboxedFileSystem;
        private readonly IFileVersionInfoFactory _innerFactory;
        private readonly PathValidator _pathValidator;

        public SandboxedFileVersionInfoFactory(SandboxedFileSystem sandboxedFileSystem, IFileVersionInfoFactory innerFactory, PathValidator pathValidator)
        {
            _sandboxedFileSystem = sandboxedFileSystem;
            _innerFactory = innerFactory;
            _pathValidator = pathValidator;
        }

        public IFileSystem FileSystem => _sandboxedFileSystem;

        private string V(string path) => _pathValidator.ValidatePath(path);

        /// <summary>
        /// Gets version information for the specified file after validating the path is within the sandbox.
        /// </summary>
        public IFileVersionInfo GetVersionInfo(string fileName) => _innerFactory.GetVersionInfo(V(fileName));
    }
}
