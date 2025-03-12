using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Client;

namespace BitPantry.CommandLine
{
    public static class CommandBaseExtensions_FileTransfer
    {
        internal static FileTransferService FileTransferService { get; set; }

        public static async Task UploadFile(
            this CommandBase cmd, 
            string filePath, 
            string remoteFilePath, 
            Func<FileUploadProgress, Task> progressUpdateFunc = null, 
            CancellationToken token = default)
        {
            await FileTransferService.UploadFile(filePath, remoteFilePath, progressUpdateFunc, token);
        }
    }
}
