namespace BitPantry.CommandLine.Remote.SignalR
{
    /// <summary>
    /// Response from file upload endpoint indicating the outcome.
    /// </summary>
    /// <param name="Status">Outcome status: "uploaded", "skipped", or "error".</param>
    /// <param name="Reason">Explanation when status is "skipped" or "error".</param>
    /// <param name="BytesWritten">Number of bytes written (for "uploaded" status).</param>
    public record FileUploadResponse(string Status, string Reason = null, long? BytesWritten = null);
}
