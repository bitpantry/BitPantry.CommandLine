# Batch 2: remote-fs-commands

**Created**: 2026-03-12
**Status**: complete
**Tasks**: 12 of 12 complete

## Tasks
- [x] T013 [depends:T012] @test-case:011:DF-001 Implement DF-001 (Lists files at specified path) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T014 [depends:T013] @test-case:011:DF-002 Implement DF-002 (Lists subdir contents when path is a dir) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T015 [depends:T014] @test-case:011:DF-003 Implement DF-003 (Glob pattern `*.txt` filters to text files) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T016 [depends:T015] @test-case:011:DF-004 Implement DF-004 (Glob `*.log` matches multiple) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T017 [depends:T016] @test-case:011:DF-005 Implement DF-005 (Traverses subdirectories) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T018 [depends:T017] @test-case:011:DF-006 Implement DF-006 (Actual sort by file size) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T019 [depends:T018] @test-case:011:DF-048 Implement DF-048 (End-to-end: files in tempDir appear after connect) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T020 [depends:T019] @test-case:011:DF-055 Implement DF-055 (Server commands appear after connect) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T021 [depends:T020] @test-case:011:EH-001 Implement EH-001 (Path not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T022 [depends:T021] @test-case:011:EH-002 Implement EH-002 (Path is a file (not a dir) and no glob) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T023 [depends:T022] @test-case:011:EH-021 Implement EH-021 (`SandboxedFileSystem` blocks path traversal attempt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T024 [depends:T023] @test-case:011:EH-029 Implement EH-029 (Path not found returns error (not exception)) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs

## Completion Criteria

- [x] All tasks verified (evidence validated)
- [x] Full test suite passes (5 consecutive clean runs)
- [x] No open ambiguities
