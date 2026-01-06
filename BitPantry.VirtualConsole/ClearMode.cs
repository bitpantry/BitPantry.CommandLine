namespace BitPantry.VirtualConsole
{
    /// <summary>
    /// Mode for erase operations (ED and EL sequences).
    /// </summary>
    public enum ClearMode
    {
        /// <summary>Erase from cursor to end (default).</summary>
        ToEnd = 0,
        /// <summary>Erase from beginning to cursor.</summary>
        ToBeginning = 1,
        /// <summary>Erase entire screen/line.</summary>
        All = 2
    }
}
