

namespace BitPantry.CommandLine.Tests.AnsiConsole
{
    public class TestConsoleService : IConsoleService
    {
        private TestAnsiConsole _console;

        public TestConsoleService(TestAnsiConsole console)
        {
            _console = console;
        }

        public CursorPosition GetCursorPosition()
        {
            var pos = _console.GetCursorPosition();
            return new CursorPosition(pos.Line, pos.Column);
        }
    }
}
