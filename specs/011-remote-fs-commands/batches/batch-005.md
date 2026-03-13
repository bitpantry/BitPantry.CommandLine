# Batch 5: remote-fs-commands

**Created**: 2026-03-12
**Status**: in-progress
**Tasks**: 12 of 15 complete

## Tasks
- [X] T049 [depends:T048] @test-case:011:UX-027 Implement UX-027 (Created message visible in VirtualConsole) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MkdirCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [X] T050 [depends:T002] @test-case:011:CV-012 Implement CV-012 (`path` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T051 [depends:T050] @test-case:011:CV-013 Implement CV-013 (`--recursive` / `-r` flag allows non-empty dir deletion) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T052 [depends:T051] @test-case:011:CV-014 Implement CV-014 (`--directory` / `-d` flag allows empty dir deletion) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T053 [depends:T052] @test-case:011:CV-015 Implement CV-015 (`--force` / `-f` flag skips confirmation) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T054 [depends:T053] @test-case:011:CV-016 Implement CV-016 (Without `-r` deleting non-empty dir produces error) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T055 [depends:T054] @test-case:011:CV-017 Implement CV-017 (Without `-d` deleting empty dir produces error) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T056 [depends:T055] @test-case:011:CV-034 Implement CV-034 (`-r` alias accepted as `--recursive`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_RmCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T057 [depends:T056] @test-case:011:DF-011 Implement DF-011 (Single file deleted) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T058 [depends:T057] @test-case:011:DF-012 Implement DF-012 (Empty directory deleted) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T059 [depends:T058] @test-case:011:DF-013 Implement DF-013 (Non-existent path with `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T060 [depends:T059] @test-case:011:DF-014 Implement DF-014 (Non-existent path without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities












