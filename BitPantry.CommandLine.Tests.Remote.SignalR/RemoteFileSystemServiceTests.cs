using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Remote.SignalR;

/// <summary>
/// Tests for RemoteFileSystemService file listing functionality.
/// </summary>
[TestClass]
public class RemoteFileSystemServiceTests
{
    [TestMethod]
    public async Task ListFilesAsync_WithEmptyPath_ReturnsRootFiles()
    {
        // Arrange
        var storageRoot = Path.GetFullPath($"./test-file-system-storage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(storageRoot);

        using var testEnv = new TestEnvironment(opts =>
        {
            opts.StorageRootPath = storageRoot;
        });
        
        // Create test files in the server's storage root
        File.WriteAllText(Path.Combine(storageRoot, "test1.txt"), "content1");
        File.WriteAllText(Path.Combine(storageRoot, "test2.txt"), "content2");

        try
        {
            // Connect
            await testEnv.Cli.ConnectToServer(testEnv.Server);

            // Get service
            var fileSystem = testEnv.Cli.Services.GetRequiredService<RemoteFileSystemService>();

            // Act
            var files = await fileSystem.ListFilesAsync("");

            // Debug output
            Console.WriteLine($"Storage root: {storageRoot}");
            Console.WriteLine($"Files exist: test1={File.Exists(Path.Combine(storageRoot, "test1.txt"))}, test2={File.Exists(Path.Combine(storageRoot, "test2.txt"))}");
            Console.WriteLine($"Returned {files.Count} files: {string.Join(", ", files)}");

            // Assert
            files.Should().NotBeNull();
            files.Should().Contain("test1.txt");
            files.Should().Contain("test2.txt");
        }
        finally
        {
            // Clean up
            if (Directory.Exists(storageRoot))
                Directory.Delete(storageRoot, true);
        }
    }
}
