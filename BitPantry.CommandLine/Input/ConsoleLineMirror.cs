using Spectre.Console;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitPantry.CommandLine.Input
{
    public class ConsoleLineMirror
    {
        private IAnsiConsole _console;
        private StringBuilder _mirrorBuffer;
        private IReadOnlyList<StyledSegment> _lastRenderedSegments;
        private readonly int _promptLength;
        private bool _pendingWrap;
        /// <summary>
        /// Tracks how many physical rows below the prompt the cursor currently is.
        /// Used to clamp CUU so the cursor never escapes above the prompt row,
        /// which is critical when Profile.Width doesn't match the actual terminal width.
        /// </summary>
        private int _rowsFromPrompt;

        /// <summary>
        /// Threshold (as a fraction of content length) for using differential rendering.
        /// If the first difference occurs at or after this fraction of the content,
        /// differential rendering is used. Otherwise, a full redraw is performed.
        /// Set to 0.75 (75%) - changes in the last quarter use differential rendering,
        /// earlier changes use full redraw to avoid complex partial updates.
        /// </summary>
        private const double DifferentialRenderingThreshold = 0.75;

        public bool Overwrite { get; set; } = false;

        public string Buffer => _mirrorBuffer.ToString();

        public int BufferPosition { get; private set; }

        private int TerminalWidth => _console.Profile.Width;

        public ConsoleLineMirror(IAnsiConsole console) : this(console, 0) { }

        public ConsoleLineMirror(IAnsiConsole console, int promptLength) : this(console, promptLength, string.Empty, 0) { }

        public ConsoleLineMirror(IAnsiConsole console, string initialInput, int initialPosition)
            : this(console, 0, initialInput, initialPosition) { }

        public ConsoleLineMirror(IAnsiConsole console, int promptLength, string initialInput, int initialPosition)
        {
            _console = console;
            _promptLength = promptLength;
            _mirrorBuffer = new StringBuilder(initialInput);
            BufferPosition = initialPosition;
        }

        public void HideCursor()
        {
            _console.Cursor.Hide();
        }

        public void ShowCursor()
        {
            _console.Cursor.Show();
        }

        /// <summary>
        /// Writes raw text directly to the console output, bypassing Spectre.Console's
        /// Text rendering pipeline which does width-based line splitting.
        /// This ensures text flows naturally with the terminal's auto-wrap behavior.
        /// </summary>
        private void WriteRaw(string text)
        {
            _console.Profile.Out.Writer.Write(text);
        }

        /// <summary>
        /// Writes styled text directly to the console output in chunks smaller than the
        /// terminal width. This prevents Spectre.Console's Text class from applying
        /// width-based line splitting while still using Spectre's color rendering.
        /// </summary>
        private void WriteStyledRaw(string text, Style style)
        {
            int width = TerminalWidth;
            if (width <= 0) width = 80;
            int chunkSize = System.Math.Max(1, width - 1);

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                int len = System.Math.Min(chunkSize, text.Length - i);
                _console.Write(new Text(text.Substring(i, len), style));
            }
        }

        /// <summary>
        /// Updates <see cref="_pendingWrap"/> and <see cref="_rowsFromPrompt"/> after
        /// a write operation. Call with the absolute offset AFTER the write completed.
        /// </summary>
        private void UpdateWrapStateAfterWrite(int afterOffset, int charsWritten = 1)
        {
            int width = TerminalWidth;
            if (width <= 0) width = 80;

            _pendingWrap = charsWritten > 0 && afterOffset > 0 && afterOffset % width == 0;

            // Estimate the row the cursor is on (relative to prompt row 0)
            int estimatedRow = afterOffset / width;
            if (_pendingWrap)
                estimatedRow--; // pending wrap means still on previous row

            if (estimatedRow > _rowsFromPrompt)
                _rowsFromPrompt = estimatedRow;
        }

        /// <summary>
        /// Invalidates the render cache, forcing a full redraw on the next RenderWithStyles call.
        /// Call this after bulk buffer mutations (e.g., ghost text acceptance via Backspace×N + Write)
        /// to ensure the differential rendering logic uses fresh state.
        /// </summary>
        public void InvalidateRenderCache()
        {
            _lastRenderedSegments = null;
        }

        public void Write(char ch)
        {
            Write(ch.ToString());
        }

        public void Backspace()
        {
            if (BufferPosition <= 0)
                return;

            int oldOffset = _promptLength + BufferPosition;

            _mirrorBuffer.Remove(BufferPosition - 1, 1);
            BufferPosition--;

            var newStr = $"{_mirrorBuffer.ToString().Substring(BufferPosition)} ";
            int targetOffset = _promptLength + BufferPosition;

            _console.Cursor.Hide();
            try
            {
                EmitCursorMovement(oldOffset, targetOffset);
                WriteRaw(newStr);
                int afterWriteOffset = targetOffset + newStr.Length;
                UpdateWrapStateAfterWrite(afterWriteOffset, newStr.Length);
                EmitCursorMovement(afterWriteOffset, targetOffset);
            }
            finally
            {
                _console.Cursor.Show();
            }
        }

        public void MovePositionLeft(int steps = 1)
        {
            var newPos = System.Math.Max(0, BufferPosition - steps);
            if (newPos != BufferPosition)
                MoveToPosition(newPos);
        }

        public void MovePositionRight(int steps = 1)
        {
            var newPos = System.Math.Min(_mirrorBuffer.Length, BufferPosition + steps);
            if (newPos != BufferPosition)
                MoveToPosition(newPos);
        }

        public void MoveToPosition(int position)
        {
            if (position == BufferPosition)
                return;
            EmitCursorMovement(_promptLength + BufferPosition, _promptLength + position);
            BufferPosition = position;
        }

        public void Clear(int startPosition = 0)
        {
            var padCount = _mirrorBuffer.Length - startPosition;

            if (padCount > 0)
            {
                _console.Cursor.Hide();
                try
                {
                    MoveToPosition(startPosition);

                    // Erase from cursor to end of display (handles multi-row content)
                    WriteRaw("\x1B[0J");

                    _mirrorBuffer.Remove(startPosition, padCount);
                    _pendingWrap = false;
                }
                finally
                {
                    _console.Cursor.Show();
                }
            }

        }

        public void Write(string str)
        {
            if (Overwrite)
            {
                foreach (var ch in str)
                {
                    if (BufferPosition == _mirrorBuffer.Length)
                        _mirrorBuffer.Append(ch);
                    else
                        _mirrorBuffer[BufferPosition] = ch;

                    BufferPosition++;
                }

                WriteRaw(str);
                int afterOffset = _promptLength + BufferPosition;
                UpdateWrapStateAfterWrite(afterOffset, str.Length);
            }
            else
            {
                _mirrorBuffer.Insert(BufferPosition, str);
                int insertOffset = _promptLength + BufferPosition;
                BufferPosition += str.Length;

                var after = _mirrorBuffer.ToString().Substring(BufferPosition);

                if (after.Length > 0)
                {
                    // Insert mode with content after cursor - hide cursor during rewrite
                    _console.Cursor.Hide();
                    try
                    {
                        WriteRaw(str);
                        WriteRaw(after);
                        int afterWriteOffset = insertOffset + str.Length + after.Length;
                        UpdateWrapStateAfterWrite(afterWriteOffset, str.Length + after.Length);
                        EmitCursorMovement(afterWriteOffset, _promptLength + BufferPosition);
                    }
                    finally
                    {
                        _console.Cursor.Show();
                    }
                }
                else
                {
                    // Appending at end - no cursor move needed
                    WriteRaw(str);
                    int afterOffset = _promptLength + BufferPosition;
                    UpdateWrapStateAfterWrite(afterOffset, str.Length);
                }
            }
        }

        public void Markup(string str)
        {
            var unstr = str.Unmarkup();

            if (Overwrite)
            {
                foreach (var ch in unstr)
                {
                    if (BufferPosition == _mirrorBuffer.Length)
                        _mirrorBuffer.Append(ch);
                    else
                        _mirrorBuffer[BufferPosition] = ch;

                    BufferPosition++;
                }

                WriteRaw(unstr);
                int afterOffset = _promptLength + BufferPosition;
                UpdateWrapStateAfterWrite(afterOffset, unstr.Length);
            }
            else
            {
                _mirrorBuffer.Insert(BufferPosition, unstr);
                int insertOffset = _promptLength + BufferPosition;
                BufferPosition += unstr.Length;

                var after = _mirrorBuffer.ToString().Substring(BufferPosition);

                if (after.Length > 0)
                {
                    // Insert mode with content after cursor - hide cursor during rewrite
                    _console.Cursor.Hide();
                    try
                    {
                        WriteRaw(unstr);
                        WriteRaw(after);
                        int afterWriteOffset = insertOffset + unstr.Length + after.Length;
                        UpdateWrapStateAfterWrite(afterWriteOffset, unstr.Length + after.Length);
                        EmitCursorMovement(afterWriteOffset, _promptLength + BufferPosition);
                    }
                    finally
                    {
                        _console.Cursor.Show();
                    }
                }
                else
                {
                    // Appending at end - no cursor move needed
                    WriteRaw(unstr);
                    int afterOffset = _promptLength + BufferPosition;
                    UpdateWrapStateAfterWrite(afterOffset, unstr.Length);
                }
            }
        }

        internal void Delete()
        {
            if (BufferPosition >= _mirrorBuffer.Length)
                return;

            _mirrorBuffer.Remove(BufferPosition, 1);

            var str = $"{_mirrorBuffer.ToString().Substring(BufferPosition)} ";
            int currentOffset = _promptLength + BufferPosition;

            _console.Cursor.Hide();
            try
            {
                WriteRaw(str);
                int afterWriteOffset = currentOffset + str.Length;
                UpdateWrapStateAfterWrite(afterWriteOffset, str.Length);
                EmitCursorMovement(afterWriteOffset, currentOffset);
            }
            finally
            {
                _console.Cursor.Show();
            }
        }

        /// <summary>
        /// Renders styled segments to the console, replacing current line content.
        /// Uses differential rendering to minimize console writes - only redraws changed portions.
        /// </summary>
        /// <param name="segments">The styled segments to render.</param>
        /// <param name="cursorPosition">The desired cursor position after rendering.</param>
        public void RenderWithStyles(IReadOnlyList<StyledSegment> segments, int cursorPosition)
        {
            // Build new buffer content
            var newContent = BuildBufferContent(segments);
            var oldLength = _mirrorBuffer.Length;
            var newLength = newContent.Length;

            // Determine render strategy
            if (_lastRenderedSegments == null)
            {
                // First render - do full draw
                RenderFull(segments, newContent, cursorPosition);
            }
            else
            {
                // Find first difference point
                var (diffIndex, diffReason) = FindFirstDifferenceIndex(segments);

                if (diffIndex < 0)
                {
                    // No differences - just ensure cursor position is correct
                    MoveToPosition(cursorPosition);
                }
                else if (ShouldUseDifferentialPath(diffIndex, oldLength, newLength, diffReason))
                {
                    // Differential path - rewrite from diff point
                    RenderDifferential(segments, newContent, cursorPosition, diffIndex, oldLength);
                }
                else
                {
                    // Full redraw fallback
                    RenderFull(segments, newContent, cursorPosition);
                }
            }

            // Cache rendered segments for next comparison
            // Always create a defensive copy to prevent external modification
            _lastRenderedSegments = segments.ToList();
        }

        /// <summary>
        /// Builds the combined text content from segments.
        /// </summary>
        private static string BuildBufferContent(IReadOnlyList<StyledSegment> segments)
        {
            var sb = new StringBuilder();
            foreach (var segment in segments)
            {
                sb.Append(segment.Text);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Finds the index where segments first differ from the cached segments.
        /// Returns -1 if no difference, otherwise the character index where difference starts.
        /// </summary>
        private (int Index, DiffReason Reason) FindFirstDifferenceIndex(IReadOnlyList<StyledSegment> newSegments)
        {
            if (_lastRenderedSegments == null)
                return (0, DiffReason.NoCachedState);

            int charIndex = 0;

            // Compare segment by segment
            int segmentIndex = 0;
            while (segmentIndex < _lastRenderedSegments.Count && segmentIndex < newSegments.Count)
            {
                var oldSeg = _lastRenderedSegments[segmentIndex];
                var newSeg = newSegments[segmentIndex];

                // Check if text matches
                if (oldSeg.Text != newSeg.Text)
                {
                    // Text differs - find exact position within segment
                    int minLen = System.Math.Min(oldSeg.Text.Length, newSeg.Text.Length);
                    for (int i = 0; i < minLen; i++)
                    {
                        if (oldSeg.Text[i] != newSeg.Text[i])
                            return (charIndex + i, DiffReason.TextDifference);
                    }
                    // One is prefix of the other
                    return (charIndex + minLen, DiffReason.TextDifference);
                }

                // Check if style matches
                if (!StylesEqual(oldSeg.Style, newSeg.Style))
                {
                    return (charIndex, DiffReason.StyleDifference);
                }

                charIndex += oldSeg.Text.Length;
                segmentIndex++;
            }

            // One list is longer than the other
            if (segmentIndex < newSegments.Count)
            {
                // New segments have more - return position where new content starts
                return (charIndex, DiffReason.AppendedContent);
            }

            if (segmentIndex < _lastRenderedSegments.Count)
            {
                // Old segments had more - content was removed
                return (charIndex, DiffReason.RemovedContent);
            }

            // Completely identical
            return (-1, DiffReason.None);
        }

        /// <summary>
        /// Compares two Spectre.Console Style objects for equality.
        /// </summary>
        private static bool StylesEqual(Style a, Style b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            
            // Compare foreground and background colors
            return Equals(a.Foreground, b.Foreground) && 
                   Equals(a.Background, b.Background) && 
                   a.Decoration == b.Decoration;
        }

        /// <summary>
        /// Determines if differential rendering should be used based on the change type.
        /// </summary>
        private static bool ShouldUseDifferentialPath(int diffIndex, int oldLength, int newLength, DiffReason reason)
        {
            // Use differential path for:
            // - Appended content (typing at end)
            // - Style changes (same text, different colors)
            // - Small text changes near the end
            // 
            // Use full redraw for:
            // - Complete replacement (e.g., history recall)
            // - Mid-line insertion/deletion that shifts subsequent content
            
            switch (reason)
            {
                case DiffReason.AppendedContent:
                case DiffReason.StyleDifference:
                    return true;
                
                case DiffReason.TextDifference:
                case DiffReason.RemovedContent:
                    // If difference is at or after the threshold, use differential
                    // Otherwise use full redraw for clarity
                    var threshold = System.Math.Max(oldLength, newLength) * DifferentialRenderingThreshold;
                    return diffIndex >= threshold;
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// Performs a full redraw of all segments using ANSI erase for efficiency.
        /// </summary>
        private void RenderFull(IReadOnlyList<StyledSegment> segments, string newContent, int cursorPosition)
        {
            HideCursor();
            try
            {
                // Move to start of input
                MoveToPosition(0);

                // Use ANSI ED mode 0 (Erase from cursor to end of display)
                WriteRaw("\x1B[0J");

                // Clear internal buffer state
                _mirrorBuffer.Clear();

                // Render each segment with its style
                foreach (var segment in segments)
                {
                    WriteStyledRaw(segment.Text, segment.Style);
                }

                // Update buffer to match rendered content
                _mirrorBuffer.Append(newContent);
                BufferPosition = newContent.Length;
                int afterWriteOffset = _promptLength + BufferPosition;
                UpdateWrapStateAfterWrite(afterWriteOffset, newContent.Length);

                // Move cursor to desired position
                if (cursorPosition != BufferPosition)
                {
                    EmitCursorMovement(afterWriteOffset, _promptLength + cursorPosition);
                    BufferPosition = cursorPosition;
                    _pendingWrap = false;
                }
            }
            finally
            {
                ShowCursor();
            }
        }

        /// <summary>
        /// Performs differential rendering - only rewrites from the first changed position.
        /// </summary>
        private void RenderDifferential(IReadOnlyList<StyledSegment> segments, string newContent, 
            int cursorPosition, int diffIndex, int oldLength)
        {
            HideCursor();
            try
            {
                // Move cursor to diff position
                MoveToPosition(diffIndex);

                // Find which segments need to be rendered (from diffIndex forward)
                // Track actual chars written to compute real cursor offset afterward
                int actualWrittenChars = 0;
                int charPos = 0;
                foreach (var segment in segments)
                {
                    var segEnd = charPos + segment.Text.Length;
                    
                    if (segEnd > diffIndex)
                    {
                        // This segment contains or is after the diff point
                        if (charPos >= diffIndex)
                        {
                            // Entire segment needs rendering
                            WriteStyledRaw(segment.Text, segment.Style);
                            actualWrittenChars += segment.Text.Length;
                        }
                        else
                        {
                            // Partial segment - only render from diff point
                            var offset = diffIndex - charPos;
                            var partialText = segment.Text.Substring(offset);
                            WriteStyledRaw(partialText, segment.Style);
                            actualWrittenChars += partialText.Length;
                        }
                    }
                    
                    charPos = segEnd;
                }

                // Clear any trailing characters if new content is shorter
                var newLength = newContent.Length;
                if (newLength < oldLength)
                {
                    // Use ANSI ED mode 0 to erase from cursor to end of display
                    // This handles multi-row trailing content correctly
                    WriteRaw("\x1B[0J");
                }

                // Compute the actual cursor offset after the writes above.
                // The cursor is at diffIndex + actualWrittenChars, NOT necessarily
                // at the end of the new content (e.g., when no chars were written
                // because diffIndex >= newContent.Length, only an erase was done).
                int cursorAfterWriteOffset = _promptLength + diffIndex + actualWrittenChars;
                // Pending wrap only occurs when a character write fills the last column
                // of a row (VirtualConsole delays the line wrap). Movement commands and
                // erase sequences never produce pending wrap, so require actualWrittenChars > 0.
                UpdateWrapStateAfterWrite(cursorAfterWriteOffset, actualWrittenChars);

                // Update buffer
                _mirrorBuffer.Clear();
                _mirrorBuffer.Append(newContent);
                BufferPosition = newContent.Length;

                // Move cursor to desired position
                int targetOffset = _promptLength + cursorPosition;
                if (cursorAfterWriteOffset != targetOffset || _pendingWrap)
                {
                    EmitCursorMovement(cursorAfterWriteOffset, targetOffset);
                    BufferPosition = cursorPosition;
                    _pendingWrap = false;
                }
            }
            finally
            {
                ShowCursor();
            }
        }

        /// <summary>
        /// Emits ANSI cursor movement codes to move from one absolute offset to another,
        /// handling row wrapping via CUU/CUD and column positioning via CHA.
        /// Accounts for delayed-wrap terminal state when the cursor is at a row boundary.
        /// </summary>
        private void EmitCursorMovement(int fromOffset, int toOffset)
        {
            if (fromOffset == toOffset && !_pendingWrap)
                return;

            int width = TerminalWidth;
            if (width <= 0) width = 80;

            int fromRow, fromCol;
            if (_pendingWrap && fromOffset > 0 && fromOffset % width == 0)
            {
                // Delayed wrap: cursor hasn't wrapped yet, still on the previous row.
                // Physical cursor column is 'width' (one past last valid column)
                // so any CHA emission will correctly resolve the pending state.
                fromRow = (fromOffset / width) - 1;
                fromCol = width;
            }
            else
            {
                fromRow = fromOffset / width;
                fromCol = fromOffset % width;
            }

            int toRow = toOffset / width;
            int toCol = toOffset % width;

            int deltaRow = toRow - fromRow;

            // Clamp CUU to never go above the prompt row. If Profile.Width doesn't match
            // the actual terminal width, fromRow may be larger than the physical row offset,
            // causing too many CUU commands that escape above the prompt into prior content.
            if (deltaRow < 0 && -deltaRow > _rowsFromPrompt)
            {
                deltaRow = -_rowsFromPrompt;
            }

            if (deltaRow > 0)
            {
                if (_pendingWrap)
                {
                    // CUD (Cursor Down, \e[B) does NOT scroll when the cursor is at
                    // the bottom of the terminal's scrolling region — it silently
                    // does nothing.  When a pending wrap coincides with the last
                    // visible row the cursor stays put and subsequent output
                    // overwrites the current line (the prompt).
                    //
                    // Fix: use CR to clear the pending-wrap flag (staying on the
                    // same row, moving to column 0), then LF for each row to
                    // advance.  LF scrolls at the bottom margin, unlike CUD.
                    // CHA that follows will set the correct column.
                    WriteRaw("\r");
                    for (int i = 0; i < deltaRow; i++)
                        WriteRaw("\n");
                }
                else
                {
                    _console.Cursor.MoveDown(deltaRow);
                }
                _rowsFromPrompt += deltaRow;
            }
            else if (deltaRow < 0)
            {
                _console.Cursor.MoveUp(-deltaRow);
                _rowsFromPrompt += deltaRow; // negative, so decrements
            }

            // Use CHA (Cursor Horizontal Absolute) for column positioning (1-based)
            if (fromCol != toCol || deltaRow != 0)
            {
                WriteRaw($"\x1B[{toCol + 1}G");
            }

            _pendingWrap = false;
        }

        /// <summary>
        /// Reason for difference between old and new segments.
        /// </summary>
        private enum DiffReason
        {
            None,
            NoCachedState,
            TextDifference,
            StyleDifference,
            AppendedContent,
            RemovedContent
        }
    }
}
