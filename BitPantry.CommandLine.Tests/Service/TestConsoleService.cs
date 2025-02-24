using BitPantry.CommandLine.Tests.VirtualConsole;

namespace BitPantry.CommandLine.Tests.Service
{
    public class TestConsoleService : IConsoleService
    {
        private VirtualAnsiConsole _console;

        public TestConsoleService(VirtualAnsiConsole console)
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
