using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.VirtualConsole
{
    /// <summary>
    /// Provides convenient access to a single row of the screen buffer.
    /// </summary>
    public class ScreenRow
    {
        private readonly ScreenBuffer _buffer;

        /// <summary>
        /// The row index (0-based).
        /// </summary>
        public int RowIndex { get; }

        /// <summary>
        /// The number of columns in this row.
        /// </summary>
        public int Length => _buffer.Width;

        internal ScreenRow(ScreenBuffer buffer, int rowIndex)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            RowIndex = rowIndex;
        }

        /// <summary>
        /// Gets the cell at the specified column.
        /// </summary>
        /// <param name="column">The column index (0-based).</param>
        /// <returns>The cell at the specified column.</returns>
        public ScreenCell GetCell(int column)
        {
            return _buffer.GetCell(RowIndex, column);
        }

        /// <summary>
        /// Gets the text content of this row (characters only, no styling).
        /// </summary>
        /// <returns>The row text.</returns>
        public string GetText()
        {
            var sb = new StringBuilder(Length);
            for (int col = 0; col < Length; col++)
            {
                sb.Append(_buffer.GetCell(RowIndex, col).Character);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Enumerates all cells in this row.
        /// </summary>
        /// <returns>An enumerable of all cells in the row.</returns>
        public IEnumerable<ScreenCell> GetCells()
        {
            for (int col = 0; col < Length; col++)
            {
                yield return _buffer.GetCell(RowIndex, col);
            }
        }

        /// <summary>
        /// Gets a subset of cells from this row.
        /// </summary>
        /// <param name="startColumn">The starting column (0-based).</param>
        /// <param name="length">The number of cells to retrieve.</param>
        /// <returns>A list of cells from the specified range.</returns>
        public IReadOnlyList<ScreenCell> GetCells(int startColumn, int length)
        {
            var result = new List<ScreenCell>();
            int endColumn = Math.Min(startColumn + length, Length);
            for (int col = startColumn; col < endColumn; col++)
            {
                result.Add(_buffer.GetCell(RowIndex, col));
            }
            return result;
        }
    }
}
