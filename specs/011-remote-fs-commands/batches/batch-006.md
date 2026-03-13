# Batch 6: remote-fs-commands

**Created**: 2026-03-12
**Status**: complete
**Tasks**: 12 of 15 complete

## Tasks
- [X] T061 [depends:T060] @test-case:011:DF-015 Implement DF-015 (Non-empty directory deleted recursively) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T062 [depends:T061] @test-case:011:DF-016 Implement DF-016 (Glob pattern matches and deletes multiple) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T063 [depends:T062] @test-case:011:DF-017 Implement DF-017 (Glob with fewer than threshold — no prompt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T064 [depends:T063] @test-case:011:DF-018 Implement DF-018 (Glob with ≥ threshold — prompts (answered yes)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T065 [depends:T064] @test-case:011:DF-019 Implement DF-019 (Glob with ≥ threshold — prompts (answered no)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T066 [depends:T065] @test-case:011:DF-020 Implement DF-020 (Cannot delete storage root) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T067 [depends:T066] @test-case:011:DF-050 Implement DF-050 (End-to-end: file gone after command) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_RmCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T068 [depends:T067] @test-case:011:EH-004 Implement EH-004 (Path not found without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T069 [depends:T068] @test-case:011:EH-005 Implement EH-005 (Path not found with `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T070 [depends:T069] @test-case:011:EH-006 Implement EH-006 (Non-empty dir without `-r` or `-d`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T071 [depends:T070] @test-case:011:EH-007 Implement EH-007 (Empty dir without `-d` or `-r`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [X] T072 [depends:T071] @test-case:011:EH-008 Implement EH-008 (Attempt to delete storage root) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities













