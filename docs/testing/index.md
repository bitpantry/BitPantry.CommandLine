# Testing Guide

Strategies and infrastructure for testing BitPantry.CommandLine applications at three levels: unit, integration, and UX.

---

## Test Levels

| Level | Scope | Tools |
|-------|-------|-------|
| [Unit Testing](unit-testing.md) | Individual commands in isolation | Mocks, `MockFileSystem`, minimal builder |
| [Integration Testing](integration-testing.md) | Full client-server round trips | `TestEnvironment`, in-memory `TestServer` |
| [UX Testing](ux-testing.md) | Console output and interactive behavior | `VirtualConsole`, `VirtualConsoleAssertions` |

---

## Approach

**Unit tests** validate command logic by building a minimal `CommandLineApplication`, running input strings, and asserting on `RunResult`. Dependencies are mocked.

**Integration tests** spin up a full ASP.NET `TestServer` in-process, connect a SignalR client to it, and exercise the complete remote execution pipeline without network access.

**UX tests** capture console output in a `VirtualConsole` and assert on the rendered text using FluentAssertions extensions.

---

## In This Section

| Page | Description |
|------|-------------|
| [Unit Testing](unit-testing.md) | Testing commands with mocks and `RunOnce` |
| [Integration Testing](integration-testing.md) | Full client-server testing with `TestEnvironment` |
| [UX Testing](ux-testing.md) | Visual output testing with `VirtualConsole` |

---

## See Also

- [Unit Testing](unit-testing.md)
- [Integration Testing](integration-testing.md)
- [UX Testing](ux-testing.md)
- [BitPantry.VirtualConsole](../virtual-console/index.md)
