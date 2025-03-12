using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    public interface IFileStorageService
    {
        Task AppendBuffer(string relativeFilePath, byte[] buffer, int count, CancellationToken token = default);
        Task DeleteFile(string toFilePath);
    }
}
