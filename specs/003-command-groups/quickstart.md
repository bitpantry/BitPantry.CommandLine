# Quickstart: Command Groups

This guide shows you how to organize commands into groups using the new hierarchical group system.

---

## Basic Concepts

- **Groups** organize related commands into hierarchical structures
- Groups are **non-executable** - invoking a group displays its help
- Commands reference groups via `[Command(Group = typeof(GroupClass))]`
- Invocation uses **space-separated** syntax: `myapp group subgroup command`

---

## Step 1: Define a Group

Create a class decorated with `[Group]`:

```csharp
using BitPantry.CommandLine.API;

[Group]
[Description("User management commands")]
public class Users { }
```

The group name is derived from the class name:
- `Users` → `users`
- `UserManagement` → `user-management` (kebab-case)

---

## Step 2: Add Commands to a Group

Reference the group type in your command:

```csharp
[Command(Group = typeof(Users))]
[Description("List all users")]
public class ListCommand : CommandBase
{
    public override async Task<int> Run()
    {
        Console.WriteLine("Listing users...");
        return 0;
    }
}

[Command(Group = typeof(Users))]
[Description("Add a new user")]
public class AddCommand : CommandBase
{
    [Argument]
    public string Username { get; set; }
    
    public override async Task<int> Run()
    {
        Console.WriteLine($"Adding user: {Username}");
        return 0;
    }
}
```

---

## Step 3: Use Your Commands

```bash
# Show group help (lists available commands)
$ myapp users
Users - User management commands

Commands:
  list     List all users
  add      Add a new user

# Run a command
$ myapp users list
Listing users...

$ myapp users add john
Adding user: john

# Get help for a specific command
$ myapp users add --help
add - Add a new user

Arguments:
  username   (required)
```

---

## Creating Nested Groups

Use nested classes to create hierarchical groups:

```csharp
[Group]
[Description("Database operations")]
public class Db
{
    [Group]
    [Description("Database migration commands")]
    public class Migrate { }
    
    [Group]
    [Description("Database backup commands")]
    public class Backup { }
}
```

Then reference the nested type:

```csharp
[Command(Group = typeof(Db.Migrate))]
[Description("Run pending migrations")]
public class UpCommand : CommandBase
{
    public override async Task<int> Run()
    {
        Console.WriteLine("Running migrations...");
        return 0;
    }
}

[Command(Group = typeof(Db.Backup))]
[Description("Create a database backup")]
public class CreateCommand : CommandBase
{
    public override async Task<int> Run()
    {
        Console.WriteLine("Creating backup...");
        return 0;
    }
}
```

**Usage:**

```bash
$ myapp db
Db - Database operations

Groups:
  migrate   Database migration commands
  backup    Database backup commands

$ myapp db migrate
Migrate - Database migration commands

Commands:
  up   Run pending migrations

$ myapp db migrate up
Running migrations...

$ myapp db backup create
Creating backup...
```

---

## Root-Level Commands

Commands without a group are available at the root:

```csharp
[Command]  // No Group specified
[Description("Display application version")]
public class VersionCommand : CommandBase
{
    public override async Task<int> Run()
    {
        Console.WriteLine("v1.0.0");
        return 0;
    }
}
```

**Usage:**

```bash
$ myapp version
v1.0.0
```

---

## Getting Help

The `--help` (or `-h`) flag works at any level:

```bash
# Application help (shows all groups and root commands)
$ myapp --help

# Group help
$ myapp users --help
# Same as just typing: myapp users

# Command help
$ myapp users add --help
```

---

## Registration

Groups and commands are registered via assembly scanning:

```csharp
var app = CommandLineApplication.Create()
    .RegisterCommands(typeof(Program).Assembly)
    .Build();

await app.Run(args);
```

Groups are automatically discovered when commands reference them.

---

## Naming Conventions

| Class Name | CLI Name |
|------------|----------|
| `Users` | `users` |
| `UserManagement` | `user-management` |
| `Db` | `db` |
| `AddUserCommand` | `add-user` (with `Command` suffix stripped) |

You can override the derived name:

```csharp
[Command(Name = "add", Group = typeof(Users))]
public class AddUserCommand : CommandBase { }
```

---

## Complete Example

```csharp
// Groups.cs
[Group]
[Description("Git operations")]
public class Git
{
    [Group]
    [Description("Remote repository commands")]
    public class Remote { }
}

// Commands/CloneCommand.cs
[Command(Group = typeof(Git))]
[Description("Clone a repository")]
public class CloneCommand : CommandBase
{
    [Argument]
    public string Url { get; set; }
    
    public override async Task<int> Run()
    {
        Console.WriteLine($"Cloning {Url}...");
        return 0;
    }
}

// Commands/AddRemoteCommand.cs
[Command(Name = "add", Group = typeof(Git.Remote))]
[Description("Add a remote")]
public class AddRemoteCommand : CommandBase
{
    [Argument]
    public string Name { get; set; }
    
    [Argument]
    public string Url { get; set; }
    
    public override async Task<int> Run()
    {
        Console.WriteLine($"Adding remote {Name} -> {Url}");
        return 0;
    }
}
```

**Usage:**

```bash
$ myapp git clone https://github.com/user/repo
Cloning https://github.com/user/repo...

$ myapp git remote add origin https://github.com/user/repo
Adding remote origin -> https://github.com/user/repo
```

---

## Tips

1. **Keep groups focused** - Each group should have a clear, single purpose
2. **Limit nesting depth** - 2-3 levels maximum for usability
3. **Use descriptive names** - Groups should be nouns, commands should be verbs
4. **Provide descriptions** - Always add `[Description]` for help output
