using BitPantry.VirtualConsole.Testing;

namespace BitPantry.CommandLine.Tests.Service
{
    public class TestConsoleService : IConsoleService
    {
        private VirtualConsoleAnsiAdapter _console;

        public TestConsoleService(VirtualConsoleAnsiAdapter console)
        {
            _console = console;
        }

        public CursorPosition GetCursorPosition()
        {
            var vc = _console.VirtualConsole;
            return new CursorPosition(vc.CursorRow, vc.CursorColumn);
        }
    }
}
