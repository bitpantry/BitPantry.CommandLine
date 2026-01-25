namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Represents the current state of the autocomplete controller.
    /// </summary>
    public enum AutoCompleteState
    {
        /// <summary>
        /// No autocomplete active. Waiting for trigger.
        /// </summary>
        Idle,

        /// <summary>
        /// Ghost text is displayed as a preview of the first/current option.
        /// User can accept with Tab/Enter/RightArrow, cancel with Escape, or type to filter.
        /// </summary>
        GhostText,

        /// <summary>
        /// Menu is open displaying multiple options.
        /// User can navigate with Up/Down, accept with Enter, cancel with Escape, or type to filter.
        /// </summary>
        MenuOpen
    }
}
