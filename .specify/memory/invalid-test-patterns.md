# Invalid Test Patterns

> **Canonical reference for test validity.** All speckit commands reference this file.
> When updating patterns here, all dependent instructions remain valid.

## Quick Check

Before writing ANY test, ask:

> **"Does this test invoke the code under test and verify its behavior?"**

If NO → The test is invalid. Redesign.

---

## Invalid Patterns Table

| Pattern | Example | Why Invalid | Fix |
|---------|---------|-------------|-----|
| Testing constants | `MaxRetries.Should().Be(3)` | Proves nothing about behavior | Test the behavior the constant controls |
| Testing inputs | `input.Contains("*").Should().BeTrue()` | Tests the input, not processing | Test what the code does with that input |
| Testing types exist | `typeof(Service).Should().NotBeNull()` | Compiler guarantees this | Test behavior of the type |
| Tautologies | `x.Should().Be(x)` | Always passes | Test observable outcomes |
| Testing attributes | `[Fact].Should().Exist()` | Tests metadata, not behavior | Test runtime behavior |
| Recreating framework behavior | `new SemaphoreSlim(N)` limits to N | Tests .NET, not your code | Test that YOUR code uses the framework correctly |
| Testing without invoking code | Create mocks, assert on mocks | Never exercises real code | Call actual methods, verify actual outcomes |

---

## Mandatory Validation Checkpoint

**⛔ Output these answers BEFORE writing test code:**

```
> Test Validity Check:
>   Invokes code under test: [YES/NO] — Does this call the actual method/class being tested?
>   Breakage detection: [YES/NO] — If implementation breaks, does test fail?
>   Not a tautology: [YES/NO] — Testing behavior, not restating structure?
```

**If any answer is NO, do not proceed. Redesign the test.**

---

## Detailed Validation Questions

### 1. Behavioral Scope
Does this test exercise actual runtime code paths?

- ❌ Testing a constant value (`Constant.Should().Be(100)`)
- ❌ Testing that a type/method exists
- ❌ Creating your own instance of a framework class and testing the framework
- ✅ Testing that calling a method produces expected output/side-effects

### 2. Breakage Detection
If I change the implementation to be WRONG, would this test fail?

- ❌ `Constant.Should().Be(100)` — changing the constant doesn't break behavior this test would catch
- ❌ `new SemaphoreSlim(N)` test — deleting the semaphore from real code won't fail this test
- ✅ `service.DoThing().Should().ProduceExpectedResult()` — breaking DoThing fails the test

### 3. Not a Tautology
Am I testing the code's behavior, not restating its structure?

- ❌ `files.Sum(f => f.Size).Should().Be(files.Sum(f => f.Size))` — tests nothing
- ❌ Creating a semaphore with value N and asserting it limits to N — tests the framework
- ✅ `command.Execute() → observable output matches expected`

---

## Transformation Examples

When tempted to test a constant or framework, test the BEHAVIOR instead:

| Invalid Test | Valid Transformation |
|--------------|---------------------|
| `ProgressThrottleMs.Should().BeLessOrEqualTo(1000)` | Download a large file, capture progress callback timestamps, verify no gap > 1 second |
| `MaxConcurrentDownloads.Should().Be(4)` | Download 10 files via command, verify max 4 concurrent HTTP requests via mock |
| `new SemaphoreSlim(4)` limits to 4 | Execute `DownloadCommand` with 10 files, instrument `FileTransferService.DownloadFile` to track concurrent calls |

---

## Verification Question

Before capturing evidence, ask yourself:

> **"If someone broke the behavior described in the test case's 'Then' column, would this test fail?"**

If the answer is "no", the test is invalid. Rewrite it.
