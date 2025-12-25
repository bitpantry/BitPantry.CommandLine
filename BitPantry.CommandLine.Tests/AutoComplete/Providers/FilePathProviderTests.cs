using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for FilePathProvider - completing file paths.
/// Covers test cases FP-001 to FP-007 from specs.
/// </summary>
[TestClass]
public class FilePathProviderTests
{
    private FilePathProvider _provider;
    private MockFileSystem _mockFileSystem;

    [TestInitialize]
    public void Setup()
    {
        _mockFileSystem = new MockFileSystem();
        _provider = new FilePathProvider(_mockFileSystem);
    }

    #region FP-001: Complete files in current directory

    [TestMethod]
    public async Task FP001_GetCompletions_CurrentDirectory_ReturnsFiles()
    {
        // Arrange
        _mockFileSystem.AddFile(@"C:\work\file1.txt", new MockFileData("content"));
        _mockFileSystem.AddFile(@"C:\work\file2.txt", new MockFileData("content"));
        _mockFileSystem.AddDirectory(@"C:\work");
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext("", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().Contain(item => item.InsertText.Contains("file1.txt"));
        result.Items.Should().Contain(item => item.InsertText.Contains("file2.txt"));
    }

    #endregion

    #region FP-002: Filter by partial name

    [TestMethod]
    public async Task FP002_GetCompletions_WithPartialName_FiltersResults()
    {
        // Arrange
        _mockFileSystem.AddFile(@"C:\work\readme.md", new MockFileData("content"));
        _mockFileSystem.AddFile(@"C:\work\readme.txt", new MockFileData("content"));
        _mockFileSystem.AddFile(@"C:\work\other.txt", new MockFileData("content"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext("read", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(item => 
            item.InsertText.ToLower().Should().StartWith("read"));
    }

    #endregion

    #region FP-003: Include directories in completion

    [TestMethod]
    public async Task FP003_GetCompletions_IncludesDirectories()
    {
        // Arrange
        _mockFileSystem.AddDirectory(@"C:\work\subdir");
        _mockFileSystem.AddFile(@"C:\work\file.txt", new MockFileData("content"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext("", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().Contain(item => item.InsertText.Contains("subdir"));
        result.Items.Should().Contain(item => item.InsertText.Contains("file.txt"));
    }

    #endregion

    #region FP-004: Complete with path prefix

    [TestMethod]
    public async Task FP004_GetCompletions_WithPathPrefix_CompletesInSubdirectory()
    {
        // Arrange
        _mockFileSystem.AddDirectory(@"C:\work\subdir");
        _mockFileSystem.AddFile(@"C:\work\subdir\nested.txt", new MockFileData("content"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext(@"subdir\", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().Contain(item => item.InsertText.Contains("nested.txt"));
    }

    #endregion

    #region FP-005: Handle non-existent path gracefully

    [TestMethod]
    public async Task FP005_GetCompletions_NonExistentPath_ReturnsEmpty()
    {
        // Arrange
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext(@"nonexistent\", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region FP-006: Case-insensitive matching

    [TestMethod]
    public async Task FP006_GetCompletions_CaseInsensitive_Matches()
    {
        // Arrange
        _mockFileSystem.AddFile(@"C:\work\README.md", new MockFileData("content"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext("read", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].InsertText.Should().Contain("README.md");
    }

    #endregion

    #region FP-007: Quote paths with spaces

    [TestMethod]
    public async Task FP007_GetCompletions_PathsWithSpaces_AreQuoted()
    {
        // Arrange
        _mockFileSystem.AddFile(@"C:\work\my file.txt", new MockFileData("content"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext("", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        var fileItem = result.Items.FirstOrDefault(item => item.InsertText.Contains("my"));
        fileItem.Should().NotBeNull();
        fileItem.InsertText.Should().StartWith("\"").And.EndWith("\"");
    }

    #endregion

    #region Provider Configuration

    [TestMethod]
    public void Priority_ShouldBePositive()
    {
        _provider.Priority.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public void CanHandle_WithArgumentValue_AndFileAttribute_ReturnsTrue()
    {
        var context = CreateContext("", CompletionElementType.ArgumentValue);
        // The provider should check for file path attribute/property type
        _provider.CanHandle(context).Should().BeTrue();
    }

    #endregion

    private CompletionContext CreateContext(
        string prefix,
        CompletionElementType elementType)
    {
        return new CompletionContext
        {
            FullInput = $"mycommand --path {prefix}",
            CursorPosition = $"mycommand --path {prefix}".Length,
            ElementType = elementType,
            CurrentWord = prefix,
            CommandName = "mycommand",
            ArgumentName = "path"
        };
    }
}
