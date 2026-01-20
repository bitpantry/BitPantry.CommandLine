# Backlog: Scope Lifecycle in Command Activation

## Current Pattern

In `CommandActivator.Activate()` and 3 other locations, scopes are created inline without disposal:

```csharp
var cmd = _svcProvider.CreateScope().ServiceProvider.GetRequiredService(resCmd.CommandInfo.Type) as CommandBase;
```

### Locations (4 occurrences)

| File | Line | Context |
|------|------|---------|
| [CommandActivator.cs](../../BitPantry.CommandLine/Processing/Activation/CommandActivator.cs#L37) | 37 | Command activation for local execution |
| [AutoCompleteOptionSetBuilder.cs](../../BitPantry.CommandLine/AutoComplete/AutoCompleteOptionSetBuilder.cs#L257) | 257 | Resolving command for autocomplete function invocation |
| [AutoCompleteOptionSetBuilder.cs](../../BitPantry.CommandLine/AutoComplete/AutoCompleteOptionSetBuilder.cs#L568) | 568 | Resolving command for autocomplete function invocation |
| [ServerLogic.cs](../../BitPantry.CommandLine.Remote.SignalR.Server/ServerLogic.cs#L126) | 126 | SignalR server-side command execution |

---

## Is This Actually a Problem?

### Short Answer: **Yes, This Is a Real Bug**

Even though `CommandBase` doesn't implement `IDisposable`, **commands inject dependencies via constructor** — and those dependencies may implement `IDisposable`:

```csharp
public class MyCommand : CommandBase
{
    private readonly IDbConnection _connection;  // Implements IDisposable
    private readonly HttpClient _httpClient;     // Implements IDisposable
    
    public MyCommand(IDbConnection connection, HttpClient httpClient)
    {
        _connection = connection;
        _httpClient = httpClient;
    }
    
    public async Task<int> Execute() { /* ... */ }
}
```

The **scope owns those injected services**. When the scope is disposed, it disposes all `IDisposable` services it created. Since the scope is never disposed:

- Database connections leak
- HTTP clients leak  
- File handles leak
- Any scoped `IDisposable` service leaks

### What the Microsoft Guidelines Say

From [Dependency Injection Guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines):

> **Disposable transient services captured by container**: When you register transient services that implement `IDisposable`, by default the DI container holds onto these references. It doesn't dispose of them until the container is disposed... **This can turn into a memory leak if resolved from the root scope.**

This applies to **any** `IDisposable` service resolved from a scope — including dependencies injected into commands.

---

## Impact

| Scenario | Consequence |
|----------|-------------|
| Command injects `DbContext` (scoped) | Connection pool exhaustion |
| Command injects `HttpClient` (transient disposable) | Socket exhaustion |
| Command injects file stream wrapper | File handle leak |
| High-volume CLI execution | Memory pressure, resource exhaustion |
| Long-running CLI session | Gradual resource leak over time |

---

## Recommended Fix

The scope must be managed properly, with disposal occurring **after** command execution completes.

### Current Flow (Broken)

```
Activator.Activate() → creates scope (discarded), gets command, returns command
Executor.Execute() → uses command
// scope never disposed → injected IDisposable dependencies leak
```

### Proper Fix: ActivationResult Owns the Scope

The activator creates the scope and passes it to `ActivationResult`. The caller disposes the result after execution.

```csharp
// CommandActivator.Activate():
var scope = _svcProvider.CreateScope();
var cmd = scope.ServiceProvider.GetRequiredService(...) as CommandBase;
// ... property injection ...
return new ActivationResult(cmd, resCmd, scope);  // Pass scope

// ActivationResult implements IDisposable, disposes scope in Dispose()

// CommandLineApplicationCore.ExecuteLocalCommand:
using var activation = _activator.Activate(rsCmd);  // Dispose after execution
activation.Command.SetConsole(_console);
// ... execute ...
return new RunResult { Result = output };
// activation disposed here → scope disposed → dependencies cleaned up
```

---

## Priority

**Medium-High** — This is a real resource leak that affects any command injecting `IDisposable` services. Should be addressed before users encounter connection pool exhaustion or similar issues in production.

---

## Detailed Remediation Plan

### Design Decision: Who Owns the Scope?

The scope must live long enough for the command to execute. Two approaches:

| Approach | Pros | Cons |
|----------|------|------|
| **A: ActivationResult owns scope** | Minimal changes to call sites | ActivationResult becomes IDisposable, callers must dispose |
| **B: Caller creates scope, passes to activator** | Clear ownership | More invasive changes |

**Recommended: Approach A** — `ActivationResult` owns and returns the scope, implementing `IDisposable`.

---

### Step 1: Update `ActivationResult` to Own the Scope

**File:** `BitPantry.CommandLine/Processing/Activation/ActivationResult.cs`

```csharp
public class ActivationResult : IDisposable
{
    private readonly IServiceScope _scope;
    
    public CommandBase Command { get; }
    public ResolvedCommand ResolvedCommand { get; }
    
    public ActivationResult(CommandBase command, ResolvedCommand resolvedCommand, IServiceScope scope)
    {
        Command = command;
        ResolvedCommand = resolvedCommand;
        _scope = scope;
    }
    
    public void Dispose()
    {
        _scope?.Dispose();
    }
}
```

---

### Step 2: Update `CommandActivator.Activate()`

**File:** [CommandActivator.cs](../../BitPantry.CommandLine/Processing/Activation/CommandActivator.cs) line 37

**Before:**
```csharp
var cmd = _svcProvider.CreateScope().ServiceProvider.GetRequiredService(resCmd.CommandInfo.Type) as CommandBase;
// ... property injection ...
return new ActivationResult(cmd, resCmd);
```

**After:**
```csharp
var scope = _svcProvider.CreateScope();
var cmd = scope.ServiceProvider.GetRequiredService(resCmd.CommandInfo.Type) as CommandBase;
// ... property injection ...
return new ActivationResult(cmd, resCmd, scope);  // Pass scope to result
```

---

### Step 3: Update `ExecuteLocalCommand` to Dispose Activation

**File:** [CommandLineApplicationCore.cs](../../BitPantry.CommandLine/Processing/Execution/CommandLineApplicationCore.cs) line 227

**Before:**
```csharp
private async Task<RunResult> ExecuteLocalCommand(ResolvedCommand rsCmd, object input)
{
    var activation = _activator.Activate(rsCmd);
    activation.Command.SetConsole(_console);
    
    // ... execute ...
    
    return new RunResult { Result = output };
}
```

**After:**
```csharp
private async Task<RunResult> ExecuteLocalCommand(ResolvedCommand rsCmd, object input)
{
    using var activation = _activator.Activate(rsCmd);  // Dispose after execution
    activation.Command.SetConsole(_console);
    
    // ... execute ...
    
    return new RunResult { Result = output };
}  // activation.Dispose() called → scope disposed → injected dependencies cleaned up
```

---

### Step 4: Update `AutoCompleteOptionSetBuilder` (2 locations)

These are different — they resolve a command just to invoke an autocomplete method, not full execution. The scope should wrap only the method invocation.

**File:** [AutoCompleteOptionSetBuilder.cs](../../BitPantry.CommandLine/AutoComplete/AutoCompleteOptionSetBuilder.cs) lines 257 and 568

**Before (line 257):**
```csharp
var cmd = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService(cmdInfo.Type);
var method = cmdInfo.Type.GetMethod(argInfo.AutoCompleteFunctionName);
var args = new[] { autoCompleteCtx };

results = await (argInfo.IsAutoCompleteFunctionAsync
    ? (Task<List<AutoCompleteOption>>)method.Invoke(cmd, args)
    : Task.Factory.StartNew(() => (List<AutoCompleteOption>)method.Invoke(cmd, args)));
```

**After:**
```csharp
using var scope = _serviceProvider.CreateScope();
var cmd = scope.ServiceProvider.GetRequiredService(cmdInfo.Type);
var method = cmdInfo.Type.GetMethod(argInfo.AutoCompleteFunctionName);
var args = new[] { autoCompleteCtx };

results = await (argInfo.IsAutoCompleteFunctionAsync
    ? (Task<List<AutoCompleteOption>>)method.Invoke(cmd, args)
    : Task.Factory.StartNew(() => (List<AutoCompleteOption>)method.Invoke(cmd, args)));
// scope disposed here
```

Apply same pattern at line 568.

---

### Step 5: Update `ServerLogic.AutoComplete`

**File:** [ServerLogic.cs](../../BitPantry.CommandLine.Remote.SignalR.Server/ServerLogic.cs) line 126

**Before:**
```csharp
var cmd = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService(cmdInfo.Type);
var method = cmdInfo.Type.GetMethod(req.FunctionName);
// ... invoke ...
```

**After:**
```csharp
using var scope = _serviceProvider.CreateScope();
var cmd = scope.ServiceProvider.GetRequiredService(cmdInfo.Type);
var method = cmdInfo.Type.GetMethod(req.FunctionName);
// ... invoke ...
// scope disposed here
```

---

## Summary of Changes

| File | Change |
|------|--------|
| `ActivationResult.cs` | Implement `IDisposable`, hold `IServiceScope`, dispose in `Dispose()` |
| `CommandActivator.cs` | Store scope, pass to `ActivationResult` |
| `CommandLineApplicationCore.cs` | Add `using` to activation |
| `AutoCompleteOptionSetBuilder.cs` (×2) | Wrap scope creation in `using` |
| `ServerLogic.cs` | Wrap scope creation in `using` |

---

## Testing

- [ ] Create test command that injects `IDisposable` service
- [ ] Verify `Dispose()` is called on injected service after command execution
- [ ] Verify `Dispose()` is called after autocomplete function invocation
- [ ] Run existing test suite to ensure no regressions

---

## Action Items (If Implementing)

- [ ] Add `IServiceScopeFactory` to `CommandActivator` or executor
- [ ] Refactor to manage scope around full command lifecycle
- [ ] Update SignalR server execution paths
- [ ] Update `AutoCompleteOptionSetBuilder` for consistency
- [ ] Add tests verifying `IDisposable` commands are properly disposed
