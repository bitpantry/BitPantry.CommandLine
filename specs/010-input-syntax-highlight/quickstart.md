# Quickstart: Input Syntax Highlighting

**Feature**: 010-input-syntax-highlight

## Overview

This feature adds real-time syntax highlighting to command line input, colorizing text as the user types based on semantic token classification.

## Key Components

### New Files

| File | Location | Purpose |
|------|----------|---------|
| SyntaxColorScheme.cs | BitPantry.CommandLine/AutoComplete/ | Centralized color definitions |
| SyntaxHighlighter.cs | BitPantry.CommandLine/Input/ | Classifies tokens and produces colored segments |
| ColoredSegment.cs | BitPantry.CommandLine/Input/ | Immutable segment record |
| TokenMatchResolver.cs | BitPantry.CommandLine/Input/ | Determines unique vs ambiguous matches |
| SyntaxHighlighterTests.cs | BitPantry.CommandLine.Tests/Input/ | Unit tests |

### Modified Files

| File | Changes |
|------|---------|
| ConsoleLineMirror.cs | Add `RenderWithStyles(List<ColoredSegment>)` method |
| InputBuilder.cs | Integrate highlighting in keystroke handler |
| GhostTextController.cs | Use `SyntaxColorScheme.GhostText` |
| AutoCompleteMenuRenderer.cs | Use `SyntaxColorScheme` for colors |

## Color Scheme

```csharp
public static class SyntaxColorScheme
{
    public static Style Group { get; } = Style.Parse("cyan");
    public static Style Command { get; } = Style.Parse("default");
    public static Style ArgumentName { get; } = Style.Parse("yellow");
    public static Style ArgumentAlias { get; } = Style.Parse("yellow");
    public static Style ArgumentValue { get; } = Style.Parse("purple");
    public static Style GhostText { get; } = Style.Parse("dim");
    public static Style Default { get; } = Style.Parse("default");
}
```

## Implementation Order

1. **SyntaxColorScheme** - No dependencies, enables other work
2. **ColoredSegment** - Simple record type
3. **TokenMatchResolver** - Uses ICommandRegistry
4. **SyntaxHighlighter** - Uses above components
5. **ConsoleLineMirror.RenderWithStyles** - Uses ColoredSegment
6. **InputBuilder integration** - Final wiring
7. **Refactor existing** - GhostTextController, AutoCompleteMenuRenderer

## Testing

Run tests: `dotnet test --filter "SyntaxHighlighter"`

Key test scenarios:
- Empty input → empty segments
- Group name → cyan segment
- Partial unique match → correct color
- Ambiguous partial → default color
- Full command with args → multiple colored segments
