# Troubleshooting Remote CLI

[← Back to CommandLine Server](CommandLineServer.md)

Common issues and solutions when working with BitPantry.CommandLine remote functionality over SignalR.

## Table of Contents

- [Connection Issues](#connection-issues)
- [Authentication Issues](#authentication-issues)
- [File Transfer Issues](#file-transfer-issues)
- [Command Execution Issues](#command-execution-issues)
- [Debugging Tips](#debugging-tips)
- [Common Error Messages](#common-error-messages)
- [Getting Help](#getting-help)
- [See Also](#see-also)

## Connection Issues

### Unable to Connect to Server

**Symptoms**: Client fails to connect with timeout or connection refused errors.

**Solutions**:

1. **Verify server is running** - Ensure the server application is running and listening on the expected URL
2. **Check URL format** - The hub URL should include the full path: `https://localhost:5001/commandline`
3. **Verify network connectivity** - Ensure firewall rules allow the connection
4. **Check SSL certificates** - For HTTPS, ensure certificates are trusted

```bash
# Test connection
connect https://localhost:5001/commandline
```

### Connection Drops Frequently

**Symptoms**: Client disconnects unexpectedly during operations.

**Solutions**:

1. **Check server logs** for errors or exceptions
2. **Increase timeouts** on both client and server
3. **Monitor network stability** between client and server

## Authentication Issues

### "Unauthorized" Response

**Symptoms**: Server returns 401 Unauthorized when attempting to connect.

**Solutions**:

1. **Verify API key** - Ensure you're providing the correct API key
2. **Check server configuration** - Verify `IApiKeyStore` returns valid credentials
3. **Inspect server logs** - Look for authentication rejection reasons

```csharp
// Server-side: Verify your API key store implementation
public class MyApiKeyStore : IApiKeyStore
{
    public Task<bool> Validate(string apiKey)
    {
        // Log for debugging
        Console.WriteLine($"Validating key: {apiKey}");
        return Task.FromResult(apiKey == "expected-key");
    }
}
```

### Token Refresh Failures

**Symptoms**: Client gets disconnected after token expires.

**Solutions**:

1. **Check refresh token validity** - Ensure refresh tokens aren't expired
2. **Verify `IRefreshTokenStore`** implementation stores and retrieves correctly
3. **Adjust token refresh settings**:

```csharp
builder.ConfigureSignalRClient(options =>
{
    // Start refreshing earlier
    options.TokenRefreshThreshold = TimeSpan.FromMinutes(10);
    // Check more frequently
    options.TokenRefreshMonitorInterval = TimeSpan.FromSeconds(30);
});
```

## File Transfer Issues

### Upload Fails with Path Error

**Symptoms**: File upload fails with path-related errors.

**Solutions**:

1. **Check `FileSystemConfiguration`** on server:

```csharp
.UseSignalR(opt =>
{
    opt.FileSystemConfiguration = new FileSystemConfiguration
    {
        SandboxPath = @"C:\allowed\uploads",
        AllowPathTraversal = false  // Security default
    };
})
```

2. **Verify destination path** is within the sandbox directory
3. **Check file permissions** on the server

### Upload Progress Not Updating

**Symptoms**: File uploads work but progress callbacks don't fire.

**Solutions**:

1. **Register progress callback** before starting upload
2. **Ensure async/await** is used correctly

```csharp
var fileService = app.Services.GetRequiredService<FileTransferService>();

await fileService.UploadFile(
    localPath,
    remotePath,
    progress =>
    {
        Console.WriteLine($"Progress: {progress.PercentComplete}%");
    });
```

## Command Execution Issues

### Command Not Found After Connect

**Symptoms**: Remote commands show in `list` but fail to execute.

**Solutions**:

1. **Verify command registration** on server
2. **Check command groups** - Use fully qualified group path if needed
3. **Review server logs** for execution errors

### Command Output Not Displaying

**Symptoms**: Commands execute but output doesn't appear on client.

**Solutions**:

1. **Use `Console` from `CommandBase`** - Never use `System.Console` directly
2. **Ensure async commands** await properly
3. **Check for exceptions** being swallowed silently

```csharp
// Correct - uses the framework's console
public class MyCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine("This works remotely!");
    }
}

// Wrong - bypasses remote output
public class BadCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        System.Console.WriteLine("This won't show on remote client!");
    }
}
```

## Debugging Tips

### Enable Detailed Logging

Configure logging on both client and server:

```csharp
// Client
builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddFilter("BitPantry", LogLevel.Debug);
    logging.AddConsole();
});

// Server
services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddFilter("BitPantry", LogLevel.Debug);
    logging.AddConsole();
});
```

### Check Connection State

```csharp
var proxy = app.Services.GetRequiredService<IServerProxy>();
Console.WriteLine($"Connected: {proxy.IsConnected}");
Console.WriteLine($"Server URL: {proxy.ServerUrl}");
```

### Test Commands Locally First

Before testing remote execution, verify commands work locally to isolate issues.

## Common Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| `ResolutionError` | Command not found | Check command registration and spelling |
| `Connection refused` | Server not running | Start the server application |
| `401 Unauthorized` | Invalid credentials | Verify API key or JWT token |
| `Path traversal blocked` | Security restriction | Use paths within sandbox |
| `Token expired` | JWT token timeout | Reconnect or fix refresh logic |

## Getting Help

If you're still experiencing issues:

1. **Check the examples** in the repository
2. **Review server logs** for detailed error messages
3. **Open an issue** on GitHub with reproduction steps

## See Also

- [Client](Client.md) - Client setup guide
- [CommandLineServer](CommandLineServer.md) - Server configuration
- [SignalRClientOptions](SignalRClientOptions.md) - Client options reference
- [JwtAuthOptions](JwtAuthOptions.md) - Authentication configuration
