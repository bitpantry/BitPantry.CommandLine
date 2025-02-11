using Spectre.Console;

namespace BitPantry.CommandLine
{
    public static class Prompt
    {
        private static readonly object _lock = new();

        private static string _format;
        private static string _terminator;
        private static string _server;

        public static string ServerName
        {
            get => _server ?? string.Empty;
            set
            {
                lock (_lock)
                    _server = value;
            }
        }

        public static string Terminator
        {
            get => _terminator ?? string.Empty;
            set
            {
                lock (_lock)
                    _terminator = value;
            }
        }

        public static string Format
        {
            get => _format ?? string.Empty;
            set
            {
                lock (_lock)
                    _format = value;
            }
        }

        static Prompt()
        {
            Reset();
        }

        public static void Reset()
        {
            Terminator = "$ ";
            ServerName = string.Empty;
            Format = "[green]{0}[/]{1}";
        }

        public static int GetPromptLength()
            => new Text(GetFormattedPrompt()).Length;

        public static void Write(IAnsiConsole console)
        {
            console.Markup(GetFormattedPrompt());
        }

        private static string GetFormattedPrompt()
            => string.Format(Format,
                ServerName.EscapeMarkup(),
                Terminator);


    }
}
