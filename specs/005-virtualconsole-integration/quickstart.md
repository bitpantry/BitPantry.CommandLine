# Quickstart: VirtualConsole Integration

**Feature**: 005-virtualconsole-integration  
**Date**: 2026-01-08

## Prerequisites

- On `005-virtualconsole-integration` branch (based on rework)
- `origin/master` fetched with VirtualConsole projects

## Step 1: Cherry-Pick Core Projects

```bash
# Cherry-pick VirtualConsole and its tests from master
git checkout origin/master -- BitPantry.VirtualConsole BitPantry.VirtualConsole.Tests

# Cherry-pick documentation
git checkout origin/master -- Docs/VirtualConsole
```

## Step 2: Create VirtualConsole.Testing Project

```bash
# Create directory
mkdir BitPantry.VirtualConsole.Testing

# Extract general-purpose files only
git show origin/master:BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs > BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs
git show origin/master:BitPantry.VirtualConsole.Testing/VirtualConsoleAnsiAdapter.cs > BitPantry.VirtualConsole.Testing/VirtualConsoleAnsiAdapter.cs
git show origin/master:BitPantry.VirtualConsole.Testing/IKeyboardSimulator.cs > BitPantry.VirtualConsole.Testing/IKeyboardSimulator.cs
```

Create `BitPantry.VirtualConsole.Testing.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Description>Testing extensions for BitPantry.VirtualConsole - provides FluentAssertions extensions and Spectre.Console adapter.</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\BitPantry.VirtualConsole\BitPantry.VirtualConsole.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Spectre.Console" />
  </ItemGroup>
</Project>
```

## Step 3: Add Projects to Solution

```bash
dotnet sln add BitPantry.VirtualConsole/BitPantry.VirtualConsole.csproj
dotnet sln add BitPantry.VirtualConsole.Tests/BitPantry.VirtualConsole.Tests.csproj
dotnet sln add BitPantry.VirtualConsole.Testing/BitPantry.VirtualConsole.Testing.csproj
```

## Step 4: Verify Build and Tests

```bash
# Build solution
dotnet build

# Run VirtualConsole tests (should be 250 tests passing)
dotnet test BitPantry.VirtualConsole.Tests
```

## Step 5: Migrate Existing Tests

Update test projects to reference VirtualConsole.Testing:

```xml
<!-- In BitPantry.CommandLine.Tests.csproj -->
<ProjectReference Include="..\BitPantry.VirtualConsole.Testing\BitPantry.VirtualConsole.Testing.csproj" />

<!-- In BitPantry.CommandLine.Tests.Remote.SignalR.csproj -->
<ProjectReference Include="..\BitPantry.VirtualConsole.Testing\BitPantry.VirtualConsole.Testing.csproj" />
```

Migrate usages in each file:

```csharp
// Before:
using BitPantry.CommandLine.Tests.VirtualConsole;
var console = new VirtualAnsiConsole();
// ... use console ...
var output = console.Output;

// After:
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
var virtualConsole = new VirtualConsole(80, 24);
var console = new VirtualConsoleAnsiAdapter(virtualConsole);
// ... use console (same IAnsiConsole interface) ...
var content = virtualConsole.GetScreenContent();
// Or use FluentAssertions:
virtualConsole.Should().ContainText("expected");
```

## Step 6: Delete Old Infrastructure

After all tests pass with new infrastructure:

```bash
# Delete old VirtualConsole folder
rm -rf BitPantry.CommandLine.Tests/VirtualConsole/
```

## Step 7: Final Verification

```bash
# Run all tests
dotnet test

# Verify no old references remain
grep -r "VirtualAnsiConsole" --include="*.cs"
# Should return no matches (except possibly in git history)
```

## Usage Examples

### Basic Screen Verification

```csharp
[TestMethod]
public void Command_ShouldOutputExpectedText()
{
    var virtualConsole = new VirtualConsole(80, 24);
    var console = new VirtualConsoleAnsiAdapter(virtualConsole);
    
    // Run command that writes to console
    console.WriteLine("Hello, World!");
    
    // Assert on screen content
    virtualConsole.Should().ContainText("Hello, World!");
    virtualConsole.Should().HaveTextAt(0, 0, "Hello, World!");
}
```

### Color and Style Verification

```csharp
[TestMethod]
public void Error_ShouldBeDisplayedInRed()
{
    var virtualConsole = new VirtualConsole(80, 24);
    var console = new VirtualConsoleAnsiAdapter(virtualConsole);
    
    // Write red text
    console.MarkupLine("[red]Error: Something went wrong[/]");
    
    // Verify color
    var cell = virtualConsole.GetCell(0, 0);
    cell.Style.ForegroundColor.Should().Be(ConsoleColor.DarkRed);
}
```

### Cursor Position Verification

```csharp
[TestMethod]
public void Prompt_ShouldPositionCursorCorrectly()
{
    var virtualConsole = new VirtualConsole(80, 24);
    var console = new VirtualConsoleAnsiAdapter(virtualConsole);
    
    console.Write("> ");
    
    virtualConsole.Should().HaveCursorAt(0, 2);
}
```

## Troubleshooting

### Build Errors

- Ensure all 3 projects are added to solution
- Verify VirtualConsole.Testing.csproj has correct project reference path

### Test Failures After Migration

- Check that `VirtualConsole` dimensions (80, 24) are appropriate for test
- Verify `GetScreenContent()` vs `GetScreenText()` - content has line breaks

### Missing FluentAssertions

- Ensure test project references `BitPantry.VirtualConsole.Testing`
- Add `using BitPantry.VirtualConsole.Testing;` for `.Should()` extension
