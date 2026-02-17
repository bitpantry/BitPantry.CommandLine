# BitPantry.CommandLine Documentation — Draft Page Tree

> Each line represents a documentation page. Indentation shows parent/child nesting. Filenames are slugified from titles. This tree will be used to generate the actual page files.

```
docs/
├── index.md                                    # Introduction & Overview
│   ├── architecture.md                         # Solution Architecture
│   └── quick-start.md                          # Getting Started — Quick Start Guide
│
├── commands/
│   ├── index.md                                # Defining Commands
│   ├── naming.md                               # Command Naming & the [Command] Attribute
│   ├── arguments.md                            # Arguments — [Argument], [Alias], [Description]
│   ├── positional-arguments.md                 # Positional Arguments
│   ├── flags.md                                # Flags — [Flag]
│   ├── groups.md                               # Command Groups — [Group] and [InGroup<T>]
│   └── error-handling.md                       # Error Handling — Fail(), CommandFailedException, IUserFacingException
│
├── building/
│   ├── index.md                                # Building the Application
│   ├── registering-commands.md                 # Registering Commands
│   ├── dependency-injection.md                 # Dependency Injection
│   ├── console-configuration.md                # Console Configuration
│   ├── prompt-configuration.md                 # Prompt Configuration
│   └── theme-configuration.md                  # Theme Configuration
│
├── running/
│   ├── index.md                                # Running Commands
│   ├── global-arguments.md                     # Global Arguments
│   ├── processing-pipeline.md                  # The Processing Pipeline
│   ├── piping.md                               # Command Piping
│   └── help-system.md                          # Help System
│
├── autocomplete/
│   ├── index.md                                # Autocomplete
│   ├── built-in-handlers.md                    # Built-in Handlers
│   ├── attribute-handlers.md                   # Custom Attribute Handlers — [AutoComplete<T>]
│   ├── type-handlers.md                        # Custom Type Handlers — ITypeAutoCompleteHandler
│   ├── rendering.md                            # Ghost Text & Menu Rendering
│   └── remote-autocomplete.md                  # Remote Autocomplete
│
├── syntax-highlighting.md                      # Syntax Highlighting
│
├── remote/
│   ├── index.md                                # Remote Execution (SignalR)
│   ├── shared-protocol.md                      # Shared Protocol — BitPantry.CommandLine.Remote.SignalR
│   ├── server/
│   │   ├── index.md                            # Setting Up the Server
│   │   ├── authentication.md                   # Server Authentication
│   │   └── sandboxing.md                       # Server File System & Sandboxing
│   ├── client/
│   │   ├── index.md                            # Setting Up the Client
│   │   ├── connecting.md                       # Connecting & Disconnecting
│   │   ├── auto-connect.md                     # Auto-Connect (Single-Command Mode)
│   │   ├── profiles.md                         # Server Profiles
│   │   └── file-transfers.md                   # File Transfers — Upload & Download
│   ├── server-proxy.md                         # The IServerProxy Interface
│   ├── rpc.md                                  # RPC Communication Pattern
│   └── remote-console-io.md                    # Remote Console I/O
│
├── virtual-console/
│   ├── index.md                                # BitPantry.VirtualConsole (Companion Package)
│   └── testing-extensions.md                   # BitPantry.VirtualConsole.Testing (Companion Package)
│
├── testing/
│   ├── index.md                                # Testing Guide
│   ├── unit-testing.md                         # Unit Testing Commands
│   ├── integration-testing.md                  # Integration Testing with TestEnvironment
│   └── ux-testing.md                           # UX Testing with VirtualConsole
│
└── api-reference/
    ├── index.md                                # API Reference
    ├── attributes.md                           # Core Attributes
    ├── builder-api.md                          # Builder API
    ├── component-model.md                      # Component Model
    └── interfaces.md                           # Interfaces
```

**Page count:** 46 pages total (13 section index pages + 33 content pages)
