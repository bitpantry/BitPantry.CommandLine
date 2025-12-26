using Spectre.Console;

namespace BitPantry.CommandLine.Input
{
    /// <summary>
    /// Renders the complete REPL prompt.
    /// </summary>
    public interface IPrompt
    {
        /// <summary>
        /// Renders the complete prompt string.
        /// </summary>
        /// <returns>The prompt string including suffix (e.g., "> ").</returns>
        string Render();

        /// <summary>
        /// Gets the length of the rendered prompt (for cursor positioning).
        /// </summary>
        int GetPromptLength();

        /// <summary>
        /// Writes the prompt to the console.
        /// </summary>
        void Write(IAnsiConsole console);
    }
}
