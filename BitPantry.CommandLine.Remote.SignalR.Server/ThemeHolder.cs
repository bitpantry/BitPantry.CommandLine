using BitPantry.CommandLine.AutoComplete;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// Scoped service that holds the client's <see cref="Theme"/> for the current hub invocation.
    /// The hub sets this at the entry point of each call from <see cref="Hub.Context.Items"/>,
    /// and the DI container resolves <see cref="Theme"/> from this holder.
    /// Follows the same pattern as <see cref="SignalRRpcScope"/>.
    /// </summary>
    public class ThemeHolder
    {
        /// <summary>
        /// The theme for the current hub invocation scope. Defaults to a standard theme
        /// until set by the hub from the connection's context.
        /// </summary>
        public Theme Theme { get; private set; } = new Theme();

        /// <summary>
        /// Sets the theme for the current scope.
        /// </summary>
        /// <param name="theme">The client's theme.</param>
        public void SetTheme(Theme theme)
        {
            ArgumentNullException.ThrowIfNull(theme);
            Theme = theme;
        }
    }
}
