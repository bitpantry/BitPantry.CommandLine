using BitPantry.CommandLine.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace BitPantry.CommandLine.Interface.Console
{
    public class ConsoleWriter : Writer
    {
        private static readonly object _locker = new object();

        public ConsoleColor ForegroundColor { get; private set; }
        public ConsoleColor BackgroundColor { get; private set; }

        public ConsoleWriter(ConsoleColor backgroundColor, ConsoleColor foregroundColor)
        {
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
        }


        protected override void OnWrite(string str)
        {
            lock (_locker)
            {
                // split multiline output string

                string splitterToken = Guid.NewGuid().ToString().Substring(0, 5);
                str = str.Replace("|", splitterToken);
                str = str.Replace(Environment.NewLine, "|");

                List<string> lines = new List<string>(str.Split('|'));

                // apply formatting

                for (int i = 0; i < lines.Count - 1; i++)
                    lines[i] = string.Format("{0}{1}", lines[i].Replace(splitterToken, "|"), Environment.NewLine);

                // remove trailing empty line (not a line break)

                if (string.IsNullOrEmpty(lines.Last())) lines.Remove(lines.Last());

                var currentBackgroundColor = System.Console.BackgroundColor;
                var currentForegroundcolor = System.Console.ForegroundColor;

                System.Console.BackgroundColor = BackgroundColor;
                System.Console.ForegroundColor = ForegroundColor;

                foreach (var ln in lines)
                    System.Console.Write(ln);

                System.Console.BackgroundColor = currentBackgroundColor;
                System.Console.ForegroundColor = currentForegroundcolor;
            }
        }
    }
}
