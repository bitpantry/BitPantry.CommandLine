namespace BitPantry.CommandLine.Client
{
    /// <summary>
    /// Reports progress during a file transfer operation.
    /// </summary>
    /// <param name="BytesTransferred">The number of bytes transferred so far.</param>
    /// <param name="TotalBytes">The total number of bytes to transfer, or null if unknown.</param>
    public record FileTransferProgress(long BytesTransferred, long? TotalBytes);
}
