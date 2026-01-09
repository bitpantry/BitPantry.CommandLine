# BitPantry.CommandLine Constitution

## Core Principles

### I. Test-Driven Development (NON-NEGOTIABLE)

All feature development follows strict TDD:
- **Tests written FIRST** before any implementation code
- Tests must **FAIL initially** (red phase) to prove they're valid
- **Tests must verify specified behavior, not implementation artifacts**
- Implementation written to make tests pass (green phase)
- Refactoring follows with tests still passing
- Comprehensive coverage: all happy paths AND error/exception paths
- No feature is complete until tests pass
- **A test that cannot catch a bug in the specified behavior is not a valid test**

### II. Dependency Injection

All services and abstractions use constructor injection:
- No static methods for functionality that could need testing or swapping
- No service locator patterns
- All dependencies registered in DI container
- Prefer interfaces over concrete types for testability

### III. Security by Design

Security is not an afterthought:
- Tokens and secrets transmitted in headers, never in URLs or query strings
- Input validation at trust boundaries (path traversal, size limits, type restrictions)
- Security rejection events logged with structured data
- Principle of least privilege for remote operations

### IV. Follow Existing Patterns

Consistency across the codebase:
- New code follows established patterns in the solution
- RPC uses existing `RpcMessageRegistry` + `MessageBase` infrastructure
- HTTP endpoints follow existing `MapPost`/`MapGet` patterns
- Configuration follows existing options patterns
- When in doubt, look at how similar functionality is already implemented

### V. Integration Testing for Cross-Cutting Concerns

Integration tests required for:
- Client-server communication
- RPC message handling end-to-end
- Authentication and authorization flows
- File transfer operations
- Any feature spanning multiple projects

## Testing Standards

- **Framework**: MSTest with FluentAssertions and Moq
- **Naming**: `MethodUnderTest_Scenario_ExpectedBehavior`
- **Structure**: Arrange/Act/Assert pattern
- **Coverage**: Both success and failure scenarios
- **Mocking**: Use MockFileSystem from System.IO.Abstractions.TestingHelpers for file operations

## Code Quality

- No suppressed warnings without documented justification
- Async methods end with `Async` suffix
- Cancellation tokens propagated through async call chains
- Disposable resources properly disposed (using statements or IDisposable pattern)

## Documentation

- Public APIs documented with XML comments
- Complex logic explained with inline comments
- Breaking changes documented in spec and migration notes provided

## Governance

- This constitution supersedes ad-hoc decisions
- Amendments require explicit discussion and documentation
- All PRs should verify compliance with these principles

**Version**: 1.0.0 | **Ratified**: 2024-12-22 | **Last Amended**: 2024-12-22
