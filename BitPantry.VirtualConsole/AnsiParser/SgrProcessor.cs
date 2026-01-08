using System;

namespace BitPantry.VirtualConsole.AnsiParser
{
    /// <summary>
    /// Processes SGR (Select Graphic Rendition) sequences for colors and text attributes.
    /// </summary>
    public class SgrProcessor
    {
        /// <summary>
        /// Determines if this processor can handle the given CSI command.
        /// </summary>
        /// <param name="finalByte">The final byte of the CSI sequence.</param>
        /// <returns>True if this processor handles the command.</returns>
        public bool CanProcess(char finalByte)
        {
            return finalByte == 'm';
        }

        /// <summary>
        /// Processes an SGR sequence.
        /// </summary>
        /// <param name="sequence">The CSI sequence.</param>
        /// <param name="buffer">The screen buffer to modify.</param>
        public void Process(CsiSequence sequence, ScreenBuffer buffer)
        {
            var parameters = sequence.Parameters;
            
            // Empty parameters means reset
            if (parameters.Length == 0)
            {
                buffer.ResetStyle();
                return;
            }

            int i = 0;
            while (i < parameters.Length)
            {
                int code = parameters[i];
                i++;

                // Check for extended color sequences
                if (code == 38 && i < parameters.Length)
                {
                    // Foreground extended color
                    i = ProcessExtendedForegroundColor(parameters, i, buffer);
                    continue;
                }
                
                if (code == 48 && i < parameters.Length)
                {
                    // Background extended color
                    i = ProcessExtendedBackgroundColor(parameters, i, buffer);
                    continue;
                }

                ProcessSgrCode(code, buffer);
            }
        }

        private int ProcessExtendedForegroundColor(int[] parameters, int i, ScreenBuffer buffer)
        {
            if (i >= parameters.Length)
                return i;

            int subCode = parameters[i];
            i++;

            if (subCode == 5 && i < parameters.Length)
            {
                // 256-color mode: 38;5;n
                byte colorIndex = (byte)Math.Min(255, Math.Max(0, parameters[i]));
                buffer.ApplyStyle(buffer.CurrentStyle.WithForeground256(colorIndex));
                i++;
            }
            else if (subCode == 2 && i + 2 < parameters.Length)
            {
                // TrueColor mode: 38;2;r;g;b
                byte r = (byte)Math.Min(255, Math.Max(0, parameters[i]));
                byte g = (byte)Math.Min(255, Math.Max(0, parameters[i + 1]));
                byte b = (byte)Math.Min(255, Math.Max(0, parameters[i + 2]));
                buffer.ApplyStyle(buffer.CurrentStyle.WithForegroundRgb(r, g, b));
                i += 3;
            }

            return i;
        }

        private int ProcessExtendedBackgroundColor(int[] parameters, int i, ScreenBuffer buffer)
        {
            if (i >= parameters.Length)
                return i;

            int subCode = parameters[i];
            i++;

            if (subCode == 5 && i < parameters.Length)
            {
                // 256-color mode: 48;5;n
                byte colorIndex = (byte)Math.Min(255, Math.Max(0, parameters[i]));
                buffer.ApplyStyle(buffer.CurrentStyle.WithBackground256(colorIndex));
                i++;
            }
            else if (subCode == 2 && i + 2 < parameters.Length)
            {
                // TrueColor mode: 48;2;r;g;b
                byte r = (byte)Math.Min(255, Math.Max(0, parameters[i]));
                byte g = (byte)Math.Min(255, Math.Max(0, parameters[i + 1]));
                byte b = (byte)Math.Min(255, Math.Max(0, parameters[i + 2]));
                buffer.ApplyStyle(buffer.CurrentStyle.WithBackgroundRgb(r, g, b));
                i += 3;
            }

            return i;
        }

        private void ProcessSgrCode(int code, ScreenBuffer buffer)
        {
            switch (code)
            {
                // Reset
                case 0:
                    buffer.ResetStyle();
                    break;

                // Attributes ON
                case 1:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithAttribute(CellAttributes.Bold));
                    break;
                case 2:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithAttribute(CellAttributes.Dim));
                    break;
                case 3:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithAttribute(CellAttributes.Italic));
                    break;
                case 4:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithAttribute(CellAttributes.Underline));
                    break;
                case 5:
                case 6: // Rapid blink (treat same as blink)
                    buffer.ApplyStyle(buffer.CurrentStyle.WithAttribute(CellAttributes.Blink));
                    break;
                case 7:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithAttribute(CellAttributes.Reverse));
                    break;
                case 8:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithAttribute(CellAttributes.Hidden));
                    break;
                case 9:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithAttribute(CellAttributes.Strikethrough));
                    break;

                // Attributes OFF
                case 22: // Normal intensity (not bold, not dim)
                    buffer.ApplyStyle(buffer.CurrentStyle
                        .WithoutAttribute(CellAttributes.Bold)
                        .WithoutAttribute(CellAttributes.Dim));
                    break;
                case 23:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithoutAttribute(CellAttributes.Italic));
                    break;
                case 24:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithoutAttribute(CellAttributes.Underline));
                    break;
                case 25:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithoutAttribute(CellAttributes.Blink));
                    break;
                case 27:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithoutAttribute(CellAttributes.Reverse));
                    break;
                case 28:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithoutAttribute(CellAttributes.Hidden));
                    break;
                case 29:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithoutAttribute(CellAttributes.Strikethrough));
                    break;

                // Standard foreground colors (30-37)
                case 30:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.Black));
                    break;
                case 31:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.DarkRed));
                    break;
                case 32:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.DarkGreen));
                    break;
                case 33:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.DarkYellow));
                    break;
                case 34:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.DarkBlue));
                    break;
                case 35:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.DarkMagenta));
                    break;
                case 36:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.DarkCyan));
                    break;
                case 37:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.Gray));
                    break;
                case 39: // Default foreground
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(null));
                    break;

                // Standard background colors (40-47)
                case 40:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.Black));
                    break;
                case 41:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.DarkRed));
                    break;
                case 42:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.DarkGreen));
                    break;
                case 43:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.DarkYellow));
                    break;
                case 44:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.DarkBlue));
                    break;
                case 45:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.DarkMagenta));
                    break;
                case 46:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.DarkCyan));
                    break;
                case 47:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.Gray));
                    break;
                case 49: // Default background
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(null));
                    break;

                // Bright foreground colors (90-97)
                case 90:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.DarkGray));
                    break;
                case 91:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.Red));
                    break;
                case 92:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.Green));
                    break;
                case 93:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.Yellow));
                    break;
                case 94:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.Blue));
                    break;
                case 95:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.Magenta));
                    break;
                case 96:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.Cyan));
                    break;
                case 97:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithForeground(ConsoleColor.White));
                    break;

                // Bright background colors (100-107)
                case 100:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.DarkGray));
                    break;
                case 101:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.Red));
                    break;
                case 102:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.Green));
                    break;
                case 103:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.Yellow));
                    break;
                case 104:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.Blue));
                    break;
                case 105:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.Magenta));
                    break;
                case 106:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.Cyan));
                    break;
                case 107:
                    buffer.ApplyStyle(buffer.CurrentStyle.WithBackground(ConsoleColor.White));
                    break;
            }
        }
    }
}
