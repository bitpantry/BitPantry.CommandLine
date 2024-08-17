Any strateegy can be used since the command is created from the container - constructor, property, etc.

# Dependency Injection
The [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) exposes an `IServiceCollection` property, `Services`. Add your dependencies to this collection. Commands are added to the service collection by the builder when registered.

## Composition Root and Dependency Scope
The dependency injection composition root is *defined* in the [CommandLineApplication](CommandLineApplication.md) where [string command expressions](CommandSyntax.md) are first submitted for execution, parsed, resolved, and finally run. For each command, a new dependency scope is created. The helper extension function, ```AddCommands``` in the code sample above configures commands for *transient* scope.

---
See also,

- [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md)
- [string command expressions](CommandSyntax.md)