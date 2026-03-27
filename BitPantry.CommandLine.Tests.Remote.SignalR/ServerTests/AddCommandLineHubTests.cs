using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using BitPantry.CommandLine.Remote.SignalR.Server.Rpc;
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
            var tempStorageRoot = Path.Combine(Path.GetTempPath(), $"cli-storage-test-{Guid.NewGuid():N}");
            var services = new ServiceCollection();

            try
            {
                // Act
                services.AddCommandLineHub(opt =>
                {
                    // File transfer is enabled by default, use a real temp path
                    opt.FileTransferOptions.StorageRootPath = tempStorageRoot;
                });

                // Assert - all file transfer services should be registered
                services.Should().Contain(sd => sd.ServiceType == typeof(FileTransferEndpointService));
                services.Should().Contain(sd => sd.ServiceType == typeof(FileSystemRpcHandler));
                services.Should().Contain(sd => sd.ServiceType == typeof(PathEntriesRpcHandler));
                services.Should().Contain(sd => sd.ServiceType == typeof(IFileSystem));
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempStorageRoot))
                    Directory.Delete(tempStorageRoot, recursive: true);
            }
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
            var tempStorageRoot = Path.Combine(Path.GetTempPath(), $"cli-storage-test-{Guid.NewGuid():N}");
            try
            {
                Directory.Exists(tempStorageRoot).Should().BeFalse("precondition: directory should not exist");

                var services = new ServiceCollection();

                // Act
                services.AddCommandLineHub(opt =>
                {
                    opt.FileTransferOptions.StorageRootPath = tempStorageRoot;
                });

                // Assert - directory should be created during AddCommandLineHub
                Directory.Exists(tempStorageRoot).Should().BeTrue(
                    "AddCommandLineHub should create the storage root directory when file transfer is enabled");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempStorageRoot))
                    Directory.Delete(tempStorageRoot, recursive: true);
            }
        }

        [TestMethod]
        public void AddCommandLineHub_StorageRootAlreadyExists_NoError()
        {
            // Arrange - create the directory ahead of time
            var tempStorageRoot = Path.Combine(Path.GetTempPath(), $"cli-storage-test-{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(tempStorageRoot);
                Directory.Exists(tempStorageRoot).Should().BeTrue("precondition: directory should exist");

                var services = new ServiceCollection();

                // Act - should not throw even though directory already exists
                var act = () => services.AddCommandLineHub(opt =>
                {
                    opt.FileTransferOptions.StorageRootPath = tempStorageRoot;
                });

                // Assert
                act.Should().NotThrow("AddCommandLineHub should be idempotent with existing storage root");
                Directory.Exists(tempStorageRoot).Should().BeTrue("directory should still exist");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempStorageRoot))
                    Directory.Delete(tempStorageRoot, recursive: true);
            }
        }

        [TestMethod]
        public void AddCommandLineHub_FileTransferDisabled_DoesNotCreateStorageRootDirectory()
        {
            // Arrange - use an isolated temp directory that doesn't exist yet
            var tempStorageRoot = Path.Combine(Path.GetTempPath(), $"cli-storage-test-{Guid.NewGuid():N}");
            try
            {
                Directory.Exists(tempStorageRoot).Should().BeFalse("precondition: directory should not exist");

                var services = new ServiceCollection();

                // Act
                services.AddCommandLineHub(opt =>
                {
                    opt.FileTransferOptions.StorageRootPath = tempStorageRoot;
                    opt.FileTransferOptions.Disable();
                });

                // Assert - directory should NOT be created when file transfer is disabled
                Directory.Exists(tempStorageRoot).Should().BeFalse(
                    "AddCommandLineHub should not create the storage root directory when file transfer is disabled");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempStorageRoot))
                    Directory.Delete(tempStorageRoot, recursive: true);
            }
        }
    }
}
