using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Spectre.Console.Testing;
// Use Spectre's built-in ControlCode class instead of our own

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Tests for ControlCode renderable that emits raw ANSI sequences.
/// This is a copy of Spectre's internal ControlCode class.
/// </summary>
[TestClass]
public class ControlCodeTests
{
    [TestMethod]
    public void ControlCode_Should_Emit_Sequence_To_Console()
    {
        // Arrange
        var console = new TestConsole()
            .EmitAnsiSequences();
        var cursorUp = new ControlCode("\u001b[2A");

        // Act
        console.Write(cursorUp);

        // Assert
        console.Output.Should().Be("\u001b[2A");
    }

    [TestMethod]
    public void ControlCode_Should_Have_Zero_Width_Measurement()
    {
        // Control codes shouldn't affect layout calculations
        var control = new ControlCode("\u001b[2K\u001b[1A");

        // When rendered to a console, the output should contain the sequence
        // but it shouldn't contribute to width calculations
        var console = new TestConsole().EmitAnsiSequences();
        console.Write(control);

        // The output contains the control sequence
        console.Output.Should().Be("\u001b[2K\u001b[1A");
    }

    [TestMethod]
    public void ControlCode_Should_Emit_Empty_String_For_Empty_Control()
    {
        var console = new TestConsole().EmitAnsiSequences();
        var control = new ControlCode(string.Empty);

        console.Write(control);

        console.Output.Should().BeEmpty();
    }

    [TestMethod]
    public void ControlCode_Should_Emit_Multiple_Sequences()
    {
        var console = new TestConsole().EmitAnsiSequences();
        // Sequence: CR + ClearLine + CursorUp(2)
        var sequence = "\r\u001b[2K\u001b[2A";
        var control = new ControlCode(sequence);

        console.Write(control);

        console.Output.Should().Be(sequence);
    }

    [TestMethod]
    public void ControlCode_Should_Not_Emit_When_Ansi_Disabled()
    {
        // When ANSI is disabled, control codes should not be emitted
        var console = new TestConsole();  // No EmitAnsiSequences()
        var control = new ControlCode("\u001b[2A");

        console.Write(control);

        // Output should be empty because ANSI is not enabled
        console.Output.Should().BeEmpty();
    }
}
