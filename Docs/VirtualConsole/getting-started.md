# Getting Started with BitPantry.VirtualConsole

## Installation

### Current (Internal to BitPantry.CommandLine)

Reference the `BitPantry.VirtualConsole` project in your test project:

```xml
<ProjectReference Include="..\BitPantry.VirtualConsole\BitPantry.VirtualConsole.csproj" />
```

### Future (NuGet Package)

```bash
dotnet add package BitPantry.VirtualConsole
```

## Basic Usage

### Creating a Virtual Console

```csharp
using BitPantry.VirtualConsole;

// Default 80x25 dimensions
var console = new VirtualConsole();

// Custom dimensions
var wideConsole = new VirtualConsole(width: 120, height: 40);
```

### Writing Output

```csharp
// Plain text
console.Write("Hello, World!");

// Text with ANSI codes (e.g., from Spectre.Console)
console.Write("\e[34mBlue text\e[0m");

// Multiple writes accumulate naturally
console.Write("Line 1\n");
console.Write("Line 2\n");
```

### Querying Screen State

```csharp
// Get a specific row
var row = console.GetRow(0);
string text = row.Text;  // Plain text content

// Get a specific cell
var cell = console.GetCell(row: 0, column: 5);
char character = cell.Character;
CellStyle style = cell.Style;

// Check styling
bool isBlue = cell.Style.ForegroundColor == ConsoleColor.Blue;
bool isInverted = cell.Style.IsInverted;
```

### Cursor Position

```csharp
// Check current cursor position
int cursorRow = console.CursorRow;
int cursorColumn = console.CursorColumn;
```

## Building Your Own Assertions

VirtualConsole provides data - you build assertions in your test project:

### With FluentAssertions (Example)

```csharp
// In your test project, create extension methods:
public static class VirtualConsoleAssertions
{
    public static void ShouldContainHighlighted(
        this ScreenRow row, 
        string text, 
        ConsoleColor color)
    {
        // Use the query API to check
        var content = row.Text;
        content.Should().Contain(text);
        
        var startIndex = content.IndexOf(text);
        for (int i = 0; i < text.Length; i++)
        {
            var cell = row.GetCell(startIndex + i);
            cell.Style.ForegroundColor.Should().Be(color,
                $"character '{text[i]}' at position {startIndex + i} should be {color}");
        }
    }
}

// Usage in tests:
console.GetRow(0).ShouldContainHighlighted("conn", ConsoleColor.Blue);
```

### Simple Assert (No Extensions Needed)

```csharp
// Direct API usage works with any test framework
var row = console.GetRow(menuRow);

// Check content
Assert.IsTrue(row.Text.Contains("connect"));

// Check that "conn" is highlighted in blue
var startIdx = row.Text.IndexOf("conn");
for (int i = 0; i < 4; i++)
{
    var cell = row.GetCell(startIdx + i);
    Assert.AreEqual(12, cell.Style.ForegroundColorCode); // 256-color blue
}
```

## Integration with Test Runners

### MSTest Example

```csharp
[TestClass]
public class MenuRenderingTests
{
    private VirtualConsole _console;

    [TestInitialize]
    public void Setup()
    {
        _console = new VirtualConsole(80, 25);
    }

    [TestMethod]
    public void Menu_ShouldHighlightFilterText()
    {
        // Arrange - render menu with filter
        RenderMenuTo(_console, filter: "conn");

        // Assert - query screen state and verify
        var row = _console.GetRow(1);
        Assert.IsTrue(row.Text.Contains("conn"));
        
        // Check styling using the query API
        var idx = row.Text.IndexOf("conn");
        var cell = row.GetCell(idx);
        Assert.AreEqual(12, cell.Style.ForegroundColorCode); // Blue
    }
}
```

## Next Steps

- [Screen Buffer](screen-buffer.md) - Understand how the buffer works
- [ANSI Support](ansi-support.md) - See all supported escape sequences
- [Examples](examples.md) - More usage patterns
