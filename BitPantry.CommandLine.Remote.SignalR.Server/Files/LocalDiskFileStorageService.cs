using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    public class LocalDiskFileStorageService : IFileStorageService
    {
        private const string _localFileStorageRoot = "./local-file-storage";

        public async Task AppendBuffer(string relativeFilePath, byte[] buffer, int count, CancellationToken token = default)
        {
            string filePath = Path.Combine(_localFileStorageRoot, relativeFilePath);
            var directoryPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            using var stream = new FileStream(filePath, FileMode.Append);
            await stream.WriteAsync(buffer, 0, count, token);
        }

        public Task DeleteFile(string toFilePath)
        {
            string filePath = Path.Combine(_localFileStorageRoot, toFilePath);
            if(File.Exists(filePath))
                File.Delete(filePath);

            return Task.CompletedTask;
        }
    }
}
