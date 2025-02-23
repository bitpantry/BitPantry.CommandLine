using Spectre.Console;
using System;

namespace BitPantry.CommandLine
{
    public static class Prompt
    {
        private static readonly object _lock = new();

        private static string _terminator;
        private static string _server;
        private static Func<PromptValues, string> _getPromptFunc;

        public static string ServerName
        {
            get { lock (_lock) { return _server ?? string.Empty; } }
            set { lock (_lock) _server = value; }
        }

        public static string Terminator
        {
            get { lock (_lock) { return _terminator ?? string.Empty; } }
            set { lock (_lock) _terminator = value; }
        }

        public static Func<PromptValues, string> PromptFunc
        {
            get { lock (_lock) { return _getPromptFunc ?? (_ => string.Empty); } }
            set { lock (_lock) _getPromptFunc = value; }
        }

        static Prompt()
        {
            Reset();
        }

        public static void Reset()
        {
            Terminator = "$ ";
            ServerName = string.Empty;
            PromptFunc = values => string.Empty;
        }

        public static int GetPromptLength()
            => new Text(GetFormattedPrompt()).Length;

        public static void Write(IAnsiConsole console)
        {
            console.Markup(GetFormattedPrompt());
        }

        private static string GetFormattedPrompt()
            => PromptFunc(new PromptValues(_terminator, _server));


    }

    public record PromptValues(string Terminator, string Server) { }
}
