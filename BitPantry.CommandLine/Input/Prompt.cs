using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitPantry.CommandLine.Input
{
    public class Prompt
    {
        private readonly object _lock = new();

        private Dictionary<string, string> _values = new Dictionary<string, string>();
        private string _promptFormat = string.Empty;

        public Dictionary<string, string> Values
        {
            get { lock (_lock) { return _values; } }
            set { lock (_lock) { _values = value; } }
        }

        public string PromptFormat
        {
            get { lock (_lock) { return _promptFormat; } }
            set { lock (_lock) { _promptFormat = value; } }
        }

        public Prompt()
        {
            Reset();
        }

        public void Reset()
        {
            lock (_lock)
            {
                Values.Clear();
                Values.Add("terminator", "$");
                PromptFormat = "{terminator} ";
            }
        }

        public int GetPromptLength()
            => new Text(GetFormattedPrompt()).Length;

        public void Write(IAnsiConsole console)
        {
            console.Markup(GetFormattedPrompt());
        }

        private string GetFormattedPrompt()
            => TokenReplacement.ReplaceTokens(PromptFormat, Values);
    }

}
