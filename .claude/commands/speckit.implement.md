---
description: Execute the implementation plan by processing and executing all tasks defined in tasks.md
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS list. All paths must be absolute. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Check checklists status** (if FEATURE_DIR/checklists/ exists):
   - Scan all checklist files in the checklists/ directory
   - For each checklist, count:
     - Total items: All lines matching `- [ ]` or `- [X]` or `- [x]`
     - Completed items: Lines matching `- [X]` or `- [x]`
     - Incomplete items: Lines matching `- [ ]`
   - Create a status table:

     ```text
     | Checklist | Total | Completed | Incomplete | Status |
     |-----------|-------|-----------|------------|--------|
     | ux.md     | 12    | 12        | 0          | ✓ PASS |
     | test.md   | 8     | 5         | 3          | ✗ FAIL |
     | security.md | 6   | 6         | 0          | ✓ PASS |
     ```

   - Calculate overall status:
     - **PASS**: All checklists have 0 incomplete items
     - **FAIL**: One or more checklists have incomplete items

   - **If any checklist is incomplete**:
     - Display the table with incomplete item counts
     - **STOP** and ask: "Some checklists are incomplete. Do you want to proceed with implementation anyway? (yes/no)"
     - Wait for user response before continuing
     - If user says "no" or "wait" or "stop", halt execution
     - If user says "yes" or "proceed" or "continue", proceed to step 3

   - **If all checklists are complete**:
     - Display the table showing all checklists passed
     - Automatically proceed to step 3

---

### ⚠️ Decision Point Protocol

**DO NOT assume to fill gaps.** When you encounter ambiguity that requires assumption to proceed:

1. **STOP** implementation immediately
2. **PRESENT** the decision point:
   - What specific gap or ambiguity was encountered
   - Where it was found (task, spec, plan, or missing entirely)
   - Available options with trade-offs for each
   - **TOP RECOMMENDATION**: Your single best recommendation (marked clearly)
3. **WAIT** for user input before proceeding

**Applies to**: Technical ambiguities (e.g., unspecified validation rules, unclear architecture choices) AND process decisions (e.g., how to handle a failed task, unexpected state).

**Threshold**: Stop only when the decision could meaningfully impact implementation or when filling the gap requires non-trivial assumption. Trivial decisions (e.g., variable naming style consistent with codebase) can proceed.

---

### 🚀 Continuous Execution Directive

**Execute all tasks without pausing for confirmation.** Do NOT:
- Stop to give progress updates and ask "should I continue?"
- Pause between phases to ask for permission to proceed
- Request confirmation before starting the next task
- Summarize completed work mid-execution and wait for acknowledgment

**Keep executing** until one of these occurs:
1. **Decision Point Protocol triggered** - ambiguity requiring user input
2. **Test Integrity Protocol triggered** - test modification requiring approval
3. **All tasks completed** - report final summary
4. **Blocking error** - cannot proceed without resolution

**Task completion ritual**: After completing each task:
1. Update tasks.md: change `- [ ]` to `- [X]` for that task
2. Immediately proceed to the next task

**Batch your work**: Complete as many tasks as possible in each response. Use parallel tool calls where appropriate. Minimize round-trips.

---

### 🧪 Test Integrity Protocol

**Tests are specifications, not just verification.** A test encodes business intent and expected behavior. When a test fails:

**Default Assumption**: The **code is wrong**, not the test.

**Test cases as source of truth**: Each test should implement one or more test cases from test-cases.md. The "When X, Then Y" definition in test-cases.md is the authoritative specification for what the test should verify.

**Before modifying ANY test assertion or constraint**:

1. **ARTICULATE** the test's original intent:
   - What test case ID(s) does this test implement? (e.g., UX-001, CV-003)
   - What is the "When X, Then Y" from test-cases.md?
   - Why were these specific assertions chosen?

2. **DIAGNOSE** the failure:
   - Is the code not implementing the intended behavior? → **Fix the code**
   - Is the test's intent outdated due to legitimate spec change? → **Confirm with user**
   - Is the test technically flawed (wrong setup, race condition)? → **Fix test mechanics, preserve intent**

3. **NEVER do the following without explicit user approval**:
   - Weaken assertions (e.g., `Should().ContainExactly(3)` → `Should().HaveCountGreaterThan(0)`)
   - Remove assertions that are failing
   - Change expected values to match current (buggy) behavior
   - Generalize specific checks (e.g., `item.Name == "Settings"` → `item.Name != null`)

**If you believe a test's intent is wrong**, trigger the **Decision Point Protocol**:
- Present the test's apparent intent
- Explain why you believe the intent (not just the code) is incorrect
- **TOP RECOMMENDATION**: Your suggested resolution
- **WAIT** for user confirmation before modifying test expectations

**Legitimate test modifications** (no approval needed):
- Fixing test setup/teardown mechanics
- Updating imports or references after refactoring
- Adjusting timing/async handling while preserving assertions
- Adding MORE specific assertions (strengthening, not weakening)

---

3. Load and analyze the implementation context:
   - **REQUIRED**: Read tasks.md for the complete task list and execution plan
   - **REQUIRED**: Read plan.md for tech stack, architecture, and file structure
   - **REQUIRED**: Read test-cases.md for test case definitions (When X, Then Y)
   - **IF EXISTS**: Read data-model.md for entities and relationships
   - **IF EXISTS**: Read contracts/ for API specifications and test requirements
   - **IF EXISTS**: Read research.md for technical decisions and constraints
   - **IF EXISTS**: Read quickstart.md for integration scenarios

4. **Project Setup Verification**:
   - **REQUIRED**: Create/verify ignore files based on actual project setup:

   **Detection & Creation Logic**:
   - Check if the following command succeeds to determine if the repository is a git repo (create/verify .gitignore if so):

     ```sh
     git rev-parse --git-dir 2>/dev/null
     ```

   - Check if Dockerfile* exists or Docker in plan.md → create/verify .dockerignore
   - Check if .eslintrc* exists → create/verify .eslintignore
   - Check if eslint.config.* exists → ensure the config's `ignores` entries cover required patterns
   - Check if .prettierrc* exists → create/verify .prettierignore
   - Check if .npmrc or package.json exists → create/verify .npmignore (if publishing)
   - Check if terraform files (*.tf) exist → create/verify .terraformignore
   - Check if .helmignore needed (helm charts present) → create/verify .helmignore

   **If ignore file already exists**: Verify it contains essential patterns, append missing critical patterns only
   **If ignore file missing**: Create with full pattern set for detected technology

   **Common Patterns by Technology** (from plan.md tech stack):
   - **Node.js/JavaScript/TypeScript**: `node_modules/`, `dist/`, `build/`, `*.log`, `.env*`
   - **Python**: `__pycache__/`, `*.pyc`, `.venv/`, `venv/`, `dist/`, `*.egg-info/`
   - **Java**: `target/`, `*.class`, `*.jar`, `.gradle/`, `build/`
   - **C#/.NET**: `bin/`, `obj/`, `*.user`, `*.suo`, `packages/`
   - **Go**: `*.exe`, `*.test`, `vendor/`, `*.out`
   - **Ruby**: `.bundle/`, `log/`, `tmp/`, `*.gem`, `vendor/bundle/`
   - **PHP**: `vendor/`, `*.log`, `*.cache`, `*.env`
   - **Rust**: `target/`, `debug/`, `release/`, `*.rs.bk`, `*.rlib`, `*.prof*`, `.idea/`, `*.log`, `.env*`
   - **Kotlin**: `build/`, `out/`, `.gradle/`, `.idea/`, `*.class`, `*.jar`, `*.iml`, `*.log`, `.env*`
   - **C++**: `build/`, `bin/`, `obj/`, `out/`, `*.o`, `*.so`, `*.a`, `*.exe`, `*.dll`, `.idea/`, `*.log`, `.env*`
   - **C**: `build/`, `bin/`, `obj/`, `out/`, `*.o`, `*.a`, `*.so`, `*.exe`, `Makefile`, `config.log`, `.idea/`, `*.log`, `.env*`
   - **Swift**: `.build/`, `DerivedData/`, `*.swiftpm/`, `Packages/`
   - **R**: `.Rproj.user/`, `.Rhistory`, `.RData`, `.Ruserdata`, `*.Rproj`, `packrat/`, `renv/`
   - **Universal**: `.DS_Store`, `Thumbs.db`, `*.tmp`, `*.swp`, `.vscode/`, `.idea/`

   **Tool-Specific Patterns**:
   - **Docker**: `node_modules/`, `.git/`, `Dockerfile*`, `.dockerignore`, `*.log*`, `.env*`, `coverage/`
   - **ESLint**: `node_modules/`, `dist/`, `build/`, `coverage/`, `*.min.js`
   - **Prettier**: `node_modules/`, `dist/`, `build/`, `coverage/`, `package-lock.json`, `yarn.lock`, `pnpm-lock.yaml`
   - **Terraform**: `.terraform/`, `*.tfstate*`, `*.tfvars`, `.terraform.lock.hcl`
   - **Kubernetes/k8s**: `*.secret.yaml`, `secrets/`, `.kube/`, `kubeconfig*`, `*.key`, `*.crt`

5. Parse tasks.md structure and extract:
   - **Task phases**: Setup, Tests, Core, Integration, Polish
   - **Task dependencies**: Sequential vs parallel execution rules
   - **Task details**: ID, description, file paths, parallel markers [P]
   - **Execution flow**: Order and dependency requirements

6. Execute implementation following the task plan:
   - **Phase-by-phase execution**: Complete each phase before moving to the next
   - **Respect dependencies**: Run sequential tasks in order, parallel tasks [P] can run together  
   - **Follow TDD approach**: Execute test tasks before their corresponding implementation tasks
   - **File-based coordination**: Tasks affecting the same files must run sequentially
   - **Validation checkpoints**: Verify each phase completion before proceeding

7. Implementation execution rules:
   - **Setup first**: Initialize project structure, dependencies, configuration
   - **Tests before code**: Write tests that implement test case IDs from test-cases.md before implementation
   - **Reference test cases**: Each test should document which test case ID(s) it implements (e.g., `// Implements: UX-001, EH-003`)
   - **When tests fail**: Follow the **Test Integrity Protocol** - assume code is wrong, not test. Never weaken assertions without user approval.
   - **Core development**: Implement models, services, CLI commands, endpoints
   - **Integration work**: Database connections, middleware, logging, external services
   - **Polish and validation**: Unit tests, performance optimization, documentation

8. Progress tracking and error handling:
   - **⚠️ CRITICAL**: Mark each task complete (`- [X]`) in tasks.md **immediately** after completing it - do this BEFORE moving to the next task
   - **Do NOT pause** to report progress - keep executing until blocked or done
   - Halt execution only if a blocking error prevents continuation
   - For parallel tasks [P], continue with successful tasks, note failed ones
   - If implementation cannot proceed, provide clear error context and suggest resolution

9. Completion validation:
   - Verify all required tasks are completed - no tasks should be considered optional or deferrable
   - Check that implemented features match the original specification
   - Validate that tests pass and coverage meets requirements
   - Verify all test case IDs from test-cases.md are implemented by at least one test
   - Confirm the implementation follows the technical plan
   - Report final status with summary of completed work and test case coverage

Note: This command assumes a complete task breakdown exists in tasks.md. If tasks are incomplete or missing, suggest running `/speckit.tasks` first to regenerate the task list.

**REMINDER**: Follow the **Decision Point Protocol** (above) whenever you encounter gaps requiring assumption. Do not proceed past meaningful ambiguity without user input.
