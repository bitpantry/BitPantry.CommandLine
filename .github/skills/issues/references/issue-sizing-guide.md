# Issue Sizing Guide

Guidelines for grouping spec requirements into appropriately-sized GitHub issues.

## Target Size

Each issue should represent **one pull request's worth of work** — implementable in a focused session by a human or agent.

## Sizing Heuristics

### Good Issue Size
- Covers 1-3 closely-related functional requirements
- Touches 2-8 production files
- Includes corresponding test files
- Has a single, clear "done" state
- Can be tested independently after implementation

### Too Large (Split It)
- Covers more than one user story (unless they're tightly coupled)
- Requires changes across multiple architectural layers with no shared thread
- Has more than 5 unrelated functional requirements
- Would result in a PR that's hard to review (>500 lines changed)

### Too Small (Combine It)
- A single file rename or config change
- Adding one field to one entity with no behavioral change
- Pure boilerplate with no logic

## Grouping Principles

### Group Together
- Entity + its repository + its service interface (they're meaningless apart)
- An endpoint + its validation + its tests
- Database migration + entity changes (they must deploy together)
- A user story's happy path + its error handling (same code path)

### Keep Separate
- Independent user stories (unless they share 80%+ of the same code)
- Infrastructure/setup work vs. feature work
- Cross-cutting concerns (logging, security headers) vs. feature logic

## Issue Types

| Type | Contents | When |
|------|----------|------|
| **Setup** | Project scaffolding, shared dependencies, base configuration | Always first, blocks everything |
| **Data Layer** | Entities, migrations, repositories | When feature introduces new data |
| **Feature** | Service logic, endpoints, commands, UI | Core deliverable |
| **Integration** | External service connections, API clients | When feature connects to external systems |
| **Cross-cutting** | Security, logging, monitoring additions | After core features work |
| **Polish** | Documentation, error messages, edge cases | Last phase |

## Dependency Minimization

- Prefer fat, independent issues over thin, dependent chains
- If issue B needs one thing from issue A, consider putting that one thing in issue B instead
- Setup/infrastructure issues are the acceptable exception — they naturally block feature work
