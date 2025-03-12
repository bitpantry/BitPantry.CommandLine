namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public record FileUploadProgress(int TotalRead, string Error = null) { }
}
