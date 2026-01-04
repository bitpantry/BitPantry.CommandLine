using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.VirtualConsole;

/// <summary>
/// Tests for ConsolidatedTestConsole that wraps Spectre's TestConsole
/// and adds cursor position tracking for visual tests.
/// </summary>
[TestClass]
public class ConsolidatedTestConsoleTests
{
    [TestMethod]
    public void Console_Should_Implement_IAnsiConsole()
    {
        // Arrange & Act
        var console = new ConsolidatedTestConsole();

        // Assert
        console.Should().BeAssignableTo<IAnsiConsole>();
        console.Dispose();
    }

    [TestMethod]
    public void Output_Should_Return_Written_Text()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24);

        // Act
        console.Write(new Text("Hello World"));

        // Assert
        console.Output.Should().Contain("Hello World");
        console.Dispose();
    }

    [TestMethod]
    public void CursorPosition_Should_Track_After_Write()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        // Act
        console.Write(new Text("hello"));

        // Assert
        console.CursorPosition.Column.Should().Be(5);
        console.CursorPosition.Line.Should().Be(0);
        console.Dispose();
    }

    [TestMethod]
    public void CursorPosition_Should_Track_After_Newline()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        // Act
        console.Write(new Text("hello"));
        console.WriteLine();

        // Assert
        console.CursorPosition.Line.Should().Be(1);
        console.Dispose();
    }

    [TestMethod]
    public void CursorPosition_Should_Track_ANSI_CursorUp()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        // Write two lines then move up
        console.WriteLine("line 1");
        console.WriteLine("line 2");
        var startLine = console.CursorPosition.Line;

        // Act - Write cursor up ANSI sequence
        console.Write(new ControlCode("\u001b[1A"));

        // Assert
        console.CursorPosition.Line.Should().Be(startLine - 1);
        console.Dispose();
    }

    [TestMethod]
    public void CursorPosition_Should_Track_Carriage_Return()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        console.Write(new Text("hello world"));
        console.CursorPosition.Column.Should().Be(11);

        // Act - Write carriage return
        console.Write(new ControlCode("\r"));

        // Assert
        console.CursorPosition.Column.Should().Be(0);
        console.Dispose();
    }

    [TestMethod]
    public void Lines_Should_Return_Written_Lines()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24);

        // Act
        console.WriteLine("line 1");
        console.WriteLine("line 2");

        // Assert
        console.Lines.Should().HaveCountGreaterOrEqualTo(2);
        console.Dispose();
    }

    [TestMethod]
    public void GetCursorPosition_Should_Return_Current_Position()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24);

        console.Write(new Text("test"));

        // Act
        var position = console.GetCursorPosition();

        // Assert
        position.Column.Should().Be(4);
        position.Line.Should().Be(0);
        console.Dispose();
    }

    [TestMethod]
    public void SetCursorPosition_Should_Update_Position()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24);

        // Act
        console.SetCursorPosition(10, 5);

        // Assert
        var position = console.GetCursorPosition();
        position.Column.Should().Be(10);
        position.Line.Should().Be(5);
        console.Dispose();
    }

    [TestMethod]
    public void Input_Should_Be_Available_For_Queueing_Keys()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24);

        // Act & Assert
        console.Input.Should().NotBeNull();
        console.Dispose();
    }

    [TestMethod]
    public void Clear_Should_Reset_Console()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24);

        console.Write(new Text("some content"));

        // Act
        console.Clear(home: true);

        // Assert
        console.CursorPosition.Column.Should().Be(0);
        console.CursorPosition.Line.Should().Be(0);
        console.Dispose();
    }

    [TestMethod]
    public void Profile_Should_Have_Expected_Settings()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(100)
            .Height(50);

        // Assert
        console.Profile.Width.Should().Be(100);
        console.Profile.Height.Should().Be(50);
        console.Dispose();
    }

    [TestMethod]
    public void EmitAnsiSequences_Should_Include_Escape_Codes()
    {
        // Arrange
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        // Act
        console.Write(new ControlCode("\u001b[2A"));

        // Assert
        console.Output.Should().Contain("\u001b[2A");
        console.Dispose();
    }
}
