# Built-in Handlers

Two autocomplete handlers are registered automatically — no configuration required. Two additional handlers, `FilePathAutoCompleteHandler` and `DirectoryPathAutoCompleteHandler`, are available as attribute handlers for path arguments.

---

## EnumAutoCompleteHandler

Suggests all values of an `enum` type, filtered by the current input prefix:

```csharp
public enum Priority { Low, Medium, High, Critical }

[Command(Name = "task")]
public class TaskCommand : CommandBase
{
    [Argument(Name = "priority")]
    public Priority Priority { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Priority: {Priority}");
    }
}
```

```
app> task --priority _
  Low
  Medium
  High
  Critical

app> task --priority h_
  High
```

The handler also supports `Nullable<T>` enum types (e.g., `Priority?`).

---

## BooleanAutoCompleteHandler

Suggests `true` and `false` for non-flag `bool` arguments, filtered by prefix:

```csharp
[Argument(Name = "debug")]
public bool Debug { get; set; }
```

```
app> build --debug _
  true
  false

app> build --debug t_
  true
```

This handler does **not** activate for `[Flag]` booleans, since flags are presence-only and never accept a value.

---

## FilePathAutoCompleteHandler

Suggests file and directory names for string arguments decorated with `[FilePathAutoComplete]`. Uses `IFileSystem` from `System.IO.Abstractions`, so it works transparently with both local and sandboxed (remote) file systems.

```csharp
[Command(Name = "open")]
public class OpenCommand : CommandBase
{
    [Argument(Name = "path")]
    [FilePathAutoComplete]
    public string FilePath { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Opening: {FilePath}");
    }
}
```

```
app> open --path _
  docs\
  src\
  README.md
  config.json

app> open --path docs\_
  docs\guide.txt
  docs\readme.md

app> open --path docs\re_
  docs\readme.md
```

Key behaviors:
- **Directories first** — directory entries are listed before files, both sorted alphabetically
- **Directory separator appended** — directory options end with the platform path separator (e.g., `\` on Windows, `/` on Linux)
- **Directory styling** — directories render with the `Theme.MenuGroup` style (cyan by default) for visual distinction
- **Case-insensitive matching** — fragments filter entries case-insensitively
- **Relative paths** — supports `..` and nested directory prefixes
- **Graceful error handling** — returns empty results for non-existent directories or access errors

### Registration

Unlike the type handlers above, `FilePathAutoCompleteHandler` is an **attribute handler** — it activates only on properties decorated with `[FilePathAutoComplete]`. The handler type is automatically registered in the DI container when commands using the attribute are registered.

### Custom Attribute Sugar

`[FilePathAutoComplete]` is syntactic sugar for `[AutoComplete<FilePathAutoCompleteHandler>]`. You can use either form.

---

## DirectoryPathAutoCompleteHandler

Suggests **directory names only** (excludes files) for string arguments decorated with `[DirectoryPathAutoComplete]`. Shares the same base implementation as `FilePathAutoCompleteHandler` but filters results to directories only. Uses `IFileSystem` from `System.IO.Abstractions` for local/remote transparency.

```csharp
[Command(Name = "cd")]
public class CdCommand : CommandBase
{
    [Argument(Name = "directory", Position = 0)]
    [DirectoryPathAutoComplete]
    public string Directory { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Changed to: {Directory}");
    }
}
```

```
app> cd _
  docs\
  src\
  tests\

app> cd s_
  src\

app> cd src\_
  src\api\
  src\models\
  src\utils\
```

Key behaviors:
- **Directories only** — file entries are excluded; only subdirectories are shown
- **Directory separator appended** — directory options end with the platform path separator
- **Directory styling** — directories render with the `Theme.MenuGroup` style (cyan by default)
- **Case-insensitive matching** — fragments filter entries case-insensitively
- **Relative paths** — supports `..` and nested directory prefixes
- **Graceful error handling** — returns empty results for non-existent directories or access errors

### Registration

Like `FilePathAutoCompleteHandler`, this is an **attribute handler** — it activates only on properties decorated with `[DirectoryPathAutoComplete]`. The handler type is automatically registered in the DI container when commands using the attribute are registered.

### Custom Attribute Sugar

`[DirectoryPathAutoComplete]` is syntactic sugar for `[AutoComplete<DirectoryPathAutoCompleteHandler>]`. You can use either form.

---

## Registration

Both handlers are registered by default in the `AutoCompleteHandlerRegistryBuilder`. They participate at the lowest priority level — attribute handlers and custom type handlers take precedence.

---

## See Also

- [Autocomplete](index.md)
- [Custom Attribute Handlers](attribute-handlers.md)
- [Custom Type Handlers](type-handlers.md)
