using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using BitPantry.CommandLine.Remote.SignalR.Server.Rpc;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.API;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class AddCommandLineHubTests
    {
        [TestMethod]
        public void AddCommandLineHub_DefaultConfiguration_RegistersFileTransferServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub();

            // Assert - FileTransferOptions should be registered
            var serviceProvider = services.BuildServiceProvider();
            var fileTransferOptions = serviceProvider.GetService<FileTransferOptions>();
            fileTransferOptions.Should().NotBeNull();
            fileTransferOptions.IsEnabled.Should().BeTrue();
        }

        [TestMethod]
        public void AddCommandLineHub_DefaultConfiguration_HasDefaultStorageRootPath()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var fileTransferOptions = serviceProvider.GetService<FileTransferOptions>();
            fileTransferOptions.StorageRootPath.Should().NotBeNullOrWhiteSpace();
            fileTransferOptions.StorageRootPath.Should().EndWith(FileTransferOptions.DefaultStorageDirectoryName);
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_DoesNotRegisterFileTransferEndpointService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            // Assert - FileTransferEndpointService should NOT be registered
            var serviceDescriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(FileTransferEndpointService));
            serviceDescriptor.Should().BeNull("FileTransferEndpointService should not be registered when file transfer is disabled");
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_DoesNotRegisterFileSystemRpcHandler()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            // Assert
            var serviceDescriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(FileSystemRpcHandler));
            serviceDescriptor.Should().BeNull("FileSystemRpcHandler should not be registered when file transfer is disabled");
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_DoesNotRegisterPathEntriesRpcHandler()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            // Assert
            var serviceDescriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(PathEntriesRpcHandler));
            serviceDescriptor.Should().BeNull("PathEntriesRpcHandler should not be registered when file transfer is disabled");
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_DoesNotRegisterIFileSystem()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            // Assert
            var serviceDescriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(IFileSystem));
            serviceDescriptor.Should().BeNull("IFileSystem should not be registered when file transfer is disabled");
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferEnabled_RegistersFileTransferServices()
        {
            // Arrange
            using var tempDir = new TempDirectoryScope();
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                // File transfer is enabled by default, use a real temp path
                opt.FileTransferOptions.StorageRootPath = tempDir.Path;
            });

            // Assert - all file transfer services should be registered
            services.Should().Contain(sd => sd.ServiceType == typeof(FileTransferEndpointService));
            services.Should().Contain(sd => sd.ServiceType == typeof(FileSystemRpcHandler));
            services.Should().Contain(sd => sd.ServiceType == typeof(PathEntriesRpcHandler));
            services.Should().Contain(sd => sd.ServiceType == typeof(IFileSystem));
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_StillRegistersFileTransferOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            // Assert - FileTransferOptions should still be registered (for other services to check)
            var serviceDescriptor = services.FirstOrDefault(sd =>
                sd.ServiceType == typeof(FileTransferOptions));
            serviceDescriptor.Should().NotBeNull("FileTransferOptions should be registered even when disabled");
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_OptionsShowDisabled()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var fileTransferOptions = serviceProvider.GetService<FileTransferOptions>();
            fileTransferOptions.IsEnabled.Should().BeFalse();
        }

        [TestMethod]
        public void AddCommandLineHub_NoConfiguration_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act - should not throw with no configuration
            var act = () => services.AddCommandLineHub();

            // Assert
            act.Should().NotThrow("AddCommandLineHub should work with default configuration");
        }

        [TestMethod]
        public void AddCommandLineHub_EmptyConfiguration_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act - should not throw with empty configuration action
            var act = () => services.AddCommandLineHub(opt => { });

            // Assert
            act.Should().NotThrow("AddCommandLineHub should work with empty configuration action");
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferEnabled_CreatesStorageRootDirectory()
        {
            // Arrange - use an isolated temp directory that doesn't exist yet
            using var tempDir = new TempDirectoryScope();
            tempDir.Exists.Should().BeFalse("precondition: directory should not exist");

            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.StorageRootPath = tempDir.Path;
            });

            // Assert - directory should be created during AddCommandLineHub
            tempDir.Exists.Should().BeTrue(
                "AddCommandLineHub should create the storage root directory when file transfer is enabled");
        }

        [TestMethod]
        public void AddCommandLineHub_StorageRootAlreadyExists_NoError()
        {
            // Arrange - create the directory ahead of time
            using var tempDir = new TempDirectoryScope(createDirectory: true);
            tempDir.Exists.Should().BeTrue("precondition: directory should exist");

            var services = new ServiceCollection();

            // Act - should not throw even though directory already exists
            var act = () => services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.StorageRootPath = tempDir.Path;
            });

            // Assert
            act.Should().NotThrow("AddCommandLineHub should be idempotent with existing storage root");
            tempDir.Exists.Should().BeTrue("directory should still exist");
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_DoesNotCreateStorageRootDirectory()
        {
            // Arrange - use an isolated temp directory that doesn't exist yet
            using var tempDir = new TempDirectoryScope();
            tempDir.Exists.Should().BeFalse("precondition: directory should not exist");

            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.StorageRootPath = tempDir.Path;
                opt.FileTransferOptions.Disable();
            });

            // Assert - directory should NOT be created when file transfer is disabled
            tempDir.Exists.Should().BeFalse(
                "AddCommandLineHub should not create the storage root directory when file transfer is disabled");
        }

        #region Non-Keyed IPathEntryProvider Tests

        #region Test Commands for Handler Resolution Tests

        /// <summary>
        /// Test command with [FilePathAutoComplete] attribute to test handler resolution.
        /// This is a test fixture - the Execute method intentionally does nothing.
        /// </summary>
        [Command(Name = "test-file")]
        private class TestFilePathCommand : CommandBase
        {
            [Argument(Name = "path")]
            [FilePathAutoComplete]
            public string Path { get; set; }

            public void Execute(CommandExecutionContext ctx) { /* Test fixture - no-op */ }
        }

        /// <summary>
        /// Test command with [DirectoryPathAutoComplete] attribute to test handler resolution.
        /// This is a test fixture - the Execute method intentionally does nothing.
        /// </summary>
        [Command(Name = "test-dir")]
        private class TestDirPathCommand : CommandBase
        {
            [Argument(Name = "path")]
            [DirectoryPathAutoComplete]
            public string Path { get; set; }

            public void Execute(CommandExecutionContext ctx) { /* Test fixture - no-op */ }
        }

        #endregion

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - Activates FilePathAutoCompleteHandler via AutoCompleteHandlerActivator
        ///   Breakage detection: [YES] - Fails if non-keyed IPathEntryProvider is not registered
        ///   Not a tautology: [YES] - Tests actual DI resolution behavior
        /// </summary>
        [TestMethod]
        public void FilePathAutoCompleteHandler_ServerContext_ResolvesFromDI()
        {
            // Arrange - configure server with file transfer enabled and a command that uses [FilePathAutoComplete]
            using var tempDir = new TempDirectoryScope(createDirectory: true);
            var services = new ServiceCollection();

            // AddCommandLineHub registers keyed IPathEntryProvider services
            // FilePathAutoCompleteHandler requires a non-keyed IPathEntryProvider
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.StorageRootPath = tempDir.Path;
                // Register a command with [FilePathAutoComplete] - this auto-registers the handler type
                opt.RegisterCommand<TestFilePathCommand>();
            });

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            // Create the handler activator (same as ServerLogic.AutoComplete does)
            var activator = new AutoCompleteHandlerActivator(scope.ServiceProvider);

            // Act - attempt to activate FilePathAutoCompleteHandler which depends on non-keyed IPathEntryProvider
            var act = () => activator.Activate<FilePathAutoCompleteHandler>();

            // Assert - should resolve successfully without DI exception
            act.Should().NotThrow(
                "FilePathAutoCompleteHandler should be activatable from the server DI container after AddCommandLineHub");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - Activates DirectoryPathAutoCompleteHandler via AutoCompleteHandlerActivator
        ///   Breakage detection: [YES] - Fails if non-keyed IPathEntryProvider is not registered
        ///   Not a tautology: [YES] - Tests actual DI resolution behavior
        /// </summary>
        [TestMethod]
        public void DirectoryPathAutoCompleteHandler_ServerContext_ResolvesFromDI()
        {
            // Arrange - configure server with file transfer enabled
            using var tempDir = new TempDirectoryScope(createDirectory: true);
            var services = new ServiceCollection();

            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.StorageRootPath = tempDir.Path;
                // Register a command with [DirectoryPathAutoComplete] - this auto-registers the handler type
                opt.RegisterCommand<TestDirPathCommand>();
            });

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            // Create the handler activator (same as ServerLogic.AutoComplete does)
            var activator = new AutoCompleteHandlerActivator(scope.ServiceProvider);

            // Act - attempt to activate DirectoryPathAutoCompleteHandler which depends on non-keyed IPathEntryProvider
            var act = () => activator.Activate<DirectoryPathAutoCompleteHandler>();

            // Assert - should resolve successfully without DI exception
            act.Should().NotThrow(
                "DirectoryPathAutoCompleteHandler should be activatable from the server DI container after AddCommandLineHub");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - Calls provider's EnumerateAsync using the sandboxed file system
        ///   Breakage detection: [YES] - Would fail if provider uses wrong file system
        ///   Not a tautology: [YES] - Tests actual behavior, not just registration
        /// </summary>
        [TestMethod]
        public async Task FilePathAutoCompleteHandler_ServerContext_UsesSandboxedFileSystem()
        {
            // Arrange - configure server with file transfer enabled and a specific storage root
            using var tempDir = new TempDirectoryScope(createDirectory: true);

            // Create a test file inside the sandboxed storage root
            var testFileName = "sandboxed-test.txt";
            File.WriteAllText(Path.Combine(tempDir.Path, testFileName), "test content");

            var services = new ServiceCollection();

            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.StorageRootPath = tempDir.Path;
            });

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            // Get the non-keyed IPathEntryProvider to verify it's using the sandboxed file system
            var provider = scope.ServiceProvider.GetRequiredService<IPathEntryProvider>();

            // Enumerate the sandbox root using "/" - PathValidator treats leading "/" as relative to sandbox root
            // The sandboxed IFileSystem.Directory.GetFiles/GetDirectories will validate and resolve the path.
            var entries = await provider.EnumerateAsync("/", includeFiles: true);

            // Assert - should see the test file we created
            entries.Should().Contain(e => e.Name == testFileName,
                "Non-keyed IPathEntryProvider should resolve to LocalPathEntryProvider backed by sandboxed IFileSystem");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - Resolves non-keyed IPathEntryProvider from DI
        ///   Breakage detection: [YES] - Verifies the provider type is correct
        ///   Not a tautology: [YES] - Tests actual provider instance type
        /// </summary>
        [TestMethod]
        public void AddCommandLineHub_FileTransferEnabled_RegistersNonKeyedIPathEntryProvider()
        {
            // Arrange
            using var tempDir = new TempDirectoryScope(createDirectory: true);
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.StorageRootPath = tempDir.Path;
            });

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            // Assert - non-keyed IPathEntryProvider should be registered and resolve to LocalPathEntryProvider
            var provider = scope.ServiceProvider.GetService<IPathEntryProvider>();
            provider.Should().NotBeNull(
                "Non-keyed IPathEntryProvider should be registered when file transfer is enabled");
            provider.Should().BeOfType<LocalPathEntryProvider>(
                "Non-keyed IPathEntryProvider should resolve to LocalPathEntryProvider");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - Resolves services from DI
        ///   Breakage detection: [YES] - Verifies the provider is NOT registered
        ///   Not a tautology: [YES] - Tests actual registration state
        /// </summary>
        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_DoesNotRegisterNonKeyedIPathEntryProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCommandLineHub(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            var serviceProvider = services.BuildServiceProvider();

            // Assert - non-keyed IPathEntryProvider should NOT be registered
            var provider = serviceProvider.GetService<IPathEntryProvider>();
            provider.Should().BeNull(
                "Non-keyed IPathEntryProvider should not be registered when file transfer is disabled");
        }

        #endregion
    }
}
