using System;

namespace BitPantry.VirtualConsole.AnsiParser
{
    /// <summary>
    /// Processes ED (Erase in Display) and EL (Erase in Line) CSI sequences.
    /// ED: CSI Ps J - Erase in display
    /// EL: CSI Ps K - Erase in line
    /// </summary>
    internal class EraseProcessor
    {
        /// <summary>
        /// Returns true if this processor can handle the given CSI sequence.
        /// </summary>
        public bool CanProcess(CsiSequence sequence)
        {
            return sequence.FinalByte == 'J' || sequence.FinalByte == 'K';
        }

        /// <summary>
        /// Processes ED (J) and EL (K) sequences.
        /// </summary>
        public void Process(CsiSequence sequence, ScreenBuffer buffer)
        {
            int param = sequence.Parameters.Length > 0 ? sequence.Parameters[0] : 0;

            if (sequence.FinalByte == 'J')
            {
                ProcessEraseDisplay(param, buffer);
            }
            else if (sequence.FinalByte == 'K')
            {
                ProcessEraseLine(param, buffer);
            }
        }

        /// <summary>
        /// ED - Erase in Display
        /// 0 = Erase from cursor to end of screen
        /// 1 = Erase from start of screen to cursor
        /// 2 = Erase entire screen
        /// </summary>
        private void ProcessEraseDisplay(int param, ScreenBuffer buffer)
        {
            switch (param)
            {
                case 0: // Erase from cursor to end of screen
                    EraseFromCursorToEndOfScreen(buffer);
                    break;
                case 1: // Erase from start to cursor
                    EraseFromStartToCursor(buffer);
                    break;
                case 2: // Erase entire screen
                    EraseEntireScreen(buffer);
                    break;
            }
        }

        /// <summary>
        /// EL - Erase in Line
        /// 0 = Erase from cursor to end of line
        /// 1 = Erase from start of line to cursor
        /// 2 = Erase entire line
        /// </summary>
        private void ProcessEraseLine(int param, ScreenBuffer buffer)
        {
            switch (param)
            {
                case 0: // Erase from cursor to end of line
                    EraseFromCursorToEndOfLine(buffer);
                    break;
                case 1: // Erase from start of line to cursor
                    EraseFromStartOfLineToCursor(buffer);
                    break;
                case 2: // Erase entire line
                    EraseEntireLine(buffer);
                    break;
            }
        }

        private void EraseFromCursorToEndOfScreen(ScreenBuffer buffer)
        {
            // Clear rest of current line
            EraseFromCursorToEndOfLine(buffer);

            // Clear all lines below
            for (int row = buffer.CursorRow + 1; row < buffer.Height; row++)
            {
                EraseLine(buffer, row);
            }
        }

        private void EraseFromStartToCursor(ScreenBuffer buffer)
        {
            // Clear all lines above
            for (int row = 0; row < buffer.CursorRow; row++)
            {
                EraseLine(buffer, row);
            }

            // Clear current line from start to cursor
            EraseFromStartOfLineToCursor(buffer);
        }

        private void EraseEntireScreen(ScreenBuffer buffer)
        {
            buffer.ClearScreen(ClearMode.All);
        }

        private void EraseFromCursorToEndOfLine(ScreenBuffer buffer)
        {
            int row = buffer.CursorRow;
            for (int col = buffer.CursorColumn; col < buffer.Width; col++)
            {
                buffer.SetCell(row, col, ' ', buffer.CurrentStyle);
            }
        }

        private void EraseFromStartOfLineToCursor(ScreenBuffer buffer)
        {
            int row = buffer.CursorRow;
            for (int col = 0; col <= buffer.CursorColumn; col++)
            {
                buffer.SetCell(row, col, ' ', buffer.CurrentStyle);
            }
        }

        private void EraseEntireLine(ScreenBuffer buffer)
        {
            EraseLine(buffer, buffer.CursorRow);
        }

        private void EraseLine(ScreenBuffer buffer, int row)
        {
            for (int col = 0; col < buffer.Width; col++)
            {
                buffer.SetCell(row, col, ' ', buffer.CurrentStyle);
            }
        }
    }
}
