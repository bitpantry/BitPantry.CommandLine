using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.AutoComplete;

/// <summary>
/// Integration tests for autocomplete on file download command.
/// These tests validate that positional arguments with [RemoteFilePathCompletion]
/// show autocomplete menu when connected to a server with files.
/// </summary>
[TestClass]
public class FileDownloadAutoCompleteTests
{
    /// <summary>
    /// BUG REPRODUCTION: file download command with positional [RemoteFilePathCompletion]
    /// shows named arguments instead of remote files when pressing Tab after "file download ".
    /// 
    /// Expected behavior: Should show remote files from the server (document1.txt, document2.txt, etc.)
    /// Actual behavior: Shows named arguments (--Source, --Destination, --Force)
    /// 
    /// Root cause: RemoteFilePathCompletionProvider is not being invoked for positional arguments
    /// </summary>
    [TestMethod]
    public async Task FileDownload_PositionalRemoteFile_ShowsNamedArgumentsInsteadOfRemoteFiles()
    {
        // Arrange: Create test environment with server and unique storage
        var storageRoot = Path.GetFullPath($"./test-autocomplete-storage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(storageRoot);

        using var testEnv = new TestEnvironment(opts =>
        {
            opts.StorageRootPath = storageRoot;
        });
        
        // Create test files in the server's storage root (not a subdirectory)
        var testFiles = new[]
        {
            "document1.txt",
            "document2.txt",
            "report.pdf",
            "data.json"
        };
        
        foreach (var file in testFiles)
        {
            File.WriteAllText(Path.Combine(storageRoot, file), $"Test content for {file}");
        }

        try
        {
            // Connect to server
            await testEnv.Cli.ConnectToServer(testEnv.Server);
            
            // Verify connection
            var proxy = testEnv.Cli.Services.GetRequiredService<IServerProxy>();
            proxy.ConnectionState.Should().Be(ServerProxyConnectionState.Connected, 
                "client must be connected for autocomplete to work");

            // Create harness with the connected CLI
            using var harness = new AutoCompleteTestHarness(testEnv.Cli);

            // Act - Type "file download " and press Tab
            harness.Keyboard.TypeText("file download ");
            await harness.Keyboard.PressKeyAsync(ConsoleKey.Tab);

            // Verify connection is still active
            proxy.ConnectionState.Should().Be(ServerProxyConnectionState.Connected, 
                "connection should still be active during autocomplete");

            // Verify autocomplete menu appeared
            harness.IsMenuVisible.Should().BeTrue("autocomplete menu should appear");
            harness.MenuItems.Should().NotBeNull();
            
            // Verify remote files appear (not named arguments)
            harness.MenuItems.Any(item => item.DisplayText.Contains("--Source") || 
                                          item.DisplayText.Contains("--Destination")).Should().BeFalse(
                "should show remote files for positional arg, not named arguments");
            
            // Check that test files appear in the autocomplete menu
            foreach (var file in testFiles)
            {
                harness.MenuItems.Any(item => item.DisplayText.Contains(file)).Should().BeTrue(
                    $"expected to find remote file '{file}' in autocomplete menu");
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(storageRoot))
                Directory.Delete(storageRoot, true);
        }
    }
}
