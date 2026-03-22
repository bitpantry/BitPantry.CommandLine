using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Description;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.AutoComplete.Handlers;

/// <summary>
/// Tests for DirectoryPathAutoCompleteHandler.
/// Verifies that only directories (no files) are returned.
/// Uses MockFileSystem for deterministic file system behavior.
/// </summary>
[TestClass]
public class DirectoryPathAutoCompleteHandlerTests
{
    private static readonly string WorkDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? @"C:\work"
        : "/work";
    private static readonly char Sep = Path.DirectorySeparatorChar;

    private static string P(string relativePath) =>
        $"{WorkDir}{Sep}{relativePath.Replace('\\', Sep)}";

    #region GetOptionsAsync Tests

    [TestMethod]
    public async Task GetOptionsAsync_EmptyQuery_ReturnsOnlyDirectories()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("file1.txt"), new MockFileData("") },
            { P("file2.cs"), new MockFileData("") },
            { P("docs\\readme.md"), new MockFileData("") },
            { P("src\\app.cs"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new DirectoryPathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — should include docs/ and src/ directories ONLY, no files
        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"docs{Sep}");
        options.Select(o => o.Value).Should().Contain($"src{Sep}");
    }

    [TestMethod]
    public async Task GetOptionsAsync_PartialName_FiltersDirectories()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("docs\\readme.md"), new MockFileData("") },
            { P("data\\info.csv"), new MockFileData("") },
            { P("src\\app.cs"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new DirectoryPathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "d");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — "d" matches "docs" and "data", not "src"
        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"data{Sep}");
        options.Select(o => o.Value).Should().Contain($"docs{Sep}");
    }

    [TestMethod]
    public async Task GetOptionsAsync_DirectoryPrefix_ReturnsSubdirs()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("docs\\api\\ref.md"), new MockFileData("") },
            { P("docs\\guide\\intro.md"), new MockFileData("") },
            { P("docs\\changelog.md"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new DirectoryPathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: $"docs{Sep}");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — subdirectories of docs only, not the file changelog.md
        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"docs{Sep}api{Sep}");
        options.Select(o => o.Value).Should().Contain($"docs{Sep}guide{Sep}");
    }

    [TestMethod]
    public async Task GetOptionsAsync_NonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("docs\\readme.md"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new DirectoryPathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: $"nonexistent{Sep}");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert
        options.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetOptionsAsync_DoesNotReturnFiles()
    {
        // Arrange — directory with only files, no subdirectories
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("file1.txt"), new MockFileData("") },
            { P("file2.cs"), new MockFileData("") },
            { P("readme.md"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new DirectoryPathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — no files should appear
        options.Should().BeEmpty("directory-only handler should not return any files");
    }

    [TestMethod]
    public async Task GetOptionsAsync_DirectoryEntries_HaveMenuStyle()
    {
        // Arrange
        var theme = new Theme();
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("docs\\readme.md"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new DirectoryPathAutoCompleteHandler(new LocalPathEntryProvider(fs), theme);
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — directory entries should have the MenuGroup style
        options.Should().HaveCount(1);
        options[0].MenuStyle.Should().Be(theme.MenuGroup);
    }

    [TestMethod]
    public async Task GetOptionsAsync_SortsAlphabetically()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("zebra\\z.txt"), new MockFileData("") },
            { P("alpha\\a.txt"), new MockFileData("") },
            { P("middle\\m.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new DirectoryPathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert
        options.Should().HaveCount(3);
        options[0].Value.Should().Be($"alpha{Sep}");
        options[1].Value.Should().Be($"middle{Sep}");
        options[2].Value.Should().Be($"zebra{Sep}");
    }

    [TestMethod]
    public async Task GetOptionsAsync_CaseInsensitiveMatching()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("Documents\\readme.md"), new MockFileData("") },
            { P("Downloads\\file.zip"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new DirectoryPathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "do");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — "do" should match "Documents" and "Downloads" case-insensitively
        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"Documents{Sep}");
        options.Select(o => o.Value).Should().Contain($"Downloads{Sep}");
    }

    #endregion

    #region Attribute Tests

    [TestMethod]
    public void Attribute_HandlerType_ReturnsDirectoryPathAutoCompleteHandler()
    {
        // Arrange
        var attr = new DirectoryPathAutoCompleteAttribute();

        // Act
        var handlerType = attr.HandlerType;

        // Assert
        handlerType.Should().Be(typeof(DirectoryPathAutoCompleteHandler));
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test command with a string property marked with [DirectoryPathAutoComplete].
    /// </summary>
    [Command]
    private class TestCommandWithDirPath : CommandBase
    {
        [Argument]
        [DirectoryPathAutoComplete]
        public string Directory { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Creates an AutoCompleteContext with the given query string.
    /// </summary>
    private static AutoCompleteContext CreateContext(string queryString = "")
    {
        var commandInfo = CommandReflection.Describe<TestCommandWithDirPath>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Directory");

        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = $"test --Directory {queryString}",
            CursorPosition = $"test --Directory {queryString}".Length,
            ArgumentInfo = argumentInfo,
            ProvidedValues = new Dictionary<BitPantry.CommandLine.Component.ArgumentInfo, string>(),
            CommandInfo = commandInfo,
        };
    }

    #endregion
}
