using System;

namespace BitPantry.CommandLine.Interface.Console
{
    using System;

    class CursorState : IDisposable
    {
        private readonly bool _original;
        private readonly bool _isSupported;

        public CursorState()
        {
            try
            {
                // Attempt to get the original cursor visibility state
                // I know this may not be supported and am handling it further down

#pragma warning disable CA1416 // Validate platform compatibility
                _original = Console.CursorVisible;
#pragma warning restore CA1416 // Validate platform compatibility

                _isSupported = true;
            }
            catch (PlatformNotSupportedException)
            {
                // Some platforms may not support Console.CursorVisible
                _original = true; // Assume the cursor should be shown
                _isSupported = false;
            }

            // Try to set the cursor visibility to true
            TrySetVisible(true);
        }

        private void TrySetVisible(bool visible)
        {
            try
            {
                if (_isSupported)
                {
                    Console.CursorVisible = visible;
                }
                else
                {
                    // If the platform doesn't support setting Console.CursorVisible, use ANSI codes
                    if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                    {
                        if (visible)
                            Console.Write("\u001B[?25h"); // Show cursor
                        else
                            Console.Write("\u001B[?25l"); // Hide cursor
                    }
                    // Windows 10+ will also handle ANSI codes, but CursorVisible should be preferred when supported
                }
            }
            catch
            {
                // Setting cursor visibility may fail, for example, if the output is piped or permissions are restricted
            }
        }

        public void Dispose()
        {
            // Restore the original cursor visibility state
            TrySetVisible(_original);
        }
    }


}
