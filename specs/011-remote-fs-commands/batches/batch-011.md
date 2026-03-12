# Batch 11: remote-fs-commands

**Created**: 2026-03-10
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T121 [depends:T120] @test-case:011:DF-036 Implement DF-036 (Outputs only last 2 lines) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T122 [depends:T121] @test-case:011:DF-037 Implement DF-037 (`--tail` > file length: all lines) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T123 [depends:T122] @test-case:011:DF-038 Implement DF-038 (Binary file detected — aborts) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T124 [depends:T123] @test-case:011:DF-039 Implement DF-039 (Binary file with `--force` — outputs anyway) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T125 [depends:T124] @test-case:011:DF-040 Implement DF-040 (Large file without `--lines` prompts (yes)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T126 [depends:T125] @test-case:011:DF-041 Implement DF-041 (Large file without `--lines` prompts (no)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T127 [depends:T126] @test-case:011:DF-042 Implement DF-042 (Large file with `--force` — no prompt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T128 [depends:T127] @test-case:011:DF-053 Implement DF-053 (End-to-end: file content visible in VirtualConsole) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CatCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T129 [depends:T128] @test-case:011:EH-015 Implement EH-015 (File not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T130 [depends:T129] @test-case:011:EH-016 Implement EH-016 (Path is a directory) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T131 [depends:T130] @test-case:011:EH-017 Implement EH-017 (Binary content without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T132 [depends:T131] @test-case:011:EH-018 Implement EH-018 (`--lines=0`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
