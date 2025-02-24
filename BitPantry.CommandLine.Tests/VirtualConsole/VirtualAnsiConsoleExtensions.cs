using Spectre.Console;

namespace BitPantry.CommandLine.Tests.VirtualConsole
{
    public static class VirtualAnsiConsoleExtensions
    {
        /// <summary>
        /// Sets the console's color system.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="colors">The color system to use.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static VirtualAnsiConsole Colors(this VirtualAnsiConsole console, ColorSystem colors)
        {
            console.Profile.Capabilities.ColorSystem = colors;
            return console;
        }

        /// <summary>
        /// Sets whether or not ANSI is supported.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="enable">Whether or not VT/ANSI control codes are supported.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static VirtualAnsiConsole SupportsAnsi(this VirtualAnsiConsole console, bool enable)
        {
            console.Profile.Capabilities.Ansi = enable;
            return console;
        }

        /// <summary>
        /// Makes the console interactive.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static VirtualAnsiConsole Interactive(this VirtualAnsiConsole console)
        {
            console.Profile.Capabilities.Interactive = true;
            return console;
        }

        /// <summary>
        /// Sets the console width.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="width">The console width.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static VirtualAnsiConsole Width(this VirtualAnsiConsole console, int width)
        {
            console.Profile.Width = width;
            return console;
        }

        /// <summary>
        /// Sets the console height.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="width">The console height.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static VirtualAnsiConsole Height(this VirtualAnsiConsole console, int width)
        {
            console.Profile.Height = width;
            return console;
        }

        /// <summary>
        /// Sets the console size.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="size">The console size.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static VirtualAnsiConsole Size(this VirtualAnsiConsole console, Size size)
        {
            console.Profile.Width = size.Width;
            console.Profile.Height = size.Height;
            return console;
        }

        /// <summary>
        /// Turns on emitting of VT/ANSI sequences.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static VirtualAnsiConsole EmitAnsiSequences(this VirtualAnsiConsole console)
        {
            console.SetCursor(null);
            console.EmitAnsiSequences = true;
            return console;
        }
    }
}
