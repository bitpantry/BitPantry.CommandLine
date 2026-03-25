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

        public bool Overwrite { get; set; } = false;

        public string Buffer => _mirrorBuffer.ToString();

        public int BufferPosition { get; private set; }

        public ConsoleLineMirror(IAnsiConsole console) : this(console, string.Empty, 0) { }

        public ConsoleLineMirror(IAnsiConsole console, string initialInput, int initialPosition)
        {
            _console = console;
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

        public void Write(char ch)
        {
            Write(ch.ToString());
        }

        public void Backspace()
        {
            if (BufferPosition <= 0)
                return;

            _mirrorBuffer.Remove(BufferPosition - 1, 1);
            BufferPosition--;

            var newStr = $"{_mirrorBuffer.ToString().Substring(BufferPosition)} ";

            _console.Cursor.MoveLeft();
            _console.Write(newStr);
            _console.Cursor.MoveLeft(newStr.Length);
        }

        public void MovePositionLeft(int steps = 1)
        {
            for (int i = 0; i < steps; i++)
            {
                if (BufferPosition <= 0)
                    return;

                BufferPosition--;
                _console.Cursor.MoveLeft();
            }
        }

        public void MovePositionRight(int steps = 1)
        {
            for (int i = 0; i < steps; i++)
            {
                if (BufferPosition >= _mirrorBuffer.Length)
                    return;

                BufferPosition++;
                _console.Cursor.MoveRight();
            }
        }

        public void MoveToPosition(int position)
        {
            if (BufferPosition < position)
                MovePositionRight(position - BufferPosition);

            if (position < BufferPosition)
                MovePositionLeft(BufferPosition - position);
        }

        public void Clear(int startPosition = 0)
        {
            var padCount = _mirrorBuffer.Length - startPosition;

            if (padCount > 0)
            {
                MoveToPosition(startPosition);

                for (int i = 0; i < padCount; i++)
                {
                    _mirrorBuffer.Remove(startPosition, 1);
                    _console.Write(" ");
                }

                _console.Cursor.MoveLeft(padCount);
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

                _console.Write(str);
            }
            else
            {
                _mirrorBuffer.Insert(BufferPosition, str);
                BufferPosition += str.Length;

                var after = _mirrorBuffer.ToString().Substring(BufferPosition);

                _console.Write(str);
                _console.Write(after);
                _console.Cursor.MoveLeft(after.Length);
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

                _console.Markup(str);
            }
            else
            {
                _mirrorBuffer.Insert(BufferPosition, unstr);
                BufferPosition += unstr.Length;

                var after = _mirrorBuffer.ToString().Substring(BufferPosition);

                _console.Markup(str);
                _console.Write(after);
                _console.Cursor.MoveLeft(after.Length);
            }
        }

        internal void Delete()
        {
            if (BufferPosition >= _mirrorBuffer.Length)
                return;

            _mirrorBuffer.Remove(BufferPosition, 1);

            var str = $"{_mirrorBuffer.ToString().Substring(BufferPosition)} ";

            _console.Write(str);
            _console.Cursor.MoveLeft(str.Length);
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
                    // If difference is in last 25% of content, use differential
                    // Otherwise use full redraw for clarity
                    var threshold = System.Math.Max(oldLength, newLength) * 0.75;
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
                // Move to start and use ANSI erase-to-end-of-line
                MoveToPosition(0);
                
                // Use ANSI CSI K (Erase in Line) - erase from cursor to end of line
                // This is more efficient than writing spaces character by character
                _console.Write("\x1B[K");
                
                // Clear internal buffer state
                _mirrorBuffer.Clear();

                // Render each segment with its style
                foreach (var segment in segments)
                {
                    _console.Write(new Text(segment.Text, segment.Style));
                }

                // Update buffer to match rendered content
                _mirrorBuffer.Append(newContent);
                BufferPosition = newContent.Length;

                // Move cursor to desired position
                if (cursorPosition < BufferPosition)
                {
                    var moveLeft = BufferPosition - cursorPosition;
                    _console.Cursor.MoveLeft(moveLeft);
                    BufferPosition = cursorPosition;
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
                            _console.Write(new Text(segment.Text, segment.Style));
                        }
                        else
                        {
                            // Partial segment - only render from diff point
                            var offset = diffIndex - charPos;
                            var partialText = segment.Text.Substring(offset);
                            _console.Write(new Text(partialText, segment.Style));
                        }
                    }
                    
                    charPos = segEnd;
                }

                // Clear any trailing characters if new content is shorter
                var newLength = newContent.Length;
                if (newLength < oldLength)
                {
                    var trailingChars = oldLength - newLength;
                    // Use ANSI erase from cursor to end of line
                    _console.Write("\x1B[K");
                }

                // Update buffer
                _mirrorBuffer.Clear();
                _mirrorBuffer.Append(newContent);
                BufferPosition = newContent.Length;

                // Move cursor to desired position
                if (cursorPosition < BufferPosition)
                {
                    var moveLeft = BufferPosition - cursorPosition;
                    _console.Cursor.MoveLeft(moveLeft);
                    BufferPosition = cursorPosition;
                }
            }
            finally
            {
                ShowCursor();
            }
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
