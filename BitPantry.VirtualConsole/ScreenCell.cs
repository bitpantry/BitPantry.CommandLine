using System;

namespace BitPantry.VirtualConsole
{
    /// <summary>
    /// Represents a single cell on the virtual screen.
    /// Contains a character and its associated style.
    /// </summary>
    public readonly struct ScreenCell : IEquatable<ScreenCell>
    {
        /// <summary>
        /// The character displayed at this cell position.
        /// Default is a space character.
        /// </summary>
        public char Character { get; }

        /// <summary>
        /// The visual style of this cell.
        /// </summary>
        public CellStyle Style { get; }

        /// <summary>
        /// Creates a default cell (space with default style).
        /// </summary>
        public ScreenCell()
        {
            Character = ' ';
            Style = CellStyle.Default;
        }

        /// <summary>
        /// Creates a cell with the specified character and style.
        /// </summary>
        public ScreenCell(char character, CellStyle style)
        {
            Character = character;
            Style = style;
        }

        /// <inheritdoc/>
        public bool Equals(ScreenCell other)
        {
            return Character == other.Character && Style.Equals(other.Style);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is ScreenCell other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return Character.GetHashCode() * 31 + Style.GetHashCode();
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(ScreenCell left, ScreenCell right) => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(ScreenCell left, ScreenCell right) => !left.Equals(right);

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ScreenCell('{Character}', {Style})";
        }
    }
}
