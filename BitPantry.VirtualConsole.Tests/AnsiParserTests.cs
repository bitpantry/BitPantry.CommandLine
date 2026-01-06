using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.VirtualConsole.AnsiParser;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class AnsiParserTests
{
    // T031: Tests for AnsiSequenceParser state machine basics
    [TestMethod]
    public void Process_PrintableCharacter_ShouldReturnPrintResult()
    {
        var parser = new AnsiSequenceParser();
        
        var result = parser.Process('A');
        
        result.Should().BeOfType<PrintResult>();
        ((PrintResult)result).Character.Should().Be('A');
    }

    [TestMethod]
    public void Process_Escape_ShouldReturnNoAction()
    {
        var parser = new AnsiSequenceParser();
        
        var result = parser.Process('\x1b'); // ESC
        
        result.Should().BeOfType<NoActionResult>();
    }

    [TestMethod]
    public void Process_CsiIntroducer_ShouldReturnNoAction()
    {
        var parser = new AnsiSequenceParser();
        parser.Process('\x1b'); // ESC
        
        var result = parser.Process('[');
        
        result.Should().BeOfType<NoActionResult>();
    }

    [TestMethod]
    public void Process_CompleteCsiSequence_ShouldReturnSequenceResult()
    {
        var parser = new AnsiSequenceParser();
        parser.Process('\x1b'); // ESC
        parser.Process('[');    // [
        
        var result = parser.Process('A'); // CUU - cursor up
        
        result.Should().BeOfType<SequenceResult>();
    }

    [TestMethod]
    public void Reset_ShouldReturnToGroundState()
    {
        var parser = new AnsiSequenceParser();
        parser.Process('\x1b'); // Start escape sequence
        
        parser.Reset();
        var result = parser.Process('A');
        
        result.Should().BeOfType<PrintResult>(); // Should print normally, not be part of sequence
    }

    // T032: Tests for CSI sequence parameter parsing
    [TestMethod]
    public void Process_CsiWithSingleParameter_ShouldParseParameter()
    {
        var parser = new AnsiSequenceParser();
        // ESC [ 5 A = cursor up 5
        parser.Process('\x1b');
        parser.Process('[');
        parser.Process('5');
        var result = parser.Process('A');
        
        var seqResult = result.Should().BeOfType<SequenceResult>().Subject;
        seqResult.Sequence.Parameters.Should().Equal(5);
        seqResult.Sequence.FinalByte.Should().Be('A');
    }

    [TestMethod]
    public void Process_CsiWithMultipleParameters_ShouldParseAll()
    {
        var parser = new AnsiSequenceParser();
        // ESC [ 10 ; 20 H = cursor position (row 10, col 20)
        parser.Process('\x1b');
        parser.Process('[');
        parser.Process('1');
        parser.Process('0');
        parser.Process(';');
        parser.Process('2');
        parser.Process('0');
        var result = parser.Process('H');
        
        var seqResult = result.Should().BeOfType<SequenceResult>().Subject;
        seqResult.Sequence.Parameters.Should().Equal(10, 20);
        seqResult.Sequence.FinalByte.Should().Be('H');
    }

    [TestMethod]
    public void Process_CsiWithNoParameters_ShouldHaveEmptyParameters()
    {
        var parser = new AnsiSequenceParser();
        // ESC [ A = cursor up 1 (default)
        parser.Process('\x1b');
        parser.Process('[');
        var result = parser.Process('A');
        
        var seqResult = result.Should().BeOfType<SequenceResult>().Subject;
        seqResult.Sequence.Parameters.Should().BeEmpty();
    }

    [TestMethod]
    public void Process_CsiWithMultiDigitParameter_ShouldParseCorrectly()
    {
        var parser = new AnsiSequenceParser();
        // ESC [ 123 D = cursor back 123
        parser.Process('\x1b');
        parser.Process('[');
        parser.Process('1');
        parser.Process('2');
        parser.Process('3');
        var result = parser.Process('D');
        
        var seqResult = result.Should().BeOfType<SequenceResult>().Subject;
        seqResult.Sequence.Parameters.Should().Equal(123);
    }

    [TestMethod]
    public void Process_CsiWithManyParameters_ShouldParseAll()
    {
        var parser = new AnsiSequenceParser();
        // ESC [ 1;2;3;4;5 m = multiple SGR codes
        parser.Process('\x1b');
        parser.Process('[');
        parser.Process('1');
        parser.Process(';');
        parser.Process('2');
        parser.Process(';');
        parser.Process('3');
        parser.Process(';');
        parser.Process('4');
        parser.Process(';');
        parser.Process('5');
        var result = parser.Process('m');
        
        var seqResult = result.Should().BeOfType<SequenceResult>().Subject;
        seqResult.Sequence.Parameters.Should().Equal(1, 2, 3, 4, 5);
    }

    [TestMethod]
    public void Process_CarriageReturn_ShouldReturnControlResult()
    {
        var parser = new AnsiSequenceParser();
        
        var result = parser.Process('\r');
        
        result.Should().BeOfType<ControlResult>();
        ((ControlResult)result).Code.Should().Be(ControlCode.CarriageReturn);
    }

    [TestMethod]
    public void Process_LineFeed_ShouldReturnControlResult()
    {
        var parser = new AnsiSequenceParser();
        
        var result = parser.Process('\n');
        
        result.Should().BeOfType<ControlResult>();
        ((ControlResult)result).Code.Should().Be(ControlCode.LineFeed);
    }

    [TestMethod]
    public void Process_Tab_ShouldReturnControlResult()
    {
        var parser = new AnsiSequenceParser();
        
        var result = parser.Process('\t');
        
        result.Should().BeOfType<ControlResult>();
        ((ControlResult)result).Code.Should().Be(ControlCode.Tab);
    }

    [TestMethod]
    public void Process_Backspace_ShouldReturnControlResult()
    {
        var parser = new AnsiSequenceParser();
        
        var result = parser.Process('\b');
        
        result.Should().BeOfType<ControlResult>();
        ((ControlResult)result).Code.Should().Be(ControlCode.Backspace);
    }

    // T077: Unknown sequence handling - ED and EL sequences should be recognized
    [TestMethod]
    public void Process_EdSequence_ShouldReturnSequenceResult()
    {
        var parser = new AnsiSequenceParser();
        // ESC [ 2 J = ED (erase display, all)
        parser.Process('\x1b');
        parser.Process('[');
        parser.Process('2');
        var result = parser.Process('J');
        
        var seqResult = result.Should().BeOfType<SequenceResult>().Subject;
        seqResult.Sequence.FinalByte.Should().Be('J');
        seqResult.Sequence.Parameters.Should().Equal(2);
    }

    [TestMethod]
    public void Process_ElSequence_ShouldReturnSequenceResult()
    {
        var parser = new AnsiSequenceParser();
        // ESC [ 0 K = EL (erase line, to end)
        parser.Process('\x1b');
        parser.Process('[');
        parser.Process('0');
        var result = parser.Process('K');
        
        var seqResult = result.Should().BeOfType<SequenceResult>().Subject;
        seqResult.Sequence.FinalByte.Should().Be('K');
        seqResult.Sequence.Parameters.Should().Equal(0);
    }

    [TestMethod]
    public void Process_EdWithoutParameter_ShouldDefaultToZero()
    {
        var parser = new AnsiSequenceParser();
        // ESC [ J = ED with default parameter (0)
        parser.Process('\x1b');
        parser.Process('[');
        var result = parser.Process('J');
        
        var seqResult = result.Should().BeOfType<SequenceResult>().Subject;
        seqResult.Sequence.FinalByte.Should().Be('J');
        // Default parameter should be 0 (or empty array)
    }
}
