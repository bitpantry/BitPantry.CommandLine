# BitPantry.CommandLine Documentation — Cross-References (See Also)

> Draft mapping of "See Also" links for each documentation page.
> Each entry lists the related pages that should appear at the bottom of that page.

---

## Root

### `index.md` — Introduction & Overview
- architecture.md
- quick-start.md
- commands/index.md
- building/index.md

### `architecture.md` — Solution Architecture
- index.md
- quick-start.md
- building/index.md
- running/processing-pipeline.md
- remote/index.md

### `quick-start.md` — Getting Started
- index.md
- commands/index.md
- building/index.md
- running/index.md

---

## Commands

### `commands/index.md` — Defining Commands
- commands/naming.md
- commands/arguments.md
- commands/groups.md
- building/registering-commands.md
- quick-start.md

### `commands/naming.md` — Command Naming & [Command]
- commands/index.md
- commands/groups.md
- running/help-system.md
- api-reference/attributes.md

### `commands/arguments.md` — Arguments
- commands/positional-arguments.md
- commands/flags.md
- autocomplete/index.md
- running/global-arguments.md
- api-reference/attributes.md

### `commands/positional-arguments.md` — Positional Arguments
- commands/arguments.md
- commands/flags.md
- autocomplete/built-in-handlers.md
- api-reference/attributes.md

### `commands/flags.md` — Flags
- commands/arguments.md
- commands/positional-arguments.md
- api-reference/attributes.md

### `commands/groups.md` — Command Groups
- commands/index.md
- commands/naming.md
- building/registering-commands.md
- running/help-system.md
- api-reference/attributes.md

### `commands/error-handling.md` — Error Handling
- running/processing-pipeline.md
- running/index.md
- remote/remote-console-io.md
- api-reference/interfaces.md

---

## Building

### `building/index.md` — Building the Application
- building/registering-commands.md
- building/dependency-injection.md
- commands/index.md
- running/index.md
- api-reference/builder-api.md

### `building/registering-commands.md` — Registering Commands
- commands/index.md
- commands/groups.md
- building/dependency-injection.md
- building/index.md

### `building/dependency-injection.md` — Dependency Injection
- building/index.md
- building/registering-commands.md
- testing/unit-testing.md
- api-reference/builder-api.md

### `building/console-configuration.md` — Console Configuration
- building/index.md
- virtual-console/index.md
- syntax-highlighting.md
- remote/remote-console-io.md
- api-reference/interfaces.md

### `building/prompt-configuration.md` — Prompt Configuration
- building/index.md
- running/index.md
- autocomplete/index.md
- syntax-highlighting.md

### `building/theme-configuration.md` — Theme Configuration
- building/index.md
- syntax-highlighting.md
- autocomplete/rendering.md

---

## Running

### `running/index.md` — Running Commands
- running/global-arguments.md
- running/processing-pipeline.md
- building/index.md
- commands/index.md

### `running/global-arguments.md` — Global Arguments
- running/index.md
- running/processing-pipeline.md
- commands/arguments.md
- remote/client/auto-connect.md

### `running/processing-pipeline.md` — The Processing Pipeline
- running/index.md
- commands/error-handling.md
- architecture.md
- api-reference/component-model.md

### `running/piping.md` — Command Piping
- running/index.md
- running/processing-pipeline.md
- commands/arguments.md

### `running/help-system.md` — Help System
- running/index.md
- commands/naming.md
- commands/arguments.md
- commands/groups.md

---

## Autocomplete

### `autocomplete/index.md` — Autocomplete
- autocomplete/built-in-handlers.md
- autocomplete/attribute-handlers.md
- autocomplete/type-handlers.md
- autocomplete/rendering.md
- commands/arguments.md

### `autocomplete/built-in-handlers.md` — Built-in Handlers
- autocomplete/index.md
- autocomplete/attribute-handlers.md
- autocomplete/type-handlers.md

### `autocomplete/attribute-handlers.md` — Custom Attribute Handlers
- autocomplete/index.md
- autocomplete/built-in-handlers.md
- commands/arguments.md
- api-reference/attributes.md

### `autocomplete/type-handlers.md` — Custom Type Handlers
- autocomplete/index.md
- autocomplete/built-in-handlers.md
- api-reference/interfaces.md

### `autocomplete/rendering.md` — Ghost Text & Menu Rendering
- autocomplete/index.md
- building/theme-configuration.md
- syntax-highlighting.md

### `autocomplete/remote-autocomplete.md` — Remote Autocomplete
- autocomplete/index.md
- remote/index.md
- remote/server-proxy.md
- remote/rpc.md

---

## Syntax Highlighting

### `syntax-highlighting.md` — Syntax Highlighting
- building/theme-configuration.md
- building/console-configuration.md
- autocomplete/rendering.md
- building/prompt-configuration.md

---

## Remote Execution

### `remote/index.md` — Remote Execution (SignalR)
- remote/shared-protocol.md
- remote/server/index.md
- remote/client/index.md
- architecture.md

### `remote/shared-protocol.md` — Shared Protocol
- remote/index.md
- remote/rpc.md
- remote/server-proxy.md
- api-reference/component-model.md

### `remote/server/index.md` — Setting Up the Server
- remote/index.md
- remote/server/authentication.md
- remote/server/sandboxing.md
- remote/client/index.md

### `remote/server/authentication.md` — Server Authentication
- remote/server/index.md
- remote/client/connecting.md
- remote/client/profiles.md

### `remote/server/sandboxing.md` — Server File System & Sandboxing
- remote/server/index.md
- remote/client/file-transfers.md

### `remote/client/index.md` — Setting Up the Client
- remote/index.md
- remote/client/connecting.md
- remote/client/profiles.md
- remote/server/index.md

### `remote/client/connecting.md` — Connecting & Disconnecting
- remote/client/index.md
- remote/client/auto-connect.md
- remote/client/profiles.md
- remote/server/authentication.md
- remote/server-proxy.md

### `remote/client/auto-connect.md` — Auto-Connect (Single-Command Mode)
- remote/client/connecting.md
- remote/client/profiles.md
- running/global-arguments.md
- running/index.md

### `remote/client/profiles.md` — Server Profiles
- remote/client/index.md
- remote/client/connecting.md
- remote/client/auto-connect.md
- remote/server/authentication.md

### `remote/client/file-transfers.md` — File Transfers
- remote/client/index.md
- remote/server/sandboxing.md
- remote/server-proxy.md
- remote/rpc.md

### `remote/server-proxy.md` — The IServerProxy Interface
- remote/index.md
- remote/rpc.md
- remote/remote-console-io.md
- remote/client/connecting.md
- api-reference/interfaces.md

### `remote/rpc.md` — RPC Communication Pattern
- remote/index.md
- remote/shared-protocol.md
- remote/server-proxy.md

### `remote/remote-console-io.md` — Remote Console I/O
- remote/index.md
- remote/server-proxy.md
- building/console-configuration.md
- virtual-console/index.md
- commands/error-handling.md

---

## VirtualConsole

### `virtual-console/index.md` — BitPantry.VirtualConsole
- virtual-console/testing-extensions.md
- building/console-configuration.md
- testing/ux-testing.md
- remote/remote-console-io.md

### `virtual-console/testing-extensions.md` — BitPantry.VirtualConsole.Testing
- virtual-console/index.md
- testing/ux-testing.md
- testing/integration-testing.md

---

## Testing

### `testing/index.md` — Testing Guide
- testing/unit-testing.md
- testing/integration-testing.md
- testing/ux-testing.md
- virtual-console/index.md

### `testing/unit-testing.md` — Unit Testing Commands
- testing/index.md
- building/dependency-injection.md
- commands/index.md

### `testing/integration-testing.md` — Integration Testing
- testing/index.md
- testing/ux-testing.md
- building/index.md
- virtual-console/testing-extensions.md

### `testing/ux-testing.md` — UX Testing with VirtualConsole
- testing/index.md
- testing/integration-testing.md
- virtual-console/index.md
- virtual-console/testing-extensions.md

---

## API Reference

### `api-reference/index.md` — API Reference
- api-reference/attributes.md
- api-reference/builder-api.md
- api-reference/component-model.md
- api-reference/interfaces.md

### `api-reference/attributes.md` — Core Attributes
- commands/naming.md
- commands/arguments.md
- commands/flags.md
- commands/groups.md
- autocomplete/attribute-handlers.md

### `api-reference/builder-api.md` — Builder API
- building/index.md
- building/registering-commands.md
- building/dependency-injection.md
- api-reference/interfaces.md

### `api-reference/component-model.md` — Component Model
- running/processing-pipeline.md
- remote/shared-protocol.md
- api-reference/interfaces.md
- architecture.md

### `api-reference/interfaces.md` — Interfaces
- api-reference/index.md
- autocomplete/type-handlers.md
- remote/server-proxy.md
- building/console-configuration.md
- commands/error-handling.md
