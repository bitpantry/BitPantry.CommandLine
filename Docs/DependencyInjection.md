Any strateegy can be used since the command is created from the container - constructor, property, etc.

# Dependency Injection
Dependency Injection can be configured using the [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) and an implementation of the [IContainer](IContainer.md) interface.

```cs
/// <summary>
/// Configures the CommandLineApplicationBuilder to build a CommandLineApplication that uses the given IContainer implementation for instance and dependency management
/// </summary>
/// <param name="container">The container implementation to use</param>
/// <returns>The CommandLineApplicationBuilder</returns>
public CommandLineApplicationBuilder UsingDependencyContainer(IContainer container)
```

By default, if ```UsingDependencyContainer``` is not called on the [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md), ```Activator.CreateInstance(commandType)``` is used via the private [IContainer](IContainer.md) implementation, ```BitPantry.CommandLine.Processing.Activation.SystemActivatorContainer```.

## Using Microsoft's Service Provider

The solution also includes a public [IContainer](IContainer.md) implementation which uses the ```Microsoft.Extensions.DependencyInjection.ServiceProvider``` via the [IContainer](IContainer.md) implementation, ```BitPantry.CommandLine.Processing.Activation.ServiceProviderContainer```. 

```cs
using Microsoft.Extensions.DependencyInjection;
using BitPantry.CommandLine.Processing.Activation;

...

var services = new ServiceCollection();

// ... add any dependencies to the service collection here ...

// uses the BitPantry.CommandLine.ServiceCollectionExtensions to search through
// the assembly containing Program.cs and add all commands as transient. Of
// course, commands could be added manually to configure the strategy 
// differently.
services.AddCommands(typeof(Program))

// builds the new container
var container = new ServiceProviderContainer(services.BuildServiceProvider());

...

// configure the command line application to use the newly built container
var builder = new CommandLineApplicationBuilder()
     .UsingDependencyContainer(container);

```

*For more information on implementing the ```IContainer``` interface, see [IContainer](IContainer.md).*

## Composition Root and Dependency Scope
The dependency injection composition root is *defined* in the [CommandLineApplication](CommandLineApplication.md) where [string command expressions](CommandSyntax.md) are first submitted for execution, parsed, resolved, and finally run. For each command, a new dependency scope is created. The helper extension function, ```AddCommands``` in the code sample above configures commands for *transient* scope.

---
See also,

- [IContainer](IContainer.md)
- [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md)