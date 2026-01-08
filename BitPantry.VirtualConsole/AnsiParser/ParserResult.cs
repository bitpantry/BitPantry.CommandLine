namespace BitPantry.VirtualConsole.AnsiParser
{
    /// <summary>
    /// Base class for ANSI parser results.
    /// </summary>
    public abstract class ParserResult
    {
    }

    /// <summary>
    /// Result indicating a printable character should be output.
    /// </summary>
    public class PrintResult : ParserResult
    {
        /// <summary>
        /// The character to print.
        /// </summary>
        public char Character { get; }

        /// <summary>
        /// Creates a new print result.
        /// </summary>
        public PrintResult(char character)
        {
            Character = character;
        }
    }

    /// <summary>
    /// Result indicating a complete CSI sequence was parsed.
    /// </summary>
    public class SequenceResult : ParserResult
    {
        /// <summary>
        /// The parsed CSI sequence.
        /// </summary>
        public CsiSequence Sequence { get; }

        /// <summary>
        /// Creates a new sequence result.
        /// </summary>
        public SequenceResult(CsiSequence sequence)
        {
            Sequence = sequence;
        }
    }

    /// <summary>
    /// Result indicating a control character was encountered.
    /// </summary>
    public class ControlResult : ParserResult
    {
        /// <summary>
        /// The control code.
        /// </summary>
        public ControlCode Code { get; }

        /// <summary>
        /// Creates a new control result.
        /// </summary>
        public ControlResult(ControlCode code)
        {
            Code = code;
        }
    }

    /// <summary>
    /// Result indicating no action should be taken (sequence still building).
    /// </summary>
    public class NoActionResult : ParserResult
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly NoActionResult Instance = new NoActionResult();

        private NoActionResult() { }
    }

    /// <summary>
    /// Control character codes.
    /// </summary>
    public enum ControlCode
    {
        /// <summary>Backspace (0x08)</summary>
        Backspace,
        /// <summary>Tab (0x09)</summary>
        Tab,
        /// <summary>Line feed (0x0A)</summary>
        LineFeed,
        /// <summary>Carriage return (0x0D)</summary>
        CarriageReturn,
        /// <summary>Bell (0x07)</summary>
        Bell
    }
}
