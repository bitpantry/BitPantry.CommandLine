using System;

namespace BitPantry.VirtualConsole
{
    /// <summary>
    /// Represents a cursor position on the virtual screen.
    /// Uses 0-based row and column indices.
    /// </summary>
    public readonly struct CursorPosition : IEquatable<CursorPosition>
    {
        /// <summary>
        /// The row (0-based).
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// The column (0-based).
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Creates a cursor position at the specified row and column.
        /// </summary>
        public CursorPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        /// <inheritdoc/>
        public bool Equals(CursorPosition other)
        {
            return Row == other.Row && Column == other.Column;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is CursorPosition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return Row * 31 + Column;
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(CursorPosition left, CursorPosition right) => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(CursorPosition left, CursorPosition right) => !left.Equals(right);

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"CursorPosition(Row={Row}, Column={Column})";
        }
    }
}
