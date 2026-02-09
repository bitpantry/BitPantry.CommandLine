# BitPantry.CommandLine Documentation — Draft Outline

> This is a working draft of the documentation outline. Each top-level bullet represents a page. Nested bullets represent sub-topic pages. We'll iterate on this before building actual pages.

---

- **Introduction & Overview**
  _High-level introduction to the BitPantry.CommandLine framework — what it is, what problems it solves, and who it's for. Covers the key value propositions: attribute-driven command definitions, built-in autocomplete, syntax highlighting, remote server execution over SignalR, and dependency injection throughout. Describes the NuGet package topology (core, shared SignalR, client, server) and how they relate._

  - **Solution Architecture**
    _Visual map of the projects in the solution, their dependencies, and the boundaries between them. Explains the layered design: core library → shared SignalR protocol → client/server packages. Covers the role of `BitPantry.Parsing.Strings` and `Spectre.Console` as foundational dependencies. Briefly introduces the VirtualConsole packages as complementary testing tools._

  - **Getting Started — Quick Start Guide**
    _Minimal working example: install the NuGet package, define a command class, build and run the application. Shows the `CommandLineApplicationBuilder` → `Build()` → `Run()` flow. Includes a simple command with one argument to get the reader productive immediately._

---

- **Defining Commands**
  _Core page covering how to create commands by extending `CommandBase` and using API attributes. Explains the `Execute` method signature (sync and async), the role of `CommandExecutionContext` / `CommandExecutionContext<T>`, and how return values work._

  - **Command Naming & the `[Command]` Attribute**
    _How command names are derived (from class name by default, or explicit via `[Command(Name = "...")]`). Valid name patterns (C# identifiers, kebab-case). Case sensitivity configuration._

  - **Arguments — `[Argument]`, `[Alias]`, `[Description]`**
    _How to define named arguments with `[Argument]`, set short aliases with `[Alias('x')]`, and document them with `[Description]`. Covers required vs optional arguments (`IsRequired`), default values, and supported data types (string parsing via `BitPantry.Parsing.Strings`)._

  - **Positional Arguments**
    _Using `[Argument(Position = 0)]` to define positional arguments. Rules: contiguous zero-based positions, no gaps. The `IsRest` property for variadic capture into collections. Interaction with named arguments._

  - **Flags — `[Flag]`**
    _Difference between flag booleans and value booleans. `[Flag]` makes a bool argument presence-only (e.g., `--verbose` → true, absent → false). Flags cannot accept values. Comparison with non-flag booleans that require explicit `true`/`false`._

  - **Command Groups — `[Group]` and `[InGroup<T>]`**
    _Organizing commands into hierarchical groups using marker classes with `[Group]`. Assigning commands to groups with `[InGroup<T>]`. Nested groups via nested marker classes. Group naming, descriptions, and how group paths are constructed (space-separated invocation syntax like `server profile add`)._

  - **Error Handling — `Fail()`, `CommandFailedException`, `IUserFacingException`**
    _How commands signal failures using the `Fail()` helper method on `CommandBase`. The `CommandFailedException` class and `IUserFacingException` marker interface for exceptions whose messages are safe to display to end users, including over remote connections._

---

- **Building the Application**
  _Covers `CommandLineApplicationBuilder` — the fluent builder API for configuring and constructing a `CommandLineApplication`. Registering commands, configuring services, and the `Build()` method._

  - **Registering Commands**
    _How to register commands individually (`RegisterCommand<T>()`), or by assembly scan (`RegisterCommands(typeof(SomeCommand))`). Duplicate handling with `ReplaceDuplicateCommands`. Auto-registration of groups when commands with `[InGroup]` are discovered._

  - **Dependency Injection**
    _Accessing `builder.Services` (the `IServiceCollection`) to register application services. Commands are resolved from the DI container, so constructor injection works naturally. Explains the service lifetime of commands (scoped per execution)._

  - **Console Configuration**
    _`UsingConsole()` for providing a custom `IAnsiConsole` (Spectre.Console). The `IConsoleService` abstraction for low-level console operations. The `IFileSystem` abstraction (System.IO.Abstractions) and `UsingFileSystem()`._

  - **Prompt Configuration**
    _`ConfigurePrompt()` to set the application name and suffix. Spectre.Console markup support in prompt values. The `IPromptSegment` interface and `CompositePrompt` model for extensible, multi-segment prompts (e.g., app name + server connection indicator + profile name). Segment ordering conventions._

  - **Theme Configuration**
    _`ConfigureTheme()` and the `Theme` class for customizing syntax highlighting colors, ghost text style, and autocomplete menu appearance. Lists all configurable style properties (Group, Command, ArgumentName, ArgumentAlias, ArgumentValue, GhostText, Default, MenuHighlight, MenuGroup)._

---

- **Running Commands**
  _How the application processes input at runtime. The REPL loop (`app.Run()`), single-command execution (`app.Run("command ...")`), and script execution from files. The `RunResult` and `RunResultCode` enum._

  - **The Processing Pipeline**
    _Detailed walkthrough of Input → Parsing → Resolution → Activation → Execution. How raw input strings become `ParsedInput`, then `ResolvedInput`, then activated `CommandBase` instances with injected argument values. The role of `CommandLineApplicationCore`._

  - **Command Piping**
    _Using the `|` pipe operator to chain commands. How `CommandExecutionContext<T>` receives the output of the previous command. Pipeline data type validation. Error propagation through the pipeline._

  - **Help System**
    _Built-in `--help` / `-h` flag handling at command, group, and root levels. The `IHelpFormatter` interface for customizing help output. The default `HelpFormatter` implementation. Using `UseHelpFormatter<T>()` to plug in a custom formatter._

  - **Script Execution**
    _How passing a file path as input causes the application to read and execute each line as a command sequentially. Error handling during script execution._

---

- **Autocomplete**
  _Overview of the autocomplete system — inline ghost text and dropdown menu suggestions. How autocomplete is triggered (Tab key), navigated (arrow keys), and accepted (Tab/Right Arrow). Covers command name completion, group name completion, argument name/alias completion, and argument value completion._

  - **Built-in Handlers**
    _The `EnumAutoCompleteHandler` (auto-suggests enum values for enum-typed arguments) and `BooleanAutoCompleteHandler` (suggests `true`/`false` for non-flag bool arguments). These are registered automatically — no configuration required._

  - **Custom Attribute Handlers — `[AutoComplete<T>]`**
    _Creating a custom `IAutoCompleteHandler` and binding it to an argument via `[AutoComplete<THandler>]`. The `AutoCompleteContext` (query string, full input, cursor position, provided values, command info). DI registration of handler types. Attribute handlers override type handlers._

  - **Custom Type Handlers — `ITypeAutoCompleteHandler`**
    _Implementing `ITypeAutoCompleteHandler` with a `CanHandle(Type)` method for broad type-based matching. Registering via `ConfigureAutoComplete(ac => ac.Register<THandler>())`. Resolution priority: attribute handler > type handler > built-in handler._

  - **Ghost Text & Menu Rendering**
    _How the `GhostTextController` displays inline dim suggestions and the `AutoCompleteMenuController` renders dropdown menus. The `CursorContext` and `CursorContextResolver` that determine what kind of suggestion to show. Menu filtering behavior during typing._

  - **Remote Autocomplete**
    _When a command is remote (registered from a connected server), autocomplete requests are forwarded to the server via `IServerProxy.AutoComplete()`. The serialized `AutoCompleteContext` round-trip. Server-side handler resolution and execution._

---

- **Syntax Highlighting**
  _Real-time input coloring as the user types. How the `SyntaxHighlighter` tokenizes input and resolves each token against the command registry. Token types: groups, commands, argument names, argument aliases, argument values, unrecognized text. Integration with the `Theme` system._

---

- **Remote Execution (SignalR)**
  _High-level overview of the client/server remote execution architecture. The three NuGet packages involved (`BitPantry.CommandLine.Remote.SignalR`, `BitPantry.CommandLine.Remote.SignalR.Client`, `BitPantry.CommandLine.Remote.SignalR.Server`). How commands registered on the server appear transparently in the client's command registry._

  - **Shared Protocol — `BitPantry.CommandLine.Remote.SignalR`**
    _The shared library used by both client and server. Covers the envelope/message types (`ServerRequest`, `ResponseMessage`, `RunRequest`, `RunResponse`, `AutoCompleteRequest`, etc.), the RPC message registry (`RpcMessageRegistry`, `RpcMessageContext`, `IRpcScope`), JSON serialization options (`RemoteJsonOptions`, custom converters), and the `SignalRMethodNames` / `ServiceEndpointNames` constants._

  - **Setting Up the Server**
    _Configuring an ASP.NET application as a CommandLine server using `services.AddCommandLineHub()` and `app.ConfigureCommandLineHub()`. The `CommandLineServerOptions` for hub URL pattern, file transfer options, and command registration. How `ServerLogic` processes incoming client requests by constructing a transient `CommandLineApplicationCore` and executing commands server-side._

    - **Server Authentication**
      _JWT token-based authentication flow. The `IApiKeyStore` and `IRefreshTokenStore` interfaces that the developer must implement. API key → access token exchange via `TokenRequestEndpointService`. Token refresh mechanism. `TokenValidationMiddleware` and `JwtAuthOptions` configuration._

    - **Server File System & Sandboxing**
      _The `SandboxedFileSystem` that confines all file operations to a configured `StorageRootPath`. `PathValidator` for path traversal protection. `FileSizeValidator` and `ExtensionValidator` for upload restrictions. How commands using `IFileSystem` work identically in local and sandboxed modes (`FileTransferOptions` configuration)._

  - **Setting Up the Client**
    _Configuring the client with `builder.ConfigureSignalRClient()`. The `SignalRClientOptions` for HTTP factories, token refresh intervals, and transport configuration. How client registration wires up `IServerProxy`, `AccessTokenManager`, `FileTransferService`, prompt segments, and the built-in `server` command group._

    - **Connecting & Disconnecting**
      _The `server connect` command — connecting by URI (with optional API key and token endpoint), or by profile name. The `server disconnect` command. How connection changes the command registry (remote commands are registered on connect, dropped on disconnect). The `ServerConnectionSegment` prompt indicator._

    - **Server Profiles**
      _Managing saved server connection profiles via `server profile add`, `list`, `show`, `remove`, `set-default`, and `set-key`. The `ProfileManager` with JSON configuration storage and encrypted credential storage (`CredentialStore` using DPAPI/Sodium). The `ProfilePromptSegment` that shows the connected profile name in the prompt._

    - **File Transfers — Upload & Download**
      _The `server upload` and `server download` commands. Glob pattern support (`GlobPatternHelper`). Concurrent transfers with throttling (max 4). Progress display for large files (≥ 25 MB threshold). Checksum verification. The `FileTransferService` for streaming HTTP uploads/downloads. Server-side endpoints and the `FileTransferEndpointService`._

  - **The `IServerProxy` Interface**
    _The abstraction at the core of remote execution. Methods: `Connect`, `Disconnect`, `Run`, `AutoComplete`, `SendRpcRequest`. The `ServerCapabilities` object (connection URI, connection ID, available commands, max file size). States: `Disconnected`, `Connected`, `Connecting`, `Reconnecting`. The `NoopServerProxy` default for local-only apps. The `SignalRServerProxy` implementation._

  - **RPC Communication Pattern**
    _How the bidirectional SignalR hub supports request/response messaging. The `RpcMessageRegistry` for correlating requests with responses via `CorrelationId`. Scoped message contexts. Server-to-client RPC (e.g., `ReadKey`, `IsKeyAvailable` for remote console I/O). Error propagation across the wire._

  - **Remote Console I/O**
    _`SignalRAnsiConsole` (server-side proxy that forwards ANSI output to the client) and `SignalRAnsiInput` (server-side proxy that requests keystrokes from the client). How Spectre.Console output is relayed transparently over the SignalR connection._

---

- **BitPantry.VirtualConsole** _(Companion Package)_
  _Introduction to the VirtualConsole — a virtual terminal emulator for testing CLI applications. Targets .NET Standard 2.0 with zero external dependencies. Processes ANSI escape sequences and maintains a 2D screen buffer (`ScreenBuffer`, `ScreenCell`, `ScreenRow`). Key features: cursor tracking, SGR style processing, erase modes, auto-wrap._

  - **BitPantry.VirtualConsole.Testing** _(Companion Package)_
    _The testing companion that bridges VirtualConsole with FluentAssertions and Spectre.Console. `VirtualConsoleAssertions` (`.Should().ContainText()`, `.NotContainText()`, `.HaveLineContaining()`, etc.). `VirtualConsoleAnsiAdapter` for plugging into `IAnsiConsole`. `KeyboardSimulator` and `TestConsoleInput` for simulating keyboard input in tests._

---

- **Testing Guide**
  _Strategies and infrastructure for testing CommandLine applications. Overview of the three test levels: unit/component (mocks), integration (real client-server), and UX/functional (VirtualConsole assertions)._

  - **Unit Testing Commands**
    _Testing commands in isolation using mocks (Moq) and a `MockFileSystem`. Building a minimal `CommandLineApplicationBuilder`, running input, and asserting on `RunResult`. Mocking `IServerProxy` for remote command tests._

  - **Integration Testing with `TestEnvironment`**
    _The `TestEnvironment` infrastructure for full client-server integration tests. In-memory ASP.NET `TestServer` (no network required). Configured SignalR client connected to test server. Test logger for inspecting client/server log output._

  - **UX Testing with VirtualConsole**
    _Using `VirtualConsole` + `VirtualConsoleAssertions` to test console output, prompt rendering, and progress display. `KeyboardSimulator` for driving interactive input sequences._

---

- **API Reference**
  _Comprehensive reference of all public types, organized by namespace/project. Intended as a lookup resource rather than narrative documentation._

  - **Core Attributes**
    _`[Command]`, `[Argument]`, `[Alias]`, `[Flag]`, `[Description]`, `[Group]`, `[InGroup<T>]`, `[AutoComplete<T>]` — full property reference with examples._

  - **Builder API**
    _`CommandLineApplicationBuilder` and `CommandLineServerOptions` — all configuration methods with signatures, descriptions, and examples._

  - **Component Model**
    _`CommandInfo`, `ArgumentInfo`, `GroupInfo`, `SerializablePropertyInfo` — the runtime metadata objects used throughout the framework._

  - **Interfaces**
    _`ICommandRegistry`, `IServerProxy`, `IHelpFormatter`, `IPromptSegment`, `IAutoCompleteHandler`, `ITypeAutoCompleteHandler`, `IConsoleService`, `IApiKeyStore`, `IRefreshTokenStore`._
