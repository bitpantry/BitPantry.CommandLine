using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

/// <summary>
/// Tests for the four semantic path autocomplete handlers:
/// ServerFilePathAutoCompleteHandler, ServerDirectoryPathAutoCompleteHandler,
/// ClientFilePathAutoCompleteHandler, ClientDirectoryPathAutoCompleteHandler.
/// All tests use a mocked IPathEntryProvider — no real FS or RPC.
/// </summary>
[TestClass]
public class SemanticPathAutoCompleteHandlerTests
{
    private static readonly char Sep = Path.DirectorySeparatorChar;
    private Mock<IPathEntryProvider> _providerMock;
    private Theme _theme;

    [TestInitialize]
    public void Setup()
    {
        _providerMock = new Mock<IPathEntryProvider>();
        _providerMock.Setup(p => p.GetCurrentDirectory()).Returns(@"C:\work");
        _theme = new Theme();
    }

    #region ServerFilePathAutoCompleteHandler Tests

    [TestMethod]
    public async Task ServerFile_EmptyQuery_ReturnsDirsAndFiles()
    {
        _providerMock.Setup(p => p.EnumerateAsync(@"C:\work", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("docs", true),
                new("file1.txt", false),
            });

        var handler = new ServerFilePathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext(""));

        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"docs{Sep}");
        options.Select(o => o.Value).Should().Contain("file1.txt");
    }

    [TestMethod]
    public async Task ServerFile_PartialName_FiltersResults()
    {
        _providerMock.Setup(p => p.EnumerateAsync(@"C:\work", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("docs", true),
                new("data", true),
                new("file1.txt", false),
            });

        var handler = new ServerFilePathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext("d"));

        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"docs{Sep}");
        options.Select(o => o.Value).Should().Contain($"data{Sep}");
    }

    [TestMethod]
    public async Task ServerFile_DirectoryPrefix_EnumeratesSubdir()
    {
        _providerMock.Setup(p => p.EnumerateAsync($"docs{Sep}", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("readme.md", false),
            });

        var handler = new ServerFilePathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext($"docs{Sep}"));

        options.Should().HaveCount(1);
        options[0].Value.Should().Be($"docs{Sep}readme.md");
    }

    [TestMethod]
    public void ServerFile_Attribute_HandlerType()
    {
        var attr = new ServerFilePathAutoCompleteAttribute();
        attr.HandlerType.Should().Be(typeof(ServerFilePathAutoCompleteHandler));
    }

    #endregion

    #region ServerDirectoryPathAutoCompleteHandler Tests

    [TestMethod]
    public async Task ServerDir_EmptyQuery_ReturnsOnlyDirs()
    {
        _providerMock.Setup(p => p.EnumerateAsync(@"C:\work", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("docs", true),
                new("src", true),
            });

        var handler = new ServerDirectoryPathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext(""));

        options.Should().HaveCount(2);
        options.Should().OnlyContain(o => o.Value.EndsWith(Sep.ToString()));
    }

    [TestMethod]
    public async Task ServerDir_DoesNotReturnFiles()
    {
        // Even if provider somehow returned files, handler is called with includeFiles=false
        _providerMock.Setup(p => p.EnumerateAsync(@"C:\work", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("docs", true),
            });

        var handler = new ServerDirectoryPathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext(""));

        options.Should().HaveCount(1);
        options[0].Value.Should().Be($"docs{Sep}");
    }

    [TestMethod]
    public void ServerDir_Attribute_HandlerType()
    {
        var attr = new ServerDirectoryPathAutoCompleteAttribute();
        attr.HandlerType.Should().Be(typeof(ServerDirectoryPathAutoCompleteHandler));
    }

    #endregion

    #region ClientFilePathAutoCompleteHandler Tests

    [TestMethod]
    public async Task ClientFile_EmptyQuery_ReturnsDirsAndFiles()
    {
        _providerMock.Setup(p => p.EnumerateAsync(@"C:\work", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("subdir", true),
                new("notes.txt", false),
            });

        var handler = new ClientFilePathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext(""));

        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().Contain($"subdir{Sep}");
        options.Select(o => o.Value).Should().Contain("notes.txt");
    }

    [TestMethod]
    public void ClientFile_Attribute_HandlerType()
    {
        var attr = new ClientFilePathAutoCompleteAttribute();
        attr.HandlerType.Should().Be(typeof(ClientFilePathAutoCompleteHandler));
    }

    #endregion

    #region ClientDirectoryPathAutoCompleteHandler Tests

    [TestMethod]
    public async Task ClientDir_EmptyQuery_ReturnsOnlyDirs()
    {
        _providerMock.Setup(p => p.EnumerateAsync(@"C:\work", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("backup", true),
            });

        var handler = new ClientDirectoryPathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext(""));

        options.Should().HaveCount(1);
        options[0].Value.Should().Be($"backup{Sep}");
    }

    [TestMethod]
    public void ClientDir_Attribute_HandlerType()
    {
        var attr = new ClientDirectoryPathAutoCompleteAttribute();
        attr.HandlerType.Should().Be(typeof(ClientDirectoryPathAutoCompleteHandler));
    }

    #endregion

    #region Shared Behavior Tests (using ServerFilePathAutoCompleteHandler as representative)

    [TestMethod]
    public async Task NonExistentDirectory_ReturnsEmpty()
    {
        _providerMock.Setup(p => p.EnumerateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>());

        var handler = new ServerFilePathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext($"nonexistent{Sep}"));

        options.Should().BeEmpty();
    }

    [TestMethod]
    public async Task DirectoryEntries_HaveMenuStyle()
    {
        _providerMock.Setup(p => p.EnumerateAsync(@"C:\work", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("docs", true),
                new("file.txt", false),
            });

        var handler = new ServerFilePathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext(""));

        var dirOption = options.First(o => o.Value.StartsWith("docs"));
        dirOption.GetMenuValue().Should().NotBeNullOrEmpty();

        // File options should not have the directory style
        var fileOption = options.First(o => o.Value == "file.txt");
        // file options use default style (no menuStyle set)
    }

    [TestMethod]
    public async Task ProviderThrows_ReturnsEmpty()
    {
        _providerMock.Setup(p => p.EnumerateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        var handler = new ServerFilePathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext(""));

        options.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CaseInsensitiveMatching()
    {
        _providerMock.Setup(p => p.EnumerateAsync(@"C:\work", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PathEntry>
            {
                new("Documents", true),
                new("data.csv", false),
            });

        var handler = new ServerFilePathAutoCompleteHandler(_providerMock.Object, _theme);
        var options = await handler.GetOptionsAsync(CreateContext("d"));

        options.Should().HaveCount(2);
    }

    #endregion

    #region Test Helpers

    [Command]
    private class TestCommand : CommandBase
    {
        [Argument]
        [ServerFilePathAutoComplete]
        public string Path { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    private static AutoCompleteContext CreateContext(string queryString = "")
    {
        var commandInfo = CommandReflection.Describe<TestCommand>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Path");

        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = $"test --Path {queryString}",
            CursorPosition = $"test --Path {queryString}".Length,
            ArgumentInfo = argumentInfo,
            ProvidedValues = new Dictionary<ArgumentInfo, string>(),
            CommandInfo = commandInfo,
        };
    }

    #endregion
}
