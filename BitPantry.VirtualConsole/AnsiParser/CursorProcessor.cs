namespace BitPantry.VirtualConsole.AnsiParser
{
    /// <summary>
    /// Processes cursor movement CSI sequences.
    /// </summary>
    public class CursorProcessor
    {
        /// <summary>
        /// Determines if this processor can handle the given CSI command.
        /// </summary>
        /// <param name="finalByte">The final byte of the CSI sequence.</param>
        /// <returns>True if this processor handles the command.</returns>
        public bool CanProcess(char finalByte)
        {
            switch (finalByte)
            {
                case 'A': // CUU - Cursor Up
                case 'B': // CUD - Cursor Down
                case 'C': // CUF - Cursor Forward
                case 'D': // CUB - Cursor Back
                case 'H': // CUP - Cursor Position
                case 'f': // HVP - Horizontal Vertical Position (same as CUP)
                case 'G': // CHA - Cursor Horizontal Absolute
                case 'd': // VPA - Vertical Position Absolute
                case 'E': // CNL - Cursor Next Line
                case 'F': // CPL - Cursor Previous Line
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Processes a cursor movement sequence.
        /// </summary>
        /// <param name="sequence">The CSI sequence.</param>
        /// <param name="buffer">The screen buffer to modify.</param>
        public void Process(CsiSequence sequence, ScreenBuffer buffer)
        {
            switch (sequence.FinalByte)
            {
                case 'A': // CUU - Cursor Up
                    {
                        int n = sequence.GetParameter(0, 1);
                        buffer.MoveCursorRelative(-n, 0);
                    }
                    break;

                case 'B': // CUD - Cursor Down
                    {
                        int n = sequence.GetParameter(0, 1);
                        buffer.MoveCursorRelative(n, 0);
                    }
                    break;

                case 'C': // CUF - Cursor Forward
                    {
                        int n = sequence.GetParameter(0, 1);
                        buffer.MoveCursorRelative(0, n);
                    }
                    break;

                case 'D': // CUB - Cursor Back
                    {
                        int n = sequence.GetParameter(0, 1);
                        buffer.MoveCursorRelative(0, -n);
                    }
                    break;

                case 'H': // CUP - Cursor Position
                case 'f': // HVP - same as CUP
                    {
                        int row = sequence.GetParameter(0, 1);
                        int col = sequence.GetParameter(1, 1);
                        // ANSI uses 1-based coordinates
                        buffer.MoveCursor(row - 1, col - 1);
                    }
                    break;

                case 'G': // CHA - Cursor Horizontal Absolute
                    {
                        int col = sequence.GetParameter(0, 1);
                        buffer.MoveCursor(buffer.CursorRow, col - 1);
                    }
                    break;

                case 'd': // VPA - Vertical Position Absolute
                    {
                        int row = sequence.GetParameter(0, 1);
                        buffer.MoveCursor(row - 1, buffer.CursorColumn);
                    }
                    break;

                case 'E': // CNL - Cursor Next Line
                    {
                        int n = sequence.GetParameter(0, 1);
                        buffer.MoveCursorRelative(n, 0);
                        buffer.MoveCursor(buffer.CursorRow, 0);
                    }
                    break;

                case 'F': // CPL - Cursor Previous Line
                    {
                        int n = sequence.GetParameter(0, 1);
                        buffer.MoveCursorRelative(-n, 0);
                        buffer.MoveCursor(buffer.CursorRow, 0);
                    }
                    break;
            }
        }
    }
}
