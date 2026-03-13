# Batch 1: remote-fs-commands

**Created**: 2026-03-12
**Status**: complete
**Tasks**: 12 of 12 complete

## Tasks
- [x] T001 @test-case:011:SETUP-001 Add server command group scaffold in BitPantry.CommandLine.Remote.SignalR.Server/Commands/ServerGroup.cs
- [x] T002 [depends:T001] @test-case:011:SETUP-002 Register server command types in BitPantry.CommandLine.Remote.SignalR.Server/Configuration/IServiceCollectionExtensions.cs
- [x] T003 [depends:T002] @test-case:011:CV-001 Implement CV-001 (Path argument is optional) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T004 [depends:T003] @test-case:011:CV-002 Implement CV-002 (Path argument accepted as positional) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T005 [depends:T004] @test-case:011:CV-003 Implement CV-003 (`--long` / `-l` flag activates long mode) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T006 [depends:T005] @test-case:011:CV-004 Implement CV-004 (`--recursive` flag activates recursion) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T007 [depends:T006] @test-case:011:CV-005 Implement CV-005 (`--sort` accepts `name`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T008 [depends:T007] @test-case:011:CV-006 Implement CV-006 (`--sort` accepts `size`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T009 [depends:T008] @test-case:011:CV-007 Implement CV-007 (`--sort` accepts `modified`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T010 [depends:T009] @test-case:011:CV-008 Implement CV-008 (`--reverse` alone reverses default sort) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T011 [depends:T010] @test-case:011:CV-009 Implement CV-009 (`--reverse` combined with `--sort size`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [x] T012 [depends:T011] @test-case:011:CV-033 Implement CV-033 (`-l` alias accepted as `--long`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs

## Completion Criteria

- [x] All tasks verified (evidence validated)
- [x] Full test suite passes (5 consecutive clean runs)
- [x] No open ambiguities

