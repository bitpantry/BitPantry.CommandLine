namespace BitPantry.CommandLine.Remote.SignalR
{
    /// <summary>
    /// Response payload for batch file existence check.
    /// </summary>
    /// <param name="Exists">Dictionary mapping filename to existence status.</param>
    public record FilesExistResponse(Dictionary<string, bool> Exists);
}
