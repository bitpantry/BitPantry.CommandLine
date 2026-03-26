# Plugins Guide

Create self-contained command packages that can be distributed as compiled DLLs and loaded into a BitPantry.CommandLine application at startup.

---

## Overview

The plugin system allows you to:

- Package commands, DI services, and autocomplete handlers in a single module
- Distribute plugins as compiled DLLs without NuGet publishing
- Load plugins from a directory at application startup
- Maintain dependency isolation between plugins

---

## Creating a Plugin

### 1. Create a Class Library Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Reference without copying to output -->
    <PackageReference Include="BitPantry.CommandLine" Version="5.3.1">
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
    <!-- Or for same-solution development: -->
    <ProjectReference Include="..\BitPantry.CommandLine\BitPantry.CommandLine.csproj">
      <Private>false</Private>
      <ExcludeAssets>runtime</ExcludeAssets>
    </ProjectReference>
  </ItemGroup>
</Project>
```

**Key settings:**
- `<EnableDynamicLoading>true</EnableDynamicLoading>` — Ensures plugin dependencies are copied to output
- `<ExcludeAssets>runtime</ExcludeAssets>` — Prevents the host's assemblies from being copied into the plugin

### 2. Implement ICommandModule

```csharp
using BitPantry.CommandLine.API;
using Microsoft.Extensions.DependencyInjection;

public class MyModule : ICommandModule
{
    public string Name => "MyModule";

    public void Configure(ICommandModuleContext context)
    {
        // Register commands
        context.Commands.RegisterCommand(typeof(MyCommand));
        context.Commands.RegisterCommand(typeof(AnotherCommand));

        // Register DI services
        context.Services.AddSingleton<IMyService, MyService>();

        // Register autocomplete handlers
        context.AutoComplete.Register<MyTypeHandler>();
    }
}
```

### 3. Create Commands

Commands in plugins work identically to in-process commands:

```csharp
using BitPantry.CommandLine.API;
using Spectre.Console;

[Command(Name = "my-command")]
[Description("Does something useful")]
public class MyCommand : CommandBase
{
    private readonly IMyService _service;

    [Argument(Position = 0)]
    [Description("The input value")]
    public string Input { get; set; } = "";

    public MyCommand(IMyService service)
    {
        _service = service;
    }

    public void Execute(CommandExecutionContext ctx)
    {
        var result = _service.Process(Input);
        Console.MarkupLine($"[green]Result:[/] {result}");
    }
}
```

### 4. Use Groups

Grouped commands work the same way:

```csharp
[Group(Name = "mygroup")]
[Description("My command group")]
public class MyGroup { }

[InGroup<MyGroup>]
[Command(Name = "subcommand")]
public class SubCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}
```

---

## Loading Plugins

### In-Process Modules

For modules in the same solution or referenced directly:

```csharp
var app = new CommandLineApplicationBuilder()
    .InstallModule<MyModule>()
    .Build();
```

With configuration:

```csharp
builder.InstallModule<MyModule>(m => m.Option = "value");
```

### From a Directory

For plugins distributed as compiled DLLs, use a directory structure:

```
myapp/
├── myapp.exe
├── BitPantry.CommandLine.dll
└── plugins/
    ├── MyPlugin/
    │   ├── MyPlugin.dll
    │   └── SomeDependency.dll
    └── AnotherPlugin/
        └── AnotherPlugin.dll
```

Load all plugins:

```csharp
var app = new CommandLineApplicationBuilder()
    .InstallModulesFromDirectory("./plugins")
    .Build();
```

**Convention:** Each subdirectory should contain a DLL with the same name as the directory.

### From a Single Assembly

```csharp
builder.InstallModuleFromAssembly("./plugins/MyPlugin/MyPlugin.dll");
```

---

## How It Works

### Assembly Load Context

Each plugin assembly is loaded into its own `AssemblyLoadContext` for dependency isolation:

- **Plugin dependencies** are resolved from the plugin's directory
- **Shared types** (`CommandBase`, `ICommandModule`, attributes) are resolved from the host's default context

This ensures:
- Plugins can have their own versions of dependencies without conflicting
- Command types from plugins integrate seamlessly with the host's command system
- DI resolution works correctly because the same `Type` references are used

### Module Context

When `Configure(context)` is called, the module receives access to:

| Property | Purpose |
|----------|---------|
| `context.Commands` | Register command types via `RegisterCommand(Type)` |
| `context.Services` | Add services to the host's DI container |
| `context.AutoComplete` | Register autocomplete handlers |

The module **does not** have access to:
- Console configuration
- Theme settings
- Prompt configuration
- File system configuration

This keeps plugins focused on their core responsibility: providing commands and their dependencies.

---

## Error Handling

### Missing Plugins Directory

If `InstallModulesFromDirectory` is called with a non-existent path, it's a no-op (no error). This allows specifying a default plugins path without requiring the directory to exist.

### Missing Assembly

If `InstallModuleFromAssembly` is called with a non-existent path, a `FileNotFoundException` is thrown with the path in the message.

### Duplicate Commands

If two modules register commands with the same name in the same group, an `ArgumentException` is thrown at registration time (during `InstallModule`).

---

## Best Practices

1. **Single Responsibility** — Each module should represent a cohesive set of related commands
2. **Document Dependencies** — List any services your module requires from the host
3. **Test Independently** — Create unit tests for your module using the in-process `InstallModule<T>()` method
4. **Version Carefully** — Ensure your plugin references a compatible version of BitPantry.CommandLine

---

## Example: Complete Plugin

**MyPlugin.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BitPantry.CommandLine" Version="5.3.1">
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
    <PackageReference Include="Spectre.Console" Version="0.54.0">
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

**MyPluginModule.cs:**
```csharp
using BitPantry.CommandLine.API;
using Microsoft.Extensions.DependencyInjection;

public class MyPluginModule : ICommandModule
{
    public string Name => "MyPlugin";

    public void Configure(ICommandModuleContext context)
    {
        context.Commands.RegisterCommand(typeof(GreetCommand));
        context.Services.AddSingleton<IGreetingService, GreetingService>();
    }
}

public interface IGreetingService
{
    string GetGreeting(string name);
}

public class GreetingService : IGreetingService
{
    public string GetGreeting(string name) => $"Hello, {name}!";
}

[Command(Name = "greet")]
[Description("Greets a user")]
public class GreetCommand : CommandBase
{
    private readonly IGreetingService _service;

    [Argument(Position = 0, IsRequired = true)]
    public string Name { get; set; } = "";

    public GreetCommand(IGreetingService service)
    {
        _service = service;
    }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"[green]{_service.GetGreeting(Name)}[/]");
    }
}
```

**Host Application:**
```csharp
var app = new CommandLineApplicationBuilder()
    .InstallModulesFromDirectory("./plugins")
    .ConfigurePrompt(p => p.Name("myapp"))
    .Build();

await app.RunInteractive();
```

---

## See Also

- [Builder API](../api-reference/builder-api.md)
- [Interfaces (ICommandModule, ICommandModuleContext)](../api-reference/interfaces.md)
- [Defining Commands](../commands/index.md)
- [Autocomplete Handlers](../autocomplete/type-handlers.md)
