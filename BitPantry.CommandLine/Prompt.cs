using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitPantry.CommandLine
{
    public static class Prompt
    {
        private static readonly object _lock = new();

        private static Dictionary<string, string> _values = new Dictionary<string, string>();
        private static string _promptFormat = string.Empty;

        public static Dictionary<string, string> Values
        {
            get { lock (_lock) { return _values; } }
            set { lock (_lock) { _values = value; } }
        }

        public static string PromptFormat
        {
            get { lock (_lock) { return _promptFormat; } }
            set { lock (_lock) { _promptFormat = value; } }
        }

        static Prompt()
        {
            Reset();
        }

        public static void Reset()
        {
            lock (_lock)
            {
                Values.Clear();
                Values.Add("terminator", "$");
                PromptFormat = "{terminator} ";
            }
        }

        public static int GetPromptLength()
            => new Text(GetFormattedPrompt()).Length;

        public static void Write(IAnsiConsole console)
        {
            console.Markup(GetFormattedPrompt());
        }

        private static string GetFormattedPrompt()
            => TokenReplacement.ReplaceTokens(PromptFormat, Values);
    }

}
