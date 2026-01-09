using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5000
builder.WebHost.UseUrls("http://localhost:5000");

// Add CommandLine Hub services
builder.Services.AddCommandLineHub(opt =>
{
    // Configure file transfer options
    var storagePath = Path.GetFullPath("./storage");
    opt.FileTransferOptions.StorageRootPath = storagePath;
    
    // Ensure the storage directory exists
    if (!Directory.Exists(storagePath))
        Directory.CreateDirectory(storagePath);
});

var app = builder.Build();

// Configure the CommandLine Hub (this sets up routing and endpoints)
app.ConfigureCommandLineHub();

Console.WriteLine("Sandbox Server running on http://localhost:5000");
Console.WriteLine("Storage root: " + Path.GetFullPath("./storage"));
Console.WriteLine("Press Ctrl+C to stop...");

app.Run();
