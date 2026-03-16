using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

/// <summary>
/// UX integration tests verifying syntax highlighting of remote command arguments.
/// Uses TestEnvironment with a real connected server and VirtualConsole cell color checks.
///
/// Bug: After connecting to a server, typing "server ls --recursive" or "server ls -a"
/// does not highlight the argument name/alias in yellow — they render in default style.
/// </summary>
[TestClass]
public class IntegrationTests_RemoteSyntaxHighlight
{
    private static TestEnvironment CreateEnvironment()
    {
        return new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr => { }); // Server auto-registers ls, cat, stat, etc.
        });
    }

    /// <summary>
    /// Returns the column index where <paramref name="text"/> starts on the cursor row.
    /// </summary>
    private static int FindTextColumn(TestEnvironment env, string text)
    {
        var row = env.Console.VirtualConsole.CursorRow;
        var lineText = env.Console.VirtualConsole.GetRow(row).GetText();
        var idx = lineText.IndexOf(text, StringComparison.Ordinal);
        idx.Should().BeGreaterOrEqualTo(0, $"'{text}' should be present on the cursor row");
        return idx;
    }

    /// <summary>
    /// Bug repro: "server ls --recursive" — the "--recursive" argument name
    /// should be styled yellow (256-color index 11) per the theme's ArgumentName style.
    /// </summary>
    [TestMethod]
    public async Task RemoteCommand_ArgumentName_IsHighlightedYellow()
    {
        // Arrange
        using var env = CreateEnvironment();
        await env.ConnectToServerAsync();

        // Act — type "server ls --recursive"
        await env.Keyboard.TypeTextAsync("server ls --recursive");

        // Assert — verify text is displayed
        var row = env.Console.VirtualConsole.CursorRow;
        var lineText = env.Console.VirtualConsole.GetRow(row).GetText().TrimEnd();
        lineText.Should().Contain("server ls --recursive");

        var serverCol = FindTextColumn(env, "server ls");

        // "server" → cyan (256-color index 14)
        var serverCell = env.Console.VirtualConsole.GetCell(row, serverCol);
        serverCell.Style.Foreground256.Should().Be(14, "Group 'server' should be Cyan");

        // "--recursive" starts at serverCol + 10 (after "server ls ")
        var argCell = env.Console.VirtualConsole.GetCell(row, serverCol + 10);
        argCell.Style.Foreground256.Should().Be(11,
            "'--recursive' argument name should be highlighted Yellow after remote command 'ls'");
    }

    /// <summary>
    /// Bug repro: "server ls -a" — the "-a" alias
    /// should be styled yellow (256-color index 11) per the theme's ArgumentAlias style.
    /// </summary>
    [TestMethod]
    public async Task RemoteCommand_ArgumentAlias_IsHighlightedYellow()
    {
        // Arrange
        using var env = CreateEnvironment();
        await env.ConnectToServerAsync();

        // Act — type "server ls -a"
        await env.Keyboard.TypeTextAsync("server ls -a");

        // Assert — verify text is displayed
        var row = env.Console.VirtualConsole.CursorRow;
        var lineText = env.Console.VirtualConsole.GetRow(row).GetText().TrimEnd();
        lineText.Should().Contain("server ls -a");

        var serverCol = FindTextColumn(env, "server ls");

        // "server" → cyan (256-color index 14)
        var serverCell = env.Console.VirtualConsole.GetCell(row, serverCol);
        serverCell.Style.Foreground256.Should().Be(14, "Group 'server' should be Cyan");

        // "-a" starts at serverCol + 10 (after "server ls ")
        var aliasCell = env.Console.VirtualConsole.GetCell(row, serverCol + 10);
        aliasCell.Style.Foreground256.Should().Be(11,
            "'-a' argument alias should be highlighted Yellow after remote command 'ls'");
    }

    /// <summary>
    /// Contrast test: "server connect --host" — local command argument should already work.
    /// This confirms the highlighting works for local commands but not remote ones.
    /// </summary>
    [TestMethod]
    public async Task LocalCommand_ArgumentName_IsHighlightedYellow()
    {
        // Arrange — use server environment but don't connect (connect is a local command)
        using var env = CreateEnvironment();

        // Act — type "server connect --host"
        await env.Keyboard.TypeTextAsync("server connect --host");

        // Assert
        var row = env.Console.VirtualConsole.CursorRow;
        var lineText = env.Console.VirtualConsole.GetRow(row).GetText().TrimEnd();
        lineText.Should().Contain("server connect --host");

        var serverCol = FindTextColumn(env, "server connect");

        // "server" → cyan
        var serverCell = env.Console.VirtualConsole.GetCell(row, serverCol);
        serverCell.Style.Foreground256.Should().Be(14, "Group 'server' should be Cyan");

        // "--host" starts at serverCol + 15 (after "server connect ")
        var argCell = env.Console.VirtualConsole.GetCell(row, serverCol + 15);
        argCell.Style.Foreground256.Should().Be(11,
            "'--host' argument name after local command should be Yellow");
    }
}
