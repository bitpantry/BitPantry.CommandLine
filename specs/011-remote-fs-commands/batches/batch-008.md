# Batch 8: remote-fs-commands

**Created**: 2026-03-12
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T085 [depends:T084] @test-case:011:DF-025 Implement DF-025 (Overwrites existing destination file) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T086 [depends:T085] @test-case:011:DF-026 Implement DF-026 (Fails if source same as destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T087 [depends:T086] @test-case:011:DF-051 Implement DF-051 (End-to-end: file at new location) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MvCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T088 [depends:T087] @test-case:011:EH-009 Implement EH-009 (Source not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T089 [depends:T088] @test-case:011:EH-010 Implement EH-010 (Destination already exists without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T090 [depends:T089] @test-case:011:EH-011 Implement EH-011 (Source equals destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T091 [depends:T090] @test-case:011:EH-024 Implement EH-024 (Path traversal in source) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T092 [depends:T091] @test-case:011:UX-016 Implement UX-016 (Success shows source and destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T093 [depends:T002] @test-case:011:CV-021 Implement CV-021 (`source` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T094 [depends:T093] @test-case:011:CV-022 Implement CV-022 (`destination` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T095 [depends:T094] @test-case:011:CV-023 Implement CV-023 (`--recursive` / `-r` required for directory copy) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T096 [depends:T095] @test-case:011:CV-024 Implement CV-024 (`--recursive` / `-r` accepted for directory copy) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
