namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public record FileUploadProgress(long TotalRead, string Error = null) { }
}
