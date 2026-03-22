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
/// Tests for FilePathAutoCompleteHandler.
/// Uses MockFileSystem for deterministic file system behavior.
/// </summary>
[TestClass]
public class FilePathAutoCompleteHandlerTests
{
    private static readonly string WorkDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? @"C:\work"
        : "/work";
    private static readonly char Sep = Path.DirectorySeparatorChar;

    private static string P(string relativePath) =>
        $"{WorkDir}{Sep}{relativePath.Replace('\\', Sep)}";

    #region GetOptionsAsync Tests

    [TestMethod]
    public async Task GetOptionsAsync_EmptyQuery_ReturnsAllEntriesInCurrentDir()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("file1.txt"), new MockFileData("") },
            { P("file2.cs"), new MockFileData("") },
            { P("docs\\readme.md"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — should include docs/ directory, file1.txt, file2.cs
        options.Should().HaveCount(3);
        options.Select(o => o.Value).Should().Contain($"docs{Sep}");
        options.Select(o => o.Value).Should().Contain("file1.txt");
        options.Select(o => o.Value).Should().Contain("file2.cs");
    }

    [TestMethod]
    public async Task GetOptionsAsync_PartialFilename_ReturnsMatchingEntries()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("file1.txt"), new MockFileData("") },
            { P("file2.cs"), new MockFileData("") },
            { P("other.log"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "file");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — only entries starting with "file"
        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().AllSatisfy(v => v.Should().StartWith("file"));
    }

    [TestMethod]
    public async Task GetOptionsAsync_DirectoryPrefix_ReturnsEntriesInSubdir()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("docs\\readme.md"), new MockFileData("") },
            { P("docs\\guide.txt"), new MockFileData("") },
            { P("file1.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: $"docs{Sep}");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — entries in docs/ subdirectory, prefixed with "docs\"
        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"docs{Sep}guide.txt");
        options.Select(o => o.Value).Should().Contain($"docs{Sep}readme.md");
    }

    [TestMethod]
    public async Task GetOptionsAsync_DirectoryPrefixWithFragment_FiltersWithinSubdir()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("docs\\readme.md"), new MockFileData("") },
            { P("docs\\guide.txt"), new MockFileData("") },
            { P("docs\\api.md"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: $"docs{Sep}re");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — only readme.md matches "re" fragment
        options.Should().HaveCount(1);
        options.First().Value.Should().Be($"docs{Sep}readme.md");
    }

    [TestMethod]
    public async Task GetOptionsAsync_NonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("file1.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: $"nonexistent{Sep}");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert
        options.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetOptionsAsync_DirectoryEntries_AppendSeparator()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("mydir\\child.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — directory entries should end with path separator
        var dirOption = options.FirstOrDefault(o => o.Value.StartsWith("mydir"));
        dirOption.Should().NotBeNull();
        dirOption!.Value.Should().EndWith(Sep.ToString());
    }

    [TestMethod]
    public async Task GetOptionsAsync_SortsDirectoriesBeforeFiles()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("alpha.txt"), new MockFileData("") },
            { P("beta\\child.txt"), new MockFileData("") },
            { P("gamma.log"), new MockFileData("") },
            { P("delta\\child.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — directories (beta\, delta\) should come before files (alpha.txt, gamma.log)
        options.Should().HaveCount(4);
        var values = options.Select(o => o.Value).ToList();
        var firstDirIndex = values.FindIndex(v => v.EndsWith(Sep.ToString()));
        var lastDirIndex = values.FindLastIndex(v => v.EndsWith(Sep.ToString()));
        var firstFileIndex = values.FindIndex(v => !v.EndsWith(Sep.ToString()));

        // All directories should come before all files
        lastDirIndex.Should().BeLessThan(firstFileIndex);
    }

    [TestMethod]
    public async Task GetOptionsAsync_CaseInsensitiveMatching()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("ReadMe.md"), new MockFileData("") },
            { P("readme.txt"), new MockFileData("") },
            { P("other.log"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "READ");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — both ReadMe.md and readme.txt should match "READ" case-insensitively
        options.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetOptionsAsync_TrailingSlashQuery_ListsDirectoryContents()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("src\\main.cs"), new MockFileData("") },
            { P("src\\util.cs"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: $"src{Sep}");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert
        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"src{Sep}main.cs");
        options.Select(o => o.Value).Should().Contain($"src{Sep}util.cs");
    }

    [TestMethod]
    public async Task GetOptionsAsync_DirectoryEntries_HaveMenuStyle()
    {
        // Arrange
        var theme = new Theme();
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("mydir\\child.txt"), new MockFileData("") },
            { P("file.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), theme);
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — directory option should have MenuStyle from theme, file option should not
        var dirOption = options.First(o => o.Value.Contains("mydir"));
        var fileOption = options.First(o => o.Value == "file.txt");

        dirOption.MenuStyle.Should().Be(theme.MenuGroup);
        fileOption.MenuStyle.Should().BeNull();
    }

    [TestMethod]
    public async Task GetOptionsAsync_FileEntries_HaveNoMenuStyle()
    {
        // Arrange
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { P("file.txt"), new MockFileData("") },
        }, currentDirectory: WorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert
        var fileOption = options.First(o => o.Value == "file.txt");
        fileOption.MenuStyle.Should().BeNull();
    }

    [TestMethod]
    public async Task GetOptionsAsync_RelativeDotDot_ReturnsParentEntries()
    {
        // Arrange — use a deeper working dir so ../ doesn't hit filesystem root on Linux
        var deepWorkDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\testroot\work" : "/testroot/work";
        var parentDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\testroot\" : "/testroot/";
        var siblingPath = $"{parentDir}sibling{Sep}other.txt";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{deepWorkDir}{Sep}file.txt", new MockFileData("") },
            { siblingPath, new MockFileData("") },
        }, currentDirectory: deepWorkDir);

        var handler = new FilePathAutoCompleteHandler(new LocalPathEntryProvider(fs), new Theme());
        var context = CreateContext(queryString: $"..{Sep}");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert — should list entries from parent of deepWorkDir
        // At minimum, "work" and "sibling" directories should be present
        options.Should().NotBeEmpty();
        options.Select(o => o.Value).Should().Contain(v => v.Contains("work"));
        options.Select(o => o.Value).Should().Contain(v => v.Contains("sibling"));
    }

    #endregion

    #region SplitQueryIntoDirectoryAndFragment Tests

    [TestMethod]
    public void SplitQuery_EmptyString_ReturnsBothEmpty()
    {
        var (dir, frag) = PathQueryHelper.SplitQueryIntoDirectoryAndFragment("");
        dir.Should().BeEmpty();
        frag.Should().BeEmpty();
    }

    [TestMethod]
    public void SplitQuery_NullString_ReturnsBothEmpty()
    {
        var (dir, frag) = PathQueryHelper.SplitQueryIntoDirectoryAndFragment(null);
        dir.Should().BeEmpty();
        frag.Should().BeEmpty();
    }

    [TestMethod]
    public void SplitQuery_FilenameOnly_ReturnsEmptyDirAndFilename()
    {
        var (dir, frag) = PathQueryHelper.SplitQueryIntoDirectoryAndFragment("file.txt");
        dir.Should().BeEmpty();
        frag.Should().Be("file.txt");
    }

    [TestMethod]
    public void SplitQuery_DirectoryWithTrailingSlash_ReturnsDirAndEmptyFragment()
    {
        var (dir, frag) = PathQueryHelper.SplitQueryIntoDirectoryAndFragment($"docs{Sep}");
        dir.Should().Be($"docs{Sep}");
        frag.Should().BeEmpty();
    }

    [TestMethod]
    public void SplitQuery_DirectoryPlusFragment_SplitsCorrectly()
    {
        var (dir, frag) = PathQueryHelper.SplitQueryIntoDirectoryAndFragment($"docs{Sep}rea");
        dir.Should().Be($"docs{Sep}");
        frag.Should().Be("rea");
    }

    [TestMethod]
    public void SplitQuery_NestedPath_SplitsAtLastSeparator()
    {
        var (dir, frag) = PathQueryHelper.SplitQueryIntoDirectoryAndFragment($"a{Sep}b{Sep}c");
        dir.Should().Be($"a{Sep}b{Sep}");
        frag.Should().Be("c");
    }

    #endregion

    #region Attribute Tests

    [TestMethod]
    public void Attribute_HandlerType_ReturnsFilePathAutoCompleteHandler()
    {
        // Arrange
        var attr = new FilePathAutoCompleteAttribute();

        // Act — HandlerType is inherited from AutoCompleteAttribute<T>
        var handlerType = attr.HandlerType;

        // Assert
        handlerType.Should().Be(typeof(FilePathAutoCompleteHandler));
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test command with a string property marked with [FilePathAutoComplete].
    /// </summary>
    [Command]
    private class TestCommandWithFilePath : CommandBase
    {
        [Argument]
        [FilePathAutoComplete]
        public string FilePath { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Creates an AutoCompleteContext with the given query string.
    /// Other properties are populated with valid defaults since the handler only uses QueryString.
    /// </summary>
    private static AutoCompleteContext CreateContext(string queryString = "")
    {
        var commandInfo = CommandReflection.Describe<TestCommandWithFilePath>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "FilePath");

        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = $"test --FilePath {queryString}",
            CursorPosition = $"test --FilePath {queryString}".Length,
            ArgumentInfo = argumentInfo,
            ProvidedValues = new Dictionary<BitPantry.CommandLine.Component.ArgumentInfo, string>(),
            CommandInfo = commandInfo,
        };
    }

    #endregion
}
