using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    public class FileTransferEndpointService
    {
        private ILogger<FileTransferEndpointService> _logger;
        private IHubContext<CommandLineHub> _cliHubCtx;
        private IFileService _fileSvc;

        public FileTransferEndpointService(ILogger<FileTransferEndpointService> logger, IHubContext<CommandLineHub> cliHubCtx, IFileService fileSvc)
        {
            _logger = logger;
            _cliHubCtx = cliHubCtx;
            _fileSvc = fileSvc;
        }

        public async Task<IResult> UploadFile(Stream fileStream, string toFilePath, string connectionId, string correlationId)
        {
            _logger.LogDebug("File upload posted :: toFilePath={ToFilePath}; connectionId={ConnectionId}; correlationId={CorrelationId}", toFilePath, connectionId, correlationId);

            // get the client

            var client = _cliHubCtx.Clients.Client(connectionId);

            // delete existing file

            if(_fileSvc.Exists(toFilePath))
                _fileSvc.Delete(toFilePath);

            // upload the file

            var buffer = new byte[81920];
            var totalRead = 0;

            var bytesRead = 0;

            using var toStream = _fileSvc.OpenWrite(toFilePath);

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalRead += bytesRead;

                await toStream.WriteAsync(buffer, 0, bytesRead);

                if(!string.IsNullOrEmpty(correlationId))
                    await client.SendAsync(SignalRMethodNames.ReceiveMessage, new FileUploadProgressMessage(totalRead) { CorrelationId = correlationId });
            }

            // return

            return Results.Ok();
        }
    }
}
