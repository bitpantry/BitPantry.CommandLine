# Batch 9: remote-fs-commands

**Created**: 2026-03-12
**Status**: complete
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T097 [depends:T096] @test-case:011:CV-025 Implement CV-025 (`--force` / `-f` flag allows overwrite) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T098 [depends:T097] @test-case:011:CV-035 Implement CV-035 (`-r` alias accepted as `--recursive`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CpCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T099 [depends:T098] @test-case:011:DF-027 Implement DF-027 (File copied, original preserved) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T100 [depends:T099] @test-case:011:DF-028 Implement DF-028 (Directory and contents copied) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T101 [depends:T100] @test-case:011:DF-029 Implement DF-029 (Nested directory structure preserved) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T102 [depends:T101] @test-case:011:DF-030 Implement DF-030 (Fails if source directory without `--recursive`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T103 [depends:T102] @test-case:011:DF-031 Implement DF-031 (Fails if dest file exists without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T104 [depends:T103] @test-case:011:DF-032 Implement DF-032 (Overwrites existing destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T105 [depends:T104] @test-case:011:DF-052 Implement DF-052 (End-to-end: both files exist) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CpCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T106 [depends:T105] @test-case:011:EH-012 Implement EH-012 (Source not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T107 [depends:T106] @test-case:011:EH-013 Implement EH-013 (Source is directory without `--recursive`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T108 [depends:T107] @test-case:011:EH-014 Implement EH-014 (Destination exists without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities

