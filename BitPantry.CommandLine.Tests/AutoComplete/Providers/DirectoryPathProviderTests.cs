using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for DirectoryPathProvider - completing directory paths.
/// Covers test cases DP-001 to DP-002 from specs.
/// </summary>
[TestClass]
public class DirectoryPathProviderTests
{
    private DirectoryPathProvider _provider;
    private MockFileSystem _mockFileSystem;

    [TestInitialize]
    public void Setup()
    {
        _mockFileSystem = new MockFileSystem();
        _provider = new DirectoryPathProvider(_mockFileSystem);
    }

    #region DP-001: Complete directories only

    [TestMethod]
    public async Task DP001_GetCompletions_ReturnsDirectoriesOnly()
    {
        // Arrange
        _mockFileSystem.AddDirectory(@"C:\work\subdir1");
        _mockFileSystem.AddDirectory(@"C:\work\subdir2");
        _mockFileSystem.AddFile(@"C:\work\file.txt", new MockFileData("content"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext("", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(item => item.InsertText.Contains("subdir1"));
        result.Items.Should().Contain(item => item.InsertText.Contains("subdir2"));
        result.Items.Should().NotContain(item => item.InsertText.Contains("file.txt"));
    }

    #endregion

    #region DP-002: Filter by partial name

    [TestMethod]
    public async Task DP002_GetCompletions_WithPartialName_FiltersResults()
    {
        // Arrange
        _mockFileSystem.AddDirectory(@"C:\work\documents");
        _mockFileSystem.AddDirectory(@"C:\work\downloads");
        _mockFileSystem.AddDirectory(@"C:\work\other");
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = CreateContext("do", CompletionElementType.ArgumentValue);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(item =>
            item.InsertText.ToLower().Should().Contain("do"));
    }

    #endregion

    #region Provider Configuration

    [TestMethod]
    public void Priority_ShouldBePositive()
    {
        _provider.Priority.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public void CanHandle_WithArgumentValue_AndDirectoryAttribute_ReturnsTrue()
    {
        var context = CreateContext("", CompletionElementType.ArgumentValue);
        // The provider should check for directory path attribute/property type
        _provider.CanHandle(context).Should().BeTrue();
    }

    #endregion

    private CompletionContext CreateContext(
        string prefix,
        CompletionElementType elementType)
    {
        return new CompletionContext
        {
            FullInput = $"mycommand --dir {prefix}",
            CursorPosition = $"mycommand --dir {prefix}".Length,
            ElementType = elementType,
            CurrentWord = prefix,
            CommandName = "mycommand",
            ArgumentName = "dir"
        };
    }
}
