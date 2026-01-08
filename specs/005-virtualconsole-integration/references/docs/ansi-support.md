# ANSI Escape Sequence Support

BitPantry.VirtualConsole processes ANSI escape sequences to accurately simulate terminal behavior. This document lists all supported sequences.

## Notation

- `ESC` = `\e` = `\x1b` = `\u001b` (escape character, decimal 27)
- `CSI` = `ESC [` (Control Sequence Introducer)
- `n` = numeric parameter (default values noted)

## Cursor Movement

| Sequence | Name | Description |
|----------|------|-------------|
| `CSI n A` | CUU - Cursor Up | Move cursor up n rows (default 1) |
| `CSI n B` | CUD - Cursor Down | Move cursor down n rows (default 1) |
| `CSI n C` | CUF - Cursor Forward | Move cursor right n columns (default 1) |
| `CSI n D` | CUB - Cursor Back | Move cursor left n columns (default 1) |
| `\r` | CR - Carriage Return | Move cursor to column 0 of current row |
| `\n` | LF - Line Feed | Move cursor to next row |

### Examples

```
\e[5A     → Move cursor up 5 rows
\e[A      → Move cursor up 1 row (default)
\e[10C    → Move cursor right 10 columns
\r        → Move to start of line
```

## Text Styling (SGR - Select Graphic Rendition)

Format: `CSI n m` where `n` is the attribute code.

### Attribute Codes

| Code | Attribute |
|------|-----------|
| 0 | Reset all attributes to default |
| 1 | Bold |
| 2 | Dim/Faint |
| 3 | Italic |
| 4 | Underline |
| 5 | Blink (slow) |
| 7 | Invert (reverse video) |
| 8 | Hidden |
| 9 | Strikethrough |

### Reset Specific Attributes

| Code | Effect |
|------|--------|
| 22 | Normal intensity (not bold/dim) |
| 23 | Not italic |
| 24 | Not underlined |
| 25 | Not blinking |
| 27 | Not inverted |
| 28 | Not hidden |
| 29 | Not strikethrough |

### Foreground Colors (Basic)

| Code | Color |
|------|-------|
| 30 | Black |
| 31 | Red |
| 32 | Green |
| 33 | Yellow |
| 34 | Blue |
| 35 | Magenta |
| 36 | Cyan |
| 37 | White |
| 39 | Default foreground |

### Background Colors (Basic)

| Code | Color |
|------|-------|
| 40 | Black |
| 41 | Red |
| 42 | Green |
| 43 | Yellow |
| 44 | Blue |
| 45 | Magenta |
| 46 | Cyan |
| 47 | White |
| 49 | Default background |

### Extended Colors (256-color mode)

| Format | Description |
|--------|-------------|
| `38;5;n` | Set foreground to 256-color palette index n |
| `48;5;n` | Set background to 256-color palette index n |

The 256-color palette:
- 0-7: Standard colors (same as 30-37)
- 8-15: High-intensity colors
- 16-231: 6×6×6 color cube
- 232-255: Grayscale

### Examples

```
\e[34m        → Blue foreground
\e[1;31m      → Bold red
\e[38;5;12m   → 256-color blue (Spectre.Console uses this)
\e[0m         → Reset all styling
\e[7m         → Inverted (for selection highlighting)
```

## Erase Sequences

### Erase in Display (ED)

| Sequence | Effect |
|----------|--------|
| `CSI 0 J` | Clear from cursor to end of screen |
| `CSI 1 J` | Clear from start of screen to cursor |
| `CSI 2 J` | Clear entire screen |
| `CSI J` | Same as `CSI 0 J` |

### Erase in Line (EL)

| Sequence | Effect |
|----------|--------|
| `CSI 0 K` | Clear from cursor to end of line |
| `CSI 1 K` | Clear from start of line to cursor |
| `CSI 2 K` | Clear entire line |
| `CSI K` | Same as `CSI 0 K` |

## Not Supported (Out of Scope)

The following are intentionally not supported in the initial release:

- Cursor position save/restore (`CSI s`, `CSI u`)
- Cursor visibility (`CSI ?25h`, `CSI ?25l`)
- Scrolling regions (`CSI r`)
- Tab stops
- Character sets and G0/G1 switching
- Mouse tracking sequences
- Window manipulation sequences
- True color (24-bit) - may be added later

## Handling Unknown Sequences

When an unrecognized escape sequence is encountered:

1. The system **throws an exception** with details about the unrecognized sequence
2. The exception message includes the raw sequence bytes for debugging
3. Tests fail immediately rather than silently producing incorrect screen state

This strict behavior ensures:
- **Accuracy**: The screen buffer always represents the expected state
- **Test reliability**: Unknown sequences don't silently corrupt test results  
- **Clear feedback**: You know exactly which sequence needs to be added

### Adding Support for New Sequences

If your application uses sequences not yet supported:

1. Register a custom handler (see Extensibility documentation)
2. Or submit an issue/PR to add support to the core package
