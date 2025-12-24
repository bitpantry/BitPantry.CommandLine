# Data Model: Positional Arguments

**Feature**: 004-positional-arguments  
**Date**: December 24, 2025

## Overview

This document defines the data structures and their relationships for the positional arguments feature. All entities are extensions to existing classes unless marked as NEW.

---

## 1. ArgumentAttribute (Extended)

**File**: `BitPantry.CommandLine/API/ArgumentAttribute.cs`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Name | string | null | Custom argument name (existing) |
| AutoCompleteFunctionName | string | null | Auto-complete function name (existing) |
| IsRequired | bool | false | Whether argument is required (existing) |
| **Position** | int | -1 | **NEW**: Positional order (-1 = named, 0+ = positional) |
| **IsRest** | bool | false | **NEW**: Captures remaining positional tokens |

**Validation Rules**:
- `Position` must be >= -1
- If `IsRest = true`, `Position` must be >= 0
- If `IsRest = true`, property type must be array or `ICollection<T>`

---

## 2. ArgumentInfo (Extended)

**File**: `BitPantry.CommandLine/Component/ArgumentInfo.cs`

| Property | Type | Description |
|----------|------|-------------|
| Name | string | Argument name (existing) |
| Alias | char | Single-char alias (existing) |
| Description | string | Help text (existing) |
| PropertyInfo | SerializablePropertyInfo | Reflection info (existing) |
| AutoCompleteFunctionName | string | Completion function (existing) |
| IsAutoCompleteFunctionAsync | bool | Async flag (existing) |
| IsRequired | bool | Required flag (existing) |
| **Position** | int | **NEW**: Positional order (-1 = named) |
| **IsRest** | bool | **NEW**: Rest argument flag |
| **IsPositional** | bool | **NEW**: Computed: `Position >= 0` |
| **IsCollection** | bool | **NEW**: Computed: property type is array/ICollection |

**Serialization**: All new properties marked with `[JsonInclude]` for remote command support.

---

## 3. CommandElementType (Extended)

**File**: `BitPantry.CommandLine/Processing/Parsing/ParsedCommandElement.cs`

```csharp
public enum CommandElementType
{
    Command,           // Command name (existing)
    ArgumentName,      // --argName (existing)
    ArgumentAlias,     // -a (existing)
    ArgumentValue,     // Value after named arg (existing)
    Empty,             // Whitespace (existing)
    Unexpected,        // Unrecognized (existing)
    PositionalValue,   // NEW: Bare value in positional region
    EndOfOptions       // NEW: The -- separator token
}
```

---

## 4. ArgumentValues (NEW)

**File**: `BitPantry.CommandLine/Processing/Resolution/ArgumentValues.cs`

**Purpose**: Wrapper for single or multiple argument values in InputMap.

| Property | Type | Description |
|----------|------|-------------|
| IsSingleValue | bool | True if contains exactly one value |
| SingleValue | ParsedCommandElement | The single value (null if multi) |
| MultipleValues | IReadOnlyList<ParsedCommandElement> | All values (empty if single) |
| Values | IReadOnlyList<ParsedCommandElement> | All values (single wrapped in list) |

**Factory Methods**:
- `static ArgumentValues Single(ParsedCommandElement element)`
- `static ArgumentValues Multiple(IEnumerable<ParsedCommandElement> elements)`
- `ArgumentValues Append(ParsedCommandElement element)` - Returns new instance with added value

---

## 5. ResolvedCommand (Modified)

**File**: `BitPantry.CommandLine/Processing/Resolution/ResolvedCommand.cs`

**Change**: Replace `InputMap` type.

| Property | Type (Before) | Type (After) |
|----------|---------------|--------------|
| InputMap | `Dictionary<ArgumentInfo, ParsedCommandElement>` | `Dictionary<ArgumentInfo, ArgumentValues>` |

**Backward Compatibility**: Existing code accessing `InputMap[arg].Value` changes to `InputMap[arg].SingleValue.Value` or uses `InputMap[arg].Values` for iteration.

---

## 6. CommandResolutionErrorType (Extended)

**File**: `BitPantry.CommandLine/Processing/Resolution/CommandResolutionErrorType.cs`

```csharp
public enum CommandResolutionErrorType
{
    // Existing
    UnknownArgument,
    DuplicateArgument,
    OptionWithValue,
    
    // NEW
    MissingRequiredPositional,    // Required positional not provided
    ExcessPositionalValues,       // More positional values than arguments (no IsRest)
    DuplicateScalarArgument       // Repeated option for non-collection type
}
```

---

## 7. AutoCompleteContext (Extended)

**File**: `BitPantry.CommandLine/AutoComplete/AutoCompleteContext.cs`

| Property | Type | Description |
|----------|------|-------------|
| QueryString | string | Current partial input (existing) |
| Values | `Dictionary<ArgumentInfo, string>` | Single values (existing, for compat) |
| **AllValues** | `IReadOnlyDictionary<ArgumentInfo, IReadOnlyList<string>>` | **NEW**: All values including multi-value |
| **CurrentPositionalIndex** | int? | **NEW**: Which positional slot is being completed (null if not positional) |

---

## 8. Validation Error Types (NEW)

**File**: `BitPantry.CommandLine/Commands/PositionalArgumentValidationException.cs`

**Purpose**: Thrown at registration time for invalid positional configurations.

| Property | Type | Description |
|----------|------|-------------|
| CommandType | Type | The command class with invalid config |
| ArgumentName | string | The argument with the issue |
| ValidationRule | string | Which rule was violated |
| Message | string | Descriptive error message |

---

## Entity Relationships

```
┌─────────────────────┐
│  ArgumentAttribute  │ ← Defined by implementer on command properties
└─────────┬───────────┘
          │ Reflection
          ▼
┌─────────────────────┐
│    ArgumentInfo     │ ← Metadata stored in CommandInfo.Arguments
└─────────┬───────────┘
          │ Resolution
          ▼
┌─────────────────────┐
│   ArgumentValues    │ ← Wrapper in ResolvedCommand.InputMap
└─────────┬───────────┘
          │ Activation
          ▼
┌─────────────────────┐
│  Command Property   │ ← Value set on instantiated command
└─────────────────────┘
```

---

## State Transitions

### Positional Value Classification

```
Input Token
    │
    ├─► Starts with "--" AND not bare "--"
    │       → ArgumentName
    │
    ├─► Is bare "--"
    │       → EndOfOptions (consumed, enables positional mode for dash-prefixed values)
    │
    ├─► Starts with "-" (single dash, not "--")
    │       → ArgumentAlias
    │
    ├─► Previous element is ArgumentName/ArgumentAlias
    │       → ArgumentValue (paired with previous)
    │
    ├─► No named argument seen yet (in positional region)
    │       → PositionalValue
    │
    └─► After named argument started (past positional region)
        → Unexpected (or ArgumentValue if following a named arg)
```

### Argument Validation Flow

```
Command Registration
    │
    ├─► Extract ArgumentInfo from properties
    │
    ├─► Collect positional args (Position >= 0)
    │
    ├─► Validate: positions contiguous?
    │       NO → Error: "Non-contiguous position indices"
    │
    ├─► Validate: no duplicate positions?
    │       NO → Error: "Duplicate position {N}"
    │
    ├─► Find IsRest argument (if any)
    │
    ├─► Validate: only one IsRest?
    │       NO → Error: "Multiple IsRest arguments"
    │
    ├─► Validate: IsRest is collection type?
    │       NO → Error: "IsRest requires collection type"
    │
    ├─► Validate: IsRest is positional?
    │       NO → Error: "IsRest requires Position >= 0"
    │
    ├─► Validate: IsRest has highest position?
    │       NO → Error: "IsRest must be last positional"
    │
    └─► Registration succeeds
```

---

## Collection Type Detection

**Logic for `IsCollection` computed property**:

```csharp
bool IsCollection(Type type) =>
    type.IsArray ||
    (type.IsGenericType && 
     typeof(ICollection<>).IsAssignableFrom(type.GetGenericTypeDefinition()));
```

**Element Type Extraction**:

```csharp
Type GetElementType(Type collectionType) =>
    collectionType.IsArray 
        ? collectionType.GetElementType()
        : collectionType.GetGenericArguments()[0];
```
