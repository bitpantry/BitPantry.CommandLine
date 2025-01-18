//using Spectre.Console;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace BitPantry.CommandLine
//{
//    public class CommandLineApplicationRepl
//    {
//        private readonly CommandLineApplication _app;

//        public string Prompt { get; private set; } = "$ ";

//        public CommandLineApplicationRepl(CommandLineApplication app) 
//        {
//            _app = app;
//        }

//        public async Task Run()
//        {
//            do
//            {
//                try
//                {
//                    AnsiConsole.Write(Prompt);
//                    var input = System.Console.ReadLine();

//                    if (System.IO.File.Exists(input))
//                        await ExecuteScript(input, app);
//                    else
//                        await app.Run(input);
//                }
//                catch (Exception ex)
//                {
//                    System.Console.ForegroundColor = ConsoleColor.Red;
//                    System.Console.WriteLine($"An unhandled exception occured :: {ex.Message}");
//                    System.Console.ResetColor();
//                }
//            } while (true);
//        }
//    }
//}
