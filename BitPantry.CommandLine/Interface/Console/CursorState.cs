using System;

namespace BitPantry.CommandLine.Interface.Console
{
    class CursorState : IDisposable
    {
        private readonly bool _original;

        public CursorState()
        {
            try { _original = System.Console.CursorVisible; }
            catch
            {
                // some platforms throw System.PlatformNotSupportedException
                // Assume the cursor should be shown
                _original = true;
            }

            TrySetVisible(true);
        }

        private void TrySetVisible(bool visible)
        {
            try { System.Console.CursorVisible = visible; }
            catch { /* setting cursor may fail if output is piped or permission is denied. */ }
        }

        public void Dispose()
        {
            TrySetVisible(_original);
        }
    }

}
