using System;

namespace BitPantry.CommandLine.AutoComplete.Rendering
{
    /// <summary>
    /// Tracks maximum rendered dimensions to support the "inflate and pad" pattern.
    /// Key behavior: Inflate() only grows dimensions, never shrinks them.
    /// This prevents phantom lines when menu content shrinks.
    /// </summary>
    public readonly struct SegmentShape
    {
        /// <summary>
        /// Maximum width in cells
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Maximum height in lines
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Creates a new SegmentShape with specified dimensions
        /// </summary>
        public SegmentShape(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Returns a new shape with max dimensions of this and other.
        /// This is the "inflate" pattern - dimensions only grow, never shrink.
        /// </summary>
        /// <param name="other">The other shape to compare against</param>
        /// <returns>A new SegmentShape with the maximum of each dimension</returns>
        public SegmentShape Inflate(SegmentShape other)
        {
            return new SegmentShape(
                Math.Max(Width, other.Width),
                Math.Max(Height, other.Height));
        }

        /// <summary>
        /// Returns a string representation for debugging
        /// </summary>
        public override string ToString() => $"SegmentShape({Width}x{Height})";
    }
}
