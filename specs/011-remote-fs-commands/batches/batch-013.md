# Batch 13: remote-fs-commands

**Created**: 2026-03-12
**Status**: pending
**Tasks**: 0 of 15 complete

## Tasks
- [ ] T145 [depends:T144] @test-case:011:DF-046 Implement DF-046 (Returns correct file count for directory) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T146 [depends:T145] @test-case:011:DF-047 Implement DF-047 (Directory total size is recursive sum) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T147 [depends:T146] @test-case:011:DF-054 Implement DF-054 (End-to-end: stat output visible) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_StatCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T148 [depends:T147] @test-case:011:EH-020 Implement EH-020 (Path not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T149 [depends:T148] @test-case:011:EH-027 Implement EH-027 (Path traversal attempt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T150 [depends:T149] @test-case:011:UX-023 Implement UX-023 (All fields rendered for a file) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T151 [depends:T150] @test-case:011:UX-024 Implement UX-024 (Size shown in human-readable and raw bytes) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T152 [depends:T151] @test-case:011:UX-025 Implement UX-025 (Directory shows ItemCount, FileCount, DirectoryCount) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T153 [depends:T002] @test-case:011:SETUP-003 Document command reference updates in README.md
- [ ] T154 [depends:T153] @test-case:011:UX-029 Implement UX-029 (Command help reflects remote file system syntax) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CommandHelp.cs
- [ ] T158 [depends:T154] @test-case:011:SETUP-005 Update CLAUDE.md command guidance for remote file system commands
- [ ] T155 [depends:T154] @test-case:011:EH-031 Implement EH-031 (Disconnected invocation returns standard not-connected message) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs
- [ ] T156 [depends:T155] @test-case:011:EH-032 Implement EH-032 (`server ls` glob no-match displays explicit message) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T157 [depends:T156] @test-case:011:EH-033 Implement EH-033 (`server rm` glob no-match displays explicit message) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T159 [depends:T157] @test-case:011:EH-034 Implement EH-034 (Mid-operation disconnect aborts with clear error) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CpCommand.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
