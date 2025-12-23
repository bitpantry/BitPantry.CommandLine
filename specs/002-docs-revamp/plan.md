# Implementation Plan: Documentation Revamp

**Branch**: `002-docs-revamp` | **Date**: 2024-12-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-docs-revamp/spec.md`

## Summary

Revamp BitPantry.CommandLine documentation to be comprehensive, well-organized for two audiences (implementers building CLI apps and end-users operating CLI apps), with cross-references, conceptual explanations, and working examples.

**Key Changes:**
- Create audience-specific landing pages (Implementer Guide, End-User Guide)
- Fill empty/stub documentation (AutoComplete.md, REPL.md, CommandBase.md, IAnsiConsole.md)
- Create missing referenced documentation (DependencyInjection.md)
- Document all built-in commands (`lc`, `server.connect`, `server.disconnect`)
- Document undocumented features (IsRequired, input history, prompt customization, logging)
- Add consistent navigation with cross-references and "See Also" sections
- Fix broken links and ensure 3-click navigation

## Technical Context

**Documentation Format**: GitHub-flavored Markdown  
**Hosting**: GitHub repository (consumed directly from repo)  
**Tone**: Technical tutorial, code-first, step-by-step  
**Languages**: English only  
**Versioning**: Latest version only (no version-specific docs)

**Existing Documentation Structure**:
```
Docs/
├── readme.md                    # Main entry/quick start
├── CommandLine/                 # Core library (16 files)
│   ├── Commands.md              # Comprehensive (★★★★★)
│   ├── CommandSyntax.md         # Comprehensive (★★★★★)
│   ├── CommandLineApplicationBuilder.md  # Good (★★★★☆)
│   ├── AutoComplete.md          # EMPTY (needs content)
│   ├── CommandBase.md           # Stub (5 lines)
│   ├── IAnsiConsole.md          # Stub (5 lines)
│   ├── REPL.md                  # Stub (6 lines)
│   └── ... (11 other files)
└── Remote/                      # SignalR (7 files)
    ├── CommandLineServer.md     # Good (★★★★☆)
    ├── Client.md                # Good (★★★★☆)
    └── ... (5 other files)
```

**Current Issues Identified**:
1. AutoComplete.md is completely empty
2. DependencyInjection.md is referenced but doesn't exist
3. CommandBase.md, IAnsiConsole.md, REPL.md are stubs (<10 lines)
4. No documentation for built-in commands (lc, server.connect, server.disconnect)
5. No documentation for: IsRequired property, input history, prompt customization, logging
6. No audience-specific navigation paths
7. Inconsistent cross-referencing

## Constitution Check

*Documentation-focused spec - code quality rules adapted for docs:*

- ✅ **Consistency**: All pages follow same structure (headings, code blocks, See Also)
- ✅ **Completeness**: Every feature has documentation coverage
- ✅ **Accuracy**: Code examples are working and match current codebase
- ✅ **Discoverability**: 3-click navigation from index to any topic
- ✅ **Two Audiences**: Separate paths for implementers and end-users

## Project Structure

### New Documentation Files

```text
Docs/
├── index.md                     # NEW: Main landing with audience paths
├── ImplementerGuide.md          # NEW: Developer-focused index
├── EndUserGuide.md              # NEW: CLI user-focused guide
├── CommandLine/
│   ├── AutoComplete.md          # EXPAND: Full autocomplete documentation
│   ├── CommandBase.md           # EXPAND: Protected members, console access
│   ├── IAnsiConsole.md          # EXPAND: Spectre.Console patterns
│   ├── REPL.md                  # EXPAND: History, prompt, keyboard shortcuts
│   ├── DependencyInjection.md   # NEW: DI patterns
│   ├── BuiltInCommands.md       # NEW: lc command documentation
│   └── Logging.md               # NEW: ILoggerFactory configuration
└── Remote/
    ├── BuiltInCommands.md       # NEW: server.connect, server.disconnect
    ├── SignalRClientOptions.md  # NEW: Client configuration reference
    └── Troubleshooting.md       # NEW: Common issues and solutions
```

### Modified Documentation Files

```text
Docs/
├── readme.md                    # MODIFY: Link to new index.md
├── CommandLine/
│   ├── Commands.md              # MODIFY: Add ToC, improve cross-refs
│   ├── CommandLineApplicationBuilder.md  # MODIFY: Add all options
│   ├── ArgumentInfo.md          # MODIFY: Document IsRequired property
│   └── QuickStart.md            # MODIFY: Add next steps
└── Remote/
    ├── CommandLineServer.md     # MODIFY: Add troubleshooting link
    └── Client.md                # MODIFY: Add options reference link
```

## Implementation Phases

### Phase 1: Documentation Infrastructure

**Goal**: Establish navigation structure and audience-specific landing pages

**1.1 Create Main Index**
- Create `Docs/index.md` as new landing page
- Include two clear paths: "For Implementers" and "For CLI Users"
- Link to both audience-specific guides
- Include overview of package structure

**1.2 Create Implementer Guide**
- Create `Docs/ImplementerGuide.md`
- Organize topics by learning progression:
  1. Getting Started (link to QuickStart.md)
  2. Defining Commands (link to Commands.md)
  3. Configuration (link to CommandLineApplicationBuilder.md)
  4. Advanced Topics (AutoComplete, DI, Logging)
  5. Remote CLI (link to Remote/ section)
- Each section with brief description and links

**1.3 Create End-User Guide**
- Create `Docs/EndUserGuide.md`
- Cover CLI operation without implementation details:
  1. Command Syntax (namespaces, arguments, aliases)
  2. REPL Features (history, autocomplete, keyboard shortcuts)
  3. Built-in Commands (`lc` command)
  4. Remote Connection (connect/disconnect)
- Standalone document - no code implementation examples

**1.4 Update readme.md**
- Add link to new `index.md`
- Keep quick start content
- Add "Documentation" section pointing to full docs

### Phase 2: Fill Empty/Stub Documentation

**Goal**: Ensure all existing documentation files have substantive content

> **Note**: AutoComplete.md was moved to Phase 7 (User Story 5) for better organization by user story.

**2.1 CommandBase.md (Currently 5 lines)**

Expand to cover:
- Purpose of CommandBase as required parent class
- Protected members available to commands:
  - `Console` property (IAnsiConsole)
  - Access to Spectre.Console features
- Console output patterns (Info, Warning, Error)
- Lifecycle of command instances
- Cross-references to IAnsiConsole.md, Commands.md

**2.2 IAnsiConsole.md (Currently 5 lines)**

Expand to cover:
- Overview of Spectre.Console integration
- How to access console in commands
- Common output patterns:
  - Text output (WriteLine, Markup)
  - Tables
  - Progress bars
  - Prompts
- Link to Spectre.Console documentation
- Cross-references to CommandBase.md

> **Note**: REPL.md expansion moved to Phase 5 (User Story 3 - End User Operates CLI).

### Phase 3: Create Missing Referenced Documentation

**Goal**: Create documentation that is referenced but doesn't exist

**3.1 DependencyInjection.md**

Create comprehensive DI documentation:
- Overview of DI in BitPantry.CommandLine
- Accessing `Services` on `CommandLineApplicationBuilder`
- Registering services for injection
- Constructor injection in commands
- Command lifecycle (transient by default)
- Service resolution patterns
- Code examples:
  - Registering a service
  - Injecting into a command
  - Using scoped services
- Cross-references to CommandLineApplicationBuilder.md, Commands.md

**3.2 Logging.md**

Create logging configuration documentation:
- Overview of logging support
- Configuring `ILoggerFactory` on builder
- Default logging behavior (`NullLoggerFactory`)
- Integration with Microsoft.Extensions.Logging
- Log levels and what gets logged
- Code examples:
  - Adding console logging
  - Adding file logging
  - Filtering log levels
- Cross-references to CommandLineApplicationBuilder.md

### Phase 4: Document Built-in Commands

**Goal**: Document all commands that ship with the packages

**4.1 CommandLine/BuiltInCommands.md**

Document the `lc` (ListCommands) command:
- Purpose: List all registered commands
- Syntax: `lc [-f|--filter <expression>]`
- Filter expressions (dynamic LINQ support)
- Output columns: Namespace, Name, IsRemote, Description, InputType, ReturnType
- Examples:
  - `lc` - list all commands
  - `lc -f "Name.Contains('user')"` - filter by name
  - `lc -f "Namespace == 'admin'"` - filter by namespace

**4.2 Remote/BuiltInCommands.md**

Document remote CLI commands:

`server.connect`:
- Purpose: Connect to remote CLI server
- Syntax: `server.connect <url> [--key <apiKey>]`
- Authentication flow
- Connection state management
- Examples

`server.disconnect`:
- Purpose: Disconnect from remote CLI server
- Syntax: `server.disconnect`
- What happens on disconnect
- Examples

### Phase 5: Document Undocumented Features

**Goal**: Ensure all features discovered in code analysis are documented

**5.1 Update ArgumentInfo.md**
- Add documentation for `IsRequired` property
- Explain validation behavior for required arguments
- Error messages when required arguments missing
- Examples of required vs optional arguments

**5.2 Update CommandLineApplicationBuilder.md**
- Document all builder options:
  - `WithConsole(IAnsiConsole)` - custom console
  - `WithConsoleService(IConsoleService)` - custom console service
  - `WithFileSystem(IFileSystem)` - custom file system
  - `ReplaceDuplicateRegisteredCommands(bool)` - duplicate handling
  - `Services` property - DI container access
- Add table of all configuration options with defaults

**5.3 Create Remote/SignalRClientOptions.md**
- Document all client options:
  - `TokenRefreshMonitorInterval` (default: 1 minute)
  - `TokenRefreshThreshold` (default: 5 minutes)
  - `HttpClientFactory` - for testing
  - `HttpMessageHandlerFactory` - for testing
- Configuration examples

**5.4 Create Remote/Troubleshooting.md**
- Common connection issues
- Authentication failures
- File transfer problems
- Debugging tips

### Phase 6: Navigation and Cross-References

**Goal**: Ensure consistent navigation and working cross-references

**6.1 Add Table of Contents**
Add ToC to long documents (>3 sections):
- Commands.md
- CommandLineApplicationBuilder.md
- CommandLineServer.md
- Client.md
- New AutoComplete.md
- New DependencyInjection.md

**6.2 Standardize "See Also" Sections**
Every documentation page should end with:
```markdown
## See Also
- [Related Topic 1](path/to/topic1.md)
- [Related Topic 2](path/to/topic2.md)
```

**6.3 Fix Broken Links**
- Audit all existing links
- Remove or fix references to DependencyInjection.md (now exists)
- Ensure all internal links use relative paths
- Verify all links resolve

**6.4 Add "Back to Index" Navigation**
Each page should include navigation breadcrumb or link back to index

### Phase 7: Consistency Pass

**Goal**: Ensure all documentation follows consistent patterns

**7.1 Heading Hierarchy**
Verify all pages use:
- H1 (`#`) for page title only
- H2 (`##`) for major sections
- H3 (`###`) for subsections

**7.2 Code Block Formatting**
Verify all code blocks use:
- Triple backticks with language identifier
- `csharp` for C# code
- `text` or `bash` for terminal output
- Consistent indentation

**7.3 Tone and Style**
Verify all pages maintain:
- Technical tutorial tone
- Code-first approach
- Step-by-step progression
- Active voice

## Documentation Inventory

### New Files (9 files)

| File | Phase | Words Target | Priority |
|------|-------|--------------|----------|
| `Docs/index.md` | 1.1 | 300-400 | P1 |
| `Docs/ImplementerGuide.md` | 1.2 | 500-700 | P1 |
| `Docs/EndUserGuide.md` | 1.3 | 800-1000 | P2 |
| `Docs/CommandLine/DependencyInjection.md` | 3.1 | 600-800 | P2 |
| `Docs/CommandLine/Logging.md` | 3.2 | 400-500 | P3 |
| `Docs/CommandLine/BuiltInCommands.md` | 4.1 | 400-500 | P2 |
| `Docs/Remote/BuiltInCommands.md` | 4.2 | 500-600 | P2 |
| `Docs/Remote/SignalRClientOptions.md` | 5.3 | 300-400 | P3 |
| `Docs/Remote/Troubleshooting.md` | 5.4 | 400-500 | P3 |

### Expanded Files (4 files)

| File | Phase | Current | Target Words |
|------|-------|---------|--------------|
| `Docs/CommandLine/AutoComplete.md` | 7 (US5) | 0 | 800-1000 |
| `Docs/CommandLine/CommandBase.md` | 2.1 | ~30 | 400-500 |
| `Docs/CommandLine/IAnsiConsole.md` | 2.2 | ~50 | 400-500 |
| `Docs/CommandLine/REPL.md` | 5 (US3) | ~40 | 500-600 |

### Modified Files (8 files)

| File | Phase | Changes |
|------|-------|---------|
| `Docs/readme.md` | 1.4 | Add docs link |
| `Docs/CommandLine/Commands.md` | 6.1 | Add ToC |
| `Docs/CommandLine/CommandLineApplicationBuilder.md` | 5.2, 6.1 | Add options, ToC |
| `Docs/CommandLine/ArgumentInfo.md` | 5.1 | Add IsRequired |
| `Docs/CommandLine/QuickStart.md` | 1.4 | Add next steps |
| `Docs/Remote/CommandLineServer.md` | 6.1 | Add ToC, troubleshooting link |
| `Docs/Remote/Client.md` | 6.1, 5.3 | Add ToC, options link |
| `Docs/Remote/JwtAuthOptions.md` | 6.2 | Add See Also |

## Existing Patterns Reference

### Documentation Page Structure (to follow)
```markdown
# Page Title
Brief introduction paragraph.

## Section 1
Content with code examples.

```csharp
// Code example with syntax highlighting
public class Example { }
```

## Section 2
More content.

### Subsection
Detailed content.

## See Also
- [Related Topic](path/to/related.md)
- [Another Topic](path/to/another.md)
```

### Code Example Pattern (to follow)
```markdown
```csharp
using BitPantry.CommandLine.API;

public class MyCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine("Example output");
    }
}
```
```

### Cross-Reference Pattern (to follow)
```markdown
See [Commands](Commands.md) for more information about defining commands.

Configure the builder using the [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md).
```

## Complexity Assessment

| Complexity Factor | Assessment |
|-------------------|------------|
| New content creation | Medium - 9 new files, ~5000 words total |
| Research required | Low - features exist in code, need documentation |
| Cross-referencing | Medium - 30+ documents to interconnect |
| Code example accuracy | Medium - all examples must be tested |
| Consistency enforcement | Low - patterns are established |

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Code examples become outdated | Include minimal examples that test core patterns; reference tests for complex examples |
| Navigation becomes complex | Limit depth to 3 levels; clear index pages |
| Inconsistent tone across new pages | Write all new pages in single phase; review pass at end |
| Broken links after reorganization | Run link checker before marking complete |
| Missing features in documentation | Cross-reference spec FR list against final docs |

## Validation Checklist

Before marking complete, verify:
- [ ] All 9 new files created with target word counts
- [ ] All 4 stub files expanded with target word counts
- [ ] All 8 modified files updated
- [ ] Every page has "See Also" section
- [ ] Every long page (>3 sections) has ToC
- [ ] Zero broken internal links
- [ ] Code examples use consistent formatting
- [ ] Heading hierarchy is consistent (H1 → H2 → H3)
- [ ] Both audience paths work (Implementer, End-User)
- [ ] 3-click navigation verified from index to any topic
