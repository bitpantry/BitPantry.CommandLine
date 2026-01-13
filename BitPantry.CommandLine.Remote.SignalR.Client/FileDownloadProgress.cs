namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Progress information for download operations (client-side).
    /// Used for progress callbacks during file downloads.
    /// Note: Download progress is calculated client-side from HTTP Content-Length
    /// header and stream reading, unlike uploads which use SignalR RPC messages.
    /// </summary>
    public record FileDownloadProgress(
        long TotalRead, 
        long TotalSize)
    {
        /// <summary>
        /// Gets the percentage complete (0-100).
        /// Returns 0 if TotalSize is 0 to avoid division by zero.
        /// </summary>
        public double PercentComplete => TotalSize > 0 ? (double)TotalRead / TotalSize * 100 : 0;
    }
}
