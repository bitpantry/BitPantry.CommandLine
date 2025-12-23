# Command Line Server

Using the ```BitPantry.CommandLine.Remote.SignalR.Server``` package you can configure an ASP.NET application to host commands over SignalR, including JWT authentication.

```
NuGet\Install-Package BitPantry.CommandLine.Remote.SignalR.Server
```

See how to [configure the client](Client.md) to connect the server and execute remote commands.

# Setup

To configure a command line server, create (or use an existing) ASP.NET application. Use ```AddCommandLineHub``` on the *WebApplicationBuilder* service collection, and ```ConfigureCommandLineHub``` on the *WebApplication*.

```cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCommandLineHub();  // <------------- STEP 1

        var app = builder.Build();

        app.ConfigureCommandLineHub();   // <------------- STEP 2

        app.Run();
    }
}
```

The web application is now configured to host a command line server over SignalR at the path */cli* (by default).

## Configuring the Server

The ```AddCommandLineHub``` extension accepts a configuration action that accepts a ```CommandLineServerOptions``` object that can be used to register commands and configure other server features.

### Registering Commands

To register commands, use the ```RegisterCommands``` function.

```
builder.Services.AddCommandLineHub(opt =>
{
    opt.RegisterCommands(typeof(Program)); // registers all classes that extend CommandBase in the assembly containing type *Program*.
});
```

All of the registration functions available from the [CommandRegistry](../CommandLine/CommandRegistry.md) are exposed by the ```CommandLineServerOptions```.

### Configuring the Server Path

By default, the server will run at the path */cli*. You can configure the server to use a different path by setting the ```HubUrlPattern``` property.

```
/// <summary>
/// The URL pattern for the <see cref="CommandLineHub"/>. e.g, a pattern of "/cli" for a web application with a root of 
/// http://localhost will make the hub's uri, "http://localhost/cli".
/// </summary>
public string HubUrlPattern { get; set; } = "/cli";
```

### Configuring Authentication

The command line server can be configured to support JWT authentication using ```AddJwtAuthentication```.

```
builder.Services.AddCommandLineHub(opt =>
{
    opt.RegisterCommands(typeof(Program));
    opt.AddJwtAuthentication<IApiKeyStore, IRefreshTokenStore>("atLeast128BitSecretForSigningTokens", authOpts =>
    {
        // configure authentication
    });
});
```

To configure authentication, you'll need pass your *API key and refresh token store* implementations as generic parameters to the ```AddJwtAuthentication``` extension.

An optional configuration action can be used to configure the ```JwtAuthOptions```.

- Read more about implementing the [IApiKeyStore](IApiKeyStore.md) 
- Read more about implmeneting the [IRefreshTokenStore](IRefreshTokenStore.md)
- Read more about configuring authentication with the [JwtAuthOptions](JwtAuthOptions.md)

### Configuring File System Access

Commands running on the server can use `IFileSystem` to perform file operations. Configure file storage and security settings using `FileTransferOptions`:

```csharp
builder.Services.AddCommandLineHub(opt =>
{
    opt.RegisterCommands(typeof(Program));
    
    opt.FileTransferOptions = new FileTransferOptions
    {
        // Root directory for all file operations (required)
        StorageRootPath = "/var/app/storage",
        
        // Maximum file size for uploads (default: 100 MB)
        MaxFileSizeBytes = 50 * 1024 * 1024,
        
        // Allowed extensions (null = all allowed)
        AllowedExtensions = new[] { ".txt", ".json", ".csv" }
    };
});
```

The file system is sandboxed to the `StorageRootPath` directory, preventing path traversal attacks. All uploads are validated for size limits and extension restrictions, and integrity is verified via SHA256 checksums.

- Read more about [using IFileSystem in commands](FileSystem.md)
- Read more about [file system configuration options](FileSystemConfiguration.md)

---
See also

- [CommandRegistry](../CommandLine/CommandRegistry.md)
- [CommandLineServerOptions](CommandLineServerOptions.md)
- [IApiKeyStore](IApiKeyStore.md)
- [IRefreshTokenStore](IRefreshTokenStore.md)
- [FileSystem](FileSystem.md)
- [FileSystemConfiguration](FileSystemConfiguration.md)