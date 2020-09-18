using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Interface.Console
{
    static partial class MaskedInput
    {
        private const char Backspace = '\b';

        /// <summary>
        /// Gets masked input from the console.
        /// </summary>
        /// <returns>The password as plaintext. Can be null or empty.</returns>
        public static string Get()
        {
            var resp = new StringBuilder();

            foreach (var key in ReadObfuscatedLine())
            {
                switch (key)
                {
                    case Backspace:
                        resp.Remove(resp.Length - 1, 1);
                        break;
                    default:
                        resp.Append(key);
                        break;
                }
            }

            return resp.ToString();
        }


        /// <summary>
        /// Base implementation of GetPassword and GetPasswordAsString. Prompts the user for
        /// a password and yields each key as the user inputs. Password is masked as input. Pressing Escape will reset the password
        /// by flushing the stream with backspace keys.
        /// </summary>
        /// <returns>A stream of characters as input by the user including Backspace for deletions.</returns>
        private static IEnumerable<char> ReadObfuscatedLine()
        {
            const string whiteOut = "\b \b";
            const ConsoleModifiers IgnoredModifiersMask = ConsoleModifiers.Alt | ConsoleModifiers.Control;
            var readChars = 0;
            ConsoleKeyInfo key;

            do
            {
                using (ShowCursor())
                    key = System.Console.ReadKey(intercept: true);

                if ((key.Modifiers & IgnoredModifiersMask) != 0)
                    continue;

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        System.Console.WriteLine();
                        break;
                    case ConsoleKey.Backspace:
                        if (readChars > 0)
                        {
                            System.Console.Write(whiteOut);
                            --readChars;
                            yield return Backspace;
                        }
                        break;
                    case ConsoleKey.Escape:
                        // Reset the password
                        while (readChars > 0)
                        {
                            System.Console.Write(whiteOut);
                            yield return Backspace;
                            --readChars;
                        }
                        break;
                    default:
                        readChars += 1;
                        System.Console.Write('*');
                        yield return key.KeyChar;
                        break;
                }
            }
            while (key.Key != ConsoleKey.Enter);
        }

        private static IDisposable ShowCursor() => new CursorState();
    }
}
