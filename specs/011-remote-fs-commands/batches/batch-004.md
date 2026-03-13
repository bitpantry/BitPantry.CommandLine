# Batch 4: remote-fs-commands

**Created**: 2026-03-12
**Status**: in-progress
**Tasks**: 12 of 15 complete

## Tasks
- [X] T037 [depends:T036] @test-case:011:UX-026 Implement UX-026 (End-to-end output visible in VirtualConsole) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [X] T038 [depends:T002] @test-case:011:CV-010 Implement CV-010 (`path` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T039 [depends:T038] @test-case:011:CV-011 Implement CV-011 (`--parents` / `-p` flag activates deep creation) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T040 [depends:T039] @test-case:011:CV-036 Implement CV-036 (`-p` alias accepted as `--parents`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MkdirCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T041 [depends:T040] @test-case:011:DF-007 Implement DF-007 (Directory created at path) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T042 [depends:T041] @test-case:011:DF-008 Implement DF-008 (All intermediate dirs created) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T043 [depends:T042] @test-case:011:DF-009 Implement DF-009 (Fails if parent missing without `--parents`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T044 [depends:T043] @test-case:011:DF-010 Implement DF-010 (Idempotent when directory already exists) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T045 [depends:T044] @test-case:011:DF-049 Implement DF-049 (End-to-end: directory exists on disk after command) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MkdirCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T046 [depends:T045] @test-case:011:EH-003 Implement EH-003 (Parent does not exist) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T047 [depends:T046] @test-case:011:EH-022 Implement EH-022 (Path traversal attempt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T048 [depends:T047] @test-case:011:UX-013 Implement UX-013 (Success message includes path) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities












