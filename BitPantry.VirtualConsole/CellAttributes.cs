using System;

namespace BitPantry.VirtualConsole
{
    /// <summary>
    /// Bitflag enum for text attributes.
    /// </summary>
    [Flags]
    public enum CellAttributes
    {
        /// <summary>No attributes.</summary>
        None = 0,
        /// <summary>Bold text (SGR 1).</summary>
        Bold = 1,
        /// <summary>Dim/faint text (SGR 2).</summary>
        Dim = 2,
        /// <summary>Italic text (SGR 3).</summary>
        Italic = 4,
        /// <summary>Underlined text (SGR 4).</summary>
        Underline = 8,
        /// <summary>Blinking text (SGR 5).</summary>
        Blink = 16,
        /// <summary>Reversed/inverted colors (SGR 7).</summary>
        Reverse = 32,
        /// <summary>Hidden text (SGR 8).</summary>
        Hidden = 64,
        /// <summary>Strikethrough text (SGR 9).</summary>
        Strikethrough = 128
    }
}
