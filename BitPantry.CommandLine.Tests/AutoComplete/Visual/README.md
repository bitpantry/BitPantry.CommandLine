# Visual UX Tests

This folder contains the canonical set of visual UX tests for the autocomplete system.

## Test Organization

These tests use the `StepwiseTestRunner` infrastructure, which simulates individual keystrokes
and tracks the exact visual state (buffer, cursor position, menu visibility, ghost text) at each step.

### Test Files

| File | Purpose | Tests |
|------|---------|-------|
| `VisualTestBase.cs` | Shared base class with test commands, registry, and runner factory methods | N/A |
| `MenuBehaviorTests.cs` | Tab, arrows, Enter, Escape, menu navigation and wrapping | ~18 |
| `GhostBehaviorTests.cs` | Ghost text display, acceptance, cursor movement interaction | ~22 |
| `InputEditingTests.cs` | Typing, backspace, delete, cursor movement (Home, End, arrows) | ~19 |
| `WorkflowTests.cs` | Complete workflows, chained completions, history navigation, submission | ~14 |
| `EdgeCaseTests.cs` | Navigation edge cases, complex input, cancellation, parameter completion | ~26 |
| `MenuRenderingTests.cs` | Menu visual rendering tests | ~15 |
| `VisualFeedbackTests.cs` | General visual feedback tests | ~7 |
| `GhostMenuInteractionRenderingTests.cs` | Ghost/menu interaction rendering | ~11 |

### Adding New Tests

1. Inherit from `VisualTestBase` to get shared command registry and runner factory methods
2. Place tests in the appropriate file based on what's being tested:
   - Menu behavior → `MenuBehaviorTests.cs`
   - Ghost text → `GhostBehaviorTests.cs`
   - Input/cursor → `InputEditingTests.cs`
   - Multi-step workflows → `WorkflowTests.cs`
   - Edge cases → `EdgeCaseTests.cs`

3. Follow the naming convention: `FeatureUnderTest_Scenario_ExpectedResult`

### Test Commands

The base class provides these test commands:
- `server` (group) - for testing hierarchical structures
  - `profile` (nested group)
    - `add`, `remove` (commands)
  - `connect`, `disconnect`, `status` (commands)
- `help`, `config` (root commands)

This allows testing:
- Root level completion
- Group navigation
- Subcommand completion
- Argument completion (connect has --host, --port/-p args)
