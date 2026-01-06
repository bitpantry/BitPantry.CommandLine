using System;

namespace BitPantry.VirtualConsole
{
    /// <summary>
    /// Represents the visual style of a screen cell (colors and attributes).
    /// Immutable value type with With* methods for modification.
    /// Supports basic ConsoleColor, 256-color palette, and 24-bit TrueColor.
    /// </summary>
    public readonly struct CellStyle : IEquatable<CellStyle>
    {
        /// <summary>
        /// Default style with no colors and no attributes.
        /// </summary>
        public static readonly CellStyle Default = new CellStyle(null, null, CellAttributes.None, null, null, null, null);

        /// <summary>
        /// Foreground color (null = default terminal color).
        /// </summary>
        public ConsoleColor? ForegroundColor { get; }

        /// <summary>
        /// Background color (null = default terminal color).
        /// </summary>
        public ConsoleColor? BackgroundColor { get; }

        /// <summary>
        /// Text attributes (bold, italic, underline, etc.).
        /// </summary>
        public CellAttributes Attributes { get; }

        /// <summary>
        /// Foreground 256-color index (0-255, null if not using 256-color).
        /// </summary>
        public byte? Foreground256 { get; }

        /// <summary>
        /// Background 256-color index (0-255, null if not using 256-color).
        /// </summary>
        public byte? Background256 { get; }

        /// <summary>
        /// Foreground RGB color (24-bit TrueColor, null if not using TrueColor).
        /// </summary>
        public (byte R, byte G, byte B)? ForegroundRgb { get; }

        /// <summary>
        /// Background RGB color (24-bit TrueColor, null if not using TrueColor).
        /// </summary>
        public (byte R, byte G, byte B)? BackgroundRgb { get; }

        /// <summary>
        /// Creates a new CellStyle with the specified properties.
        /// </summary>
        public CellStyle(ConsoleColor? foreground, ConsoleColor? background, CellAttributes attributes)
            : this(foreground, background, attributes, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a new CellStyle with extended color support.
        /// </summary>
        public CellStyle(
            ConsoleColor? foreground, 
            ConsoleColor? background, 
            CellAttributes attributes,
            byte? foreground256,
            byte? background256,
            (byte R, byte G, byte B)? foregroundRgb,
            (byte R, byte G, byte B)? backgroundRgb)
        {
            ForegroundColor = foreground;
            BackgroundColor = background;
            Attributes = attributes;
            Foreground256 = foreground256;
            Background256 = background256;
            ForegroundRgb = foregroundRgb;
            BackgroundRgb = backgroundRgb;
        }

        /// <summary>
        /// Returns a new CellStyle with the specified foreground color.
        /// </summary>
        public CellStyle WithForeground(ConsoleColor? color)
        {
            return new CellStyle(color, BackgroundColor, Attributes, null, Background256, null, BackgroundRgb);
        }

        /// <summary>
        /// Returns a new CellStyle with the specified background color.
        /// </summary>
        public CellStyle WithBackground(ConsoleColor? color)
        {
            return new CellStyle(ForegroundColor, color, Attributes, Foreground256, null, ForegroundRgb, null);
        }

        /// <summary>
        /// Returns a new CellStyle with the specified attribute added.
        /// </summary>
        public CellStyle WithAttribute(CellAttributes attribute)
        {
            return new CellStyle(ForegroundColor, BackgroundColor, Attributes | attribute, Foreground256, Background256, ForegroundRgb, BackgroundRgb);
        }

        /// <summary>
        /// Returns a new CellStyle with the specified attribute removed.
        /// </summary>
        public CellStyle WithoutAttribute(CellAttributes attribute)
        {
            return new CellStyle(ForegroundColor, BackgroundColor, Attributes & ~attribute, Foreground256, Background256, ForegroundRgb, BackgroundRgb);
        }

        /// <summary>
        /// Returns a new CellStyle with the specified 256-color foreground.
        /// </summary>
        public CellStyle WithForeground256(byte colorIndex)
        {
            return new CellStyle(null, BackgroundColor, Attributes, colorIndex, Background256, null, BackgroundRgb);
        }

        /// <summary>
        /// Returns a new CellStyle with the specified 256-color background.
        /// </summary>
        public CellStyle WithBackground256(byte colorIndex)
        {
            return new CellStyle(ForegroundColor, null, Attributes, Foreground256, colorIndex, ForegroundRgb, null);
        }

        /// <summary>
        /// Returns a new CellStyle with the specified TrueColor foreground.
        /// </summary>
        public CellStyle WithForegroundRgb(byte r, byte g, byte b)
        {
            return new CellStyle(null, BackgroundColor, Attributes, null, Background256, (r, g, b), BackgroundRgb);
        }

        /// <summary>
        /// Returns a new CellStyle with the specified TrueColor background.
        /// </summary>
        public CellStyle WithBackgroundRgb(byte r, byte g, byte b)
        {
            return new CellStyle(ForegroundColor, null, Attributes, Foreground256, null, ForegroundRgb, (r, g, b));
        }

        /// <inheritdoc/>
        public bool Equals(CellStyle other)
        {
            return ForegroundColor == other.ForegroundColor
                && BackgroundColor == other.BackgroundColor
                && Attributes == other.Attributes
                && Foreground256 == other.Foreground256
                && Background256 == other.Background256
                && ForegroundRgb == other.ForegroundRgb
                && BackgroundRgb == other.BackgroundRgb;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is CellStyle other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (ForegroundColor?.GetHashCode() ?? 0);
                hash = hash * 31 + (BackgroundColor?.GetHashCode() ?? 0);
                hash = hash * 31 + Attributes.GetHashCode();
                hash = hash * 31 + (Foreground256?.GetHashCode() ?? 0);
                hash = hash * 31 + (Background256?.GetHashCode() ?? 0);
                hash = hash * 31 + (ForegroundRgb?.GetHashCode() ?? 0);
                hash = hash * 31 + (BackgroundRgb?.GetHashCode() ?? 0);
                return hash;
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(CellStyle left, CellStyle right) => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(CellStyle left, CellStyle right) => !left.Equals(right);

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"CellStyle(FG={ForegroundColor?.ToString() ?? "default"}, BG={BackgroundColor?.ToString() ?? "default"}, Attrs={Attributes})";
        }
    }
}
