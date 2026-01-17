---
description: Build test inventory by scanning all test projects and extracting metadata.
---

## User Input

```text
$ARGUMENTS
```

If a specific test project is provided, scan only that project. Otherwise, scan all test projects.

---

## Intent

Phase 1 of the Test Remediation workflow. Discovers all tests in the solution and extracts structural metadata needed for clustering and assessment.

---

## Prerequisites

1. Read `refactor-temp/README.md` for project context
2. Read `refactor-temp/standards.md` for testing standards reference

---

## Execution Steps

### Step 1: Identify Test Projects

Scan for test projects in the workspace:

```
BitPantry.CommandLine.Tests/
BitPantry.CommandLine.Tests.Remote.SignalR/
BitPantry.VirtualConsole.Tests/
```

### Step 2: Scan Each Project

For each test project, find all `*.cs` files and extract:

1. **Test Classes** — Files containing `[TestClass]` attribute
2. **Test Methods** — Methods with `[TestMethod]` attribute

### Step 3: Extract Metadata per Test

For each test method, extract:

| Field | How to Extract |
|-------|----------------|
| `id` | Sequential: T001, T002, etc. |
| `testClass` | Class name containing `[TestClass]` |
| `testMethod` | Method name with `[TestMethod]` |
| `filePath` | Relative path from workspace root |
| `lineNumber` | Line where method is defined |
| `subjectClass` | Look for `new ClassName()` or `_subject` pattern in Arrange |
| `subjectMethod` | Look for method call in Act section |
| `mockSetup` | Extract `Mock<IInterface>` and `.Setup()` patterns |
| `actSignature` | Normalize the Act line (method call being tested) |
| `assertionTargets` | Extract `.Should()`, `.Verify()`, `Assert.*` patterns |
| `testInfrastructure` | Detect `TestConsole`, `VirtualConsole`, helper usage |

### Step 4: Generate Inventory Files

**Create `refactor-temp/test-inventory.json`:**

```json
{
  "generated": "2026-01-15",
  "totalTests": 150,
  "projects": {
    "BitPantry.CommandLine.Tests": 45,
    "BitPantry.CommandLine.Tests.Remote.SignalR": 85,
    "BitPantry.VirtualConsole.Tests": 20
  },
  "tests": [
    {
      "id": "T001",
      "testClass": "DownloadCommandTests",
      "testMethod": "Execute_ValidFile_Succeeds",
      "filePath": "BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/DownloadCommandTests.cs",
      "lineNumber": 45,
      "subjectClass": "DownloadCommand",
      "subjectMethod": "Execute",
      "mockSetup": ["IServerProxy:Connected", "IFileTransferService"],
      "actSignature": "command.Execute(context)",
      "assertionTargets": ["console.Output", "Should().Contain"],
      "testInfrastructure": ["TestConsole", "TestServerProxyFactory"]
    }
  ]
}
```

**Create `refactor-temp/test-inventory.md`:**

```markdown
# Test Inventory

**Generated**: 2026-01-15  
**Total Tests**: 150

## Summary by Project

| Project | Test Classes | Test Methods |
|---------|--------------|--------------|
| BitPantry.CommandLine.Tests | 10 | 45 |
| BitPantry.CommandLine.Tests.Remote.SignalR | 15 | 85 |
| BitPantry.VirtualConsole.Tests | 5 | 20 |

## Tests by Class

### BitPantry.CommandLine.Tests.Remote.SignalR

#### DownloadCommandTests (25 tests)
- T001: Execute_ValidFile_Succeeds
- T002: Execute_NotConnected_ReturnsError
...

#### UploadCommandTests (15 tests)
...
```

### Step 5: Update Status

Update `refactor-temp/plan.md` progress tracking:
- Phase 1 Status: Complete
- Tests Inventoried: [count]

---

## Output Format

Report completion:

```
Test Inventory Complete

Projects scanned: 3
Test classes found: 30
Test methods found: 150

Output files:
- refactor-temp/test-inventory.json
- refactor-temp/test-inventory.md

Next step: Run /test-remediation.cluster to group tests by consolidation criteria.
```

---

## Constraints

- Extract metadata using pattern matching, not full AST parsing
- Include ALL test methods, even if metadata extraction is incomplete
- Mark tests with incomplete metadata as `"extractionStatus": "partial"`
- Do not modify any test files during this phase
