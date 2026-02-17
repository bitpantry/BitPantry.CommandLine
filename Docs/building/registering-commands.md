# Registering Commands

Commands must be registered with the builder before they can be invoked. Registration can be done individually or by scanning assemblies.

---

## Individual Registration

Register commands one at a time with `RegisterCommand<T>()`:

```csharp
var app = new CommandLineApplicationBuilder()
    .RegisterCommand<GreetCommand>()
    .RegisterCommand<DeployCommand>()
    .RegisterCommand<StatusCommand>()
    .Build();
```

Or by `Type`:

```csharp
builder.RegisterCommand(typeof(GreetCommand));
```

---

## Assembly Scanning

Register all commands in one or more assemblies by providing a target type from each assembly:

```csharp
var app = new CommandLineApplicationBuilder()
    .RegisterCommands(typeof(GreetCommand))   // Scans the assembly containing GreetCommand
    .Build();
```

Scan multiple assemblies:

```csharp
builder.RegisterCommands(typeof(LocalCommand), typeof(PluginCommand));
```

Exclude specific types:

```csharp
builder.RegisterCommands(
    assemblyTargetTypes: new[] { typeof(GreetCommand) },
    ignoreTypes: new[] { typeof(DebugCommand) }
);
```

---

## Group Auto-Registration

When a command with `[InGroup<T>]` is registered, its group hierarchy is automatically discovered and registered. You do not need to explicitly register group marker classes:

```csharp
// Only the command needs to be registered
builder.RegisterCommand<ConnectCommand>();
// The ServerGroup marker class is registered automatically
```

---

## Duplicate Handling

By default, registering two commands with the same name throws an exception. To allow replacement:

```csharp
var registryBuilder = new CommandRegistryBuilder
{
    ReplaceDuplicateCommands = true
};
```

When enabled, the last registration wins.

---

## See Also

- [Defining Commands](../commands/index.md)
- [Command Groups](../commands/groups.md)
- [Dependency Injection](dependency-injection.md)
- [Building the Application](index.md)
