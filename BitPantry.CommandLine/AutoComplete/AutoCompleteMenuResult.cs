namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Result codes from AutoCompleteMenuController.HandleKey indicating what action occurred.
    /// </summary>
    public enum AutoCompleteMenuResult
    {
        /// <summary>
        /// The key was not handled by the menu controller.
        /// The caller should process the key normally.
        /// </summary>
        NotHandled,

        /// <summary>
        /// The key was handled (e.g., navigation).
        /// The caller should update the menu display.
        /// </summary>
        Handled,

        /// <summary>
        /// An option was selected (Tab or Enter pressed).
        /// The caller should apply the selection and close the menu.
        /// </summary>
        Selected,

        /// <summary>
        /// The menu was dismissed (Escape pressed).
        /// The caller should close the menu without applying selection.
        /// </summary>
        Dismissed
    }
}
