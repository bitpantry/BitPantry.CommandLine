using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

[TestClass]
public class LocalPathEntryProviderTests
{
    private static readonly string WorkDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? @"C:\work"
        : "/work";

    private static string P(string relativePath) =>
        $"{WorkDir}{Path.DirectorySeparatorChar}{relativePath.Replace('\\', Path.DirectorySeparatorChar)}";

    [TestMethod]
    public async Task EnumerateAsync_IncludeFilesTrue_ReturnsDirectoriesAndFiles()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("file1.txt"), new MockFileData("") },
            { P("docs\\readme.md"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var provider = new LocalPathEntryProvider(fs);

        var entries = await provider.EnumerateAsync(WorkDir, includeFiles: true);

        entries.Should().HaveCount(2);
        entries.Should().Contain(e => e.Name == "docs" && e.IsDirectory);
        entries.Should().Contain(e => e.Name == "file1.txt" && !e.IsDirectory);
    }

    [TestMethod]
    public async Task EnumerateAsync_IncludeFilesFalse_ReturnsOnlyDirectories()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("file1.txt"), new MockFileData("") },
            { P("docs\\readme.md"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var provider = new LocalPathEntryProvider(fs);

        var entries = await provider.EnumerateAsync(WorkDir, includeFiles: false);

        entries.Should().HaveCount(1);
        entries.Should().OnlyContain(e => e.IsDirectory);
        entries.First().Name.Should().Be("docs");
    }

    [TestMethod]
    public async Task EnumerateAsync_EmptyDirectory_ReturnsEmpty()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(), currentDirectory: WorkDir);
        fs.Directory.CreateDirectory(WorkDir);

        var provider = new LocalPathEntryProvider(fs);

        var entries = await provider.EnumerateAsync(WorkDir, includeFiles: true);

        entries.Should().BeEmpty();
    }

    [TestMethod]
    public async Task EnumerateAsync_NonExistentDirectory_ReturnsEmpty()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(), currentDirectory: WorkDir);
        fs.Directory.CreateDirectory(WorkDir);

        var provider = new LocalPathEntryProvider(fs);

        var nonexistent = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\nonexistent"
            : "/nonexistent";
        var entries = await provider.EnumerateAsync(nonexistent, includeFiles: true);

        entries.Should().BeEmpty();
    }

    [TestMethod]
    public async Task EnumerateAsync_MarksDirectoriesCorrectly()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("subdir\\inner.txt"), new MockFileData("") },
            { P("file.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var provider = new LocalPathEntryProvider(fs);

        var entries = await provider.EnumerateAsync(WorkDir, includeFiles: true);

        var dirEntry = entries.First(e => e.Name == "subdir");
        dirEntry.IsDirectory.Should().BeTrue();

        var fileEntry = entries.First(e => e.Name == "file.txt");
        fileEntry.IsDirectory.Should().BeFalse();
    }

    [TestMethod]
    public void GetCurrentDirectory_ReturnsFileSystemCwd()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(), currentDirectory: WorkDir);
        fs.Directory.CreateDirectory(WorkDir);

        var provider = new LocalPathEntryProvider(fs);

        provider.GetCurrentDirectory().Should().Be(WorkDir);
    }

    [TestMethod]
    public async Task EnumerateAsync_SortsAlphabetically()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("zebra\\inner.txt"), new MockFileData("") },
            { P("alpha\\inner.txt"), new MockFileData("") },
            { P("middle\\inner.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var provider = new LocalPathEntryProvider(fs);

        var entries = await provider.EnumerateAsync(WorkDir, includeFiles: false);

        entries.Select(e => e.Name).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }
}
