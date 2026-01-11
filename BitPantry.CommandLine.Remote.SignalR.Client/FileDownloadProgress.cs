namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Progress information for download operations (client-side).
    /// Mirrors FileUploadProgress for consistent patterns.
    /// </summary>
    public record FileDownloadProgress(
        long TotalRead, 
        long TotalSize, 
        string CorrelationId,
        string Error = null)
    {
        /// <summary>
        /// Gets the percentage complete (0-100).
        /// Returns 0 if TotalSize is 0 to avoid division by zero.
        /// </summary>
        public double PercentComplete => TotalSize > 0 ? (double)TotalRead / TotalSize * 100 : 0;
    }
}
