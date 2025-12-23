using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    public class FileTransferEndpointService
    {
        private readonly ILogger<FileTransferEndpointService> _logger;
        private readonly IHubContext<CommandLineHub> _cliHubCtx;
        private readonly IFileSystem _fileSystem;
        private readonly FileTransferOptions _options;
        private readonly PathValidator _pathValidator;
        private readonly FileSizeValidator _sizeValidator;
        private readonly ExtensionValidator _extensionValidator;

        public FileTransferEndpointService(
            ILogger<FileTransferEndpointService> logger, 
            IHubContext<CommandLineHub> cliHubCtx, 
            IFileSystem fileSystem,
            FileTransferOptions options)
        {
            _logger = logger;
            _cliHubCtx = cliHubCtx;
            _fileSystem = fileSystem;
            _options = options;
            _pathValidator = new PathValidator(options.StorageRootPath);
            _sizeValidator = new FileSizeValidator(options);
            _extensionValidator = new ExtensionValidator(options);
        }

        public async Task<IResult> UploadFile(Stream fileStream, string toFilePath, string connectionId, string correlationId, long? contentLength = null, string? clientChecksum = null)
        {
            _logger.LogDebug("File upload posted :: toFilePath={ToFilePath}; connectionId={ConnectionId}; correlationId={CorrelationId}; contentLength={ContentLength}", 
                toFilePath, connectionId, correlationId, contentLength);

            // Validate the path to prevent path traversal attacks
            string validatedPath;
            try
            {
                validatedPath = _pathValidator.ValidatePath(toFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Path traversal attempt detected: {Path}", toFilePath);
                return Results.Problem(
                    title: "Forbidden",
                    detail: "Path traversal is not allowed.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid path: {Path}", toFilePath);
                return Results.Problem(
                    title: "Bad Request",
                    detail: "Invalid file path.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Validate file extension
            try
            {
                _extensionValidator.ValidateExtension(toFilePath);
            }
            catch (FileExtensionNotAllowedException ex)
            {
                _logger.LogWarning(ex, "Disallowed file extension: {Path}", toFilePath);
                return Results.Problem(
                    title: "Bad Request",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Pre-flight size validation using Content-Length header
            try
            {
                _sizeValidator.ValidateContentLength(contentLength);
            }
            catch (FileSizeLimitExceededException ex)
            {
                _logger.LogWarning(ex, "File size exceeds limit: {ContentLength} > {MaxSize}", contentLength, _options.MaxFileSizeBytes);
                return Results.Problem(
                    title: "Payload Too Large",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status413PayloadTooLarge);
            }

            // get the client
            var client = _cliHubCtx.Clients.Client(connectionId);

            // Ensure the parent directory exists
            var parentDir = _fileSystem.Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrEmpty(parentDir) && !_fileSystem.Directory.Exists(parentDir))
            {
                _fileSystem.Directory.CreateDirectory(parentDir);
            }

            // delete existing file
            if (_fileSystem.File.Exists(validatedPath))
                _fileSystem.File.Delete(validatedPath);

            // upload the file with incremental checksum computation
            var buffer = new byte[81920];
            long totalRead = 0;
            var bytesRead = 0;
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            try
            {
                using var toStream = _fileSystem.File.Create(validatedPath);

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    totalRead += bytesRead;

                    // Streaming size check - abort early if limit exceeded
                    try
                    {
                        _sizeValidator.ValidateStreamingBytes(totalRead);
                    }
                    catch (FileSizeLimitExceededException ex)
                    {
                        _logger.LogWarning(ex, "Streaming size limit exceeded: {TotalRead} > {MaxSize}", totalRead, _options.MaxFileSizeBytes);
                        
                        // Close stream and delete partial file
                        toStream.Close();
                        if (_fileSystem.File.Exists(validatedPath))
                            _fileSystem.File.Delete(validatedPath);
                        
                        return Results.Problem(
                            title: "Payload Too Large",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status413PayloadTooLarge);
                    }

                    // Update checksum incrementally
                    hasher.AppendData(buffer, 0, bytesRead);

                    await toStream.WriteAsync(buffer, 0, bytesRead);

                    if (!string.IsNullOrEmpty(correlationId))
                        await client.SendAsync(SignalRMethodNames.ReceiveMessage, new FileUploadProgressMessage(totalRead) { CorrelationId = correlationId });
                }

                // Send final progress message for empty files (totalRead == 0 means loop never executed)
                if (totalRead == 0 && !string.IsNullOrEmpty(correlationId))
                {
                    await client.SendAsync(SignalRMethodNames.ReceiveMessage, new FileUploadProgressMessage(0) { CorrelationId = correlationId });
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error during file upload to {Path}", validatedPath);
                
                // Cleanup partial file
                if (_fileSystem.File.Exists(validatedPath))
                    _fileSystem.File.Delete(validatedPath);
                
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An error occurred while saving the file.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // Verify checksum if client provided one
            if (!string.IsNullOrEmpty(clientChecksum))
            {
                var serverChecksum = Convert.ToHexString(hasher.GetHashAndReset());
                
                if (!string.Equals(clientChecksum, serverChecksum, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Checksum mismatch for file {Path}. Client: {ClientChecksum}, Server: {ServerChecksum}", 
                        validatedPath, clientChecksum, serverChecksum);
                    
                    // Delete the uploaded file as integrity cannot be verified
                    if (_fileSystem.File.Exists(validatedPath))
                        _fileSystem.File.Delete(validatedPath);
                    
                    return Results.Problem(
                        title: "Bad Request",
                        detail: "File checksum verification failed. The file may have been corrupted during transfer.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                
                _logger.LogDebug("Checksum verified for file {Path}: {Checksum}", validatedPath, serverChecksum);
            }

            // return
            return Results.Ok();
        }

        /// <summary>
        /// Downloads a file from the server storage.
        /// </summary>
        public async Task<IResult> DownloadFile(string filePath, HttpContext httpContext)
        {
            _logger.LogDebug("File download requested :: filePath={FilePath}", filePath);

            // Validate the path to prevent path traversal attacks
            string validatedPath;
            try
            {
                validatedPath = _pathValidator.ValidatePath(filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Path traversal attempt detected in download: {Path}", filePath);
                return Results.Problem(
                    title: "Forbidden",
                    detail: "Path traversal is not allowed.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid path in download: {Path}", filePath);
                return Results.Problem(
                    title: "Bad Request",
                    detail: "Invalid file path.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Check if file exists
            if (!_fileSystem.File.Exists(validatedPath))
            {
                _logger.LogWarning("File not found for download: {Path}", validatedPath);
                return Results.Problem(
                    title: "Not Found",
                    detail: "The requested file does not exist.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            // Read file and compute checksum
            var fileBytes = await _fileSystem.File.ReadAllBytesAsync(validatedPath);
            
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hasher.AppendData(fileBytes);
            var checksum = Convert.ToHexString(hasher.GetHashAndReset());

            _logger.LogDebug("Sending file {Path} with checksum {Checksum}", validatedPath, checksum);

            // Add checksum header to response
            httpContext.Response.Headers["X-File-Checksum"] = checksum;
            httpContext.Response.Headers["Content-Length"] = fileBytes.Length.ToString();

            // Return file with checksum header
            return Results.File(
                fileBytes,
                contentType: "application/octet-stream",
                fileDownloadName: Path.GetFileName(validatedPath),
                enableRangeProcessing: false);
        }
    }
}