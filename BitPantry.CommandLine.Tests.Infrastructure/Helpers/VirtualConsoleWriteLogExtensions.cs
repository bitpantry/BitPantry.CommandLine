using BitPantry.VirtualConsole.Testing;

namespace BitPantry.CommandLine.Tests.Infrastructure.Helpers
{
    public static class VirtualConsoleWriteLogExtensions
    {
        // Spectre.Console progress bar characters
        private const char FilledBar = '━';   // U+2501 - Heavy horizontal (filled portion)
        private const char UnfilledBar = '─'; // U+2500 - Light horizontal (unfilled portion)
        
        /// <summary>
        /// Checks if a Spectre.Console progress bar was visible in the write log.
        /// Detects progress bar characters (filled ━ or unfilled ─) combined with
        /// ANSI escape sequences and optionally transfer speed indicators (/s suffix).
        /// </summary>
        public static bool WasSpectreProgressBarVisible(this VirtualConsoleWriteLog log)
        {
            var contents = log.Contents;
            
            // Must have progress bar characters (filled or unfilled)
            var hasBarChars = contents.Contains(FilledBar) || contents.Contains(UnfilledBar);
            if (!hasBarChars)
                return false;
            
            // Must also have ANSI escape sequences (Spectre always uses these for styling)
            var hasAnsiEscape = contents.Contains("\x1b[");
            if (!hasAnsiEscape)
                return false;
            
            // Additional confidence: percentage or transfer speed indicators
            var hasPercentage = contents.Contains('%');
            var hasTransferSpeed = contents.Contains("/s") && 
                (contents.Contains("MB") || contents.Contains("KB") || 
                 contents.Contains("GB") || contents.Contains("B/s"));
            
            return hasPercentage || hasTransferSpeed;
        }
    }
}
