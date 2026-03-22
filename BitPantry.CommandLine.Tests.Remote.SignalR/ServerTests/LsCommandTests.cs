using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using FluentAssertions;
using Spectre.Console.Testing;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.CommandLine.AutoComplete;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class LsCommandTests
    {
        private static readonly Type LsCommandType = typeof(LsCommand);

        [TestMethod]
        public void ServerGroup_HasGroupAttribute_WithNameServer()
        {
            var type = typeof(BitPantry.CommandLine.Remote.SignalR.Server.Commands.ServerGroup);
            var attr = type.GetCustomAttributes(typeof(GroupAttribute), false)
                .OfType<GroupAttribute>().FirstOrDefault();
            attr.Should().NotBeNull("ServerGroup must have [Group] attribute");
            attr!.Name.Should().Be("server");
        }

        [TestMethod]
        public void LsCommand_IsRegisteredInServerCommandRegistry()
        {
            LsCommandType.Should().NotBeNull();
            LsCommandType.IsSubclassOf(typeof(CommandBase)).Should().BeTrue(
                "LsCommand must extend CommandBase to be registerable");
        }

        // T003 CV-001: Path argument is optional
        [TestMethod]
        public void LsCommand_PathArgument_IsOptional()
        {
            var prop = LsCommandType.GetProperty("Path");
            prop.Should().NotBeNull("LsCommand must have a Path property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Path must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeFalse("Path argument should be optional");
        }

        // T004 CV-002: Path argument accepted as positional
        [TestMethod]
        public void LsCommand_PathArgument_IsPositional()
        {
            var prop = LsCommandType.GetProperty("Path");
            prop.Should().NotBeNull();
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull();
            argAttr!.Position.Should().Be(0, "Path should be positional at index 0");
        }

        // T005 CV-003: command name is 'ls'
        [TestMethod]
        public void LsCommand_HasCommandAttribute_WithNameLs()
        {
            var attr = LsCommandType.GetCustomAttribute<CommandAttribute>();
            attr.Should().NotBeNull("LsCommand must have [Command] attribute");
            attr!.Name.Should().Be("ls");
        }

        // T006 CV-004: Path argument has name 'path'
        [TestMethod]
        public void LsCommand_PathArgument_HasNamePath()
        {
            var prop = LsCommandType.GetProperty("Path");
            prop.Should().NotBeNull();
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull();
            argAttr!.Name.Should().Be("path");
        }

        // T007 CV-005: Path default value '.'
        [TestMethod]
        public void LsCommand_PathArgument_DefaultsToCurrentDirectory()
        {
            var fs = new MockFileSystem();
            var instance = new LsCommand(fs, new Theme());
            var prop = LsCommandType.GetProperty("Path");
            prop.Should().NotBeNull();
            var value = prop!.GetValue(instance);
            value.Should().Be(".", "Path should default to '.' (current directory)");
        }

        // T008 CV-006: Long flag exists
        [TestMethod]
        public void LsCommand_HasLongFlag()
        {
            var prop = LsCommandType.GetProperty("Long");
            prop.Should().NotBeNull("LsCommand must have a Long property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("Long must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Long must have [Argument] attribute");
            argAttr!.Name.Should().Be("long");
        }

        // T009 CV-007: Long flag alias '-l'
        [TestMethod]
        public void LsCommand_LongFlag_HasAliasL()
        {
            var prop = LsCommandType.GetProperty("Long");
            prop.Should().NotBeNull();
            var aliasAttr = prop!.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Long must have [Alias] attribute");
            aliasAttr!.Alias.Should().Be('l');
        }

        // T010 CV-008: All flag exists
        [TestMethod]
        public void LsCommand_HasAllFlag()
        {
            var prop = LsCommandType.GetProperty("All");
            prop.Should().NotBeNull("LsCommand must have an All property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("All must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("All must have [Argument] attribute");
            argAttr!.Name.Should().Be("all");
        }

        // T011 CV-009: All flag alias '-a'
        [TestMethod]
        public void LsCommand_AllFlag_HasAliasA()
        {
            var prop = LsCommandType.GetProperty("All");
            prop.Should().NotBeNull();
            var aliasAttr = prop!.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("All must have [Alias] attribute");
            aliasAttr!.Alias.Should().Be('a');
        }

        // T012 CV-033: LsCommand extends CommandBase
        [TestMethod]
        public void LsCommand_ExtendsCommandBase()
        {
            LsCommandType.IsSubclassOf(typeof(CommandBase)).Should().BeTrue(
                "LsCommand must extend CommandBase");
        }

        // T013 DF-001: Lists files at specified path
        [TestMethod]
        public async Task Execute_WithPath_ListsFilesAtPath()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "reports", "q1.txt"), new MockFileData("q1 data") },
                { Path.Combine(TestPaths.StorageRoot, "reports", "q2.txt"), new MockFileData("q2 data") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "reports");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("q1.txt");
            console.Output.Should().Contain("q2.txt");
        }

        // T014 DF-002: Lists subdir contents when path is a dir
        [TestMethod]
        public async Task Execute_WithSubdirPath_ListsSubdirContents()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "data", "a.txt"), new MockFileData("a data") },
                { Path.Combine(TestPaths.StorageRoot, "other", "b.txt"), new MockFileData("b data") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "data");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("a.txt");
            console.Output.Should().NotContain("b.txt");
        }

        // T015 DF-003: Glob pattern *.txt filters to text files
        [TestMethod]
        public async Task Execute_WithGlobPattern_FiltersToMatchingFiles()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "a.txt"), new MockFileData("text") },
                { Path.Combine(TestPaths.StorageRoot, "b.log"), new MockFileData("log") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "*.txt");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("a.txt");
            console.Output.Should().NotContain("b.log");
        }

        // T016 DF-004: Glob *.log matches multiple
        [TestMethod]
        public async Task Execute_WithGlobPattern_MatchesMultipleFiles()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "a.log"), new MockFileData("log1") },
                { Path.Combine(TestPaths.StorageRoot, "b.log"), new MockFileData("log2") },
                { Path.Combine(TestPaths.StorageRoot, "c.txt"), new MockFileData("text") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "*.log");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("a.log");
            console.Output.Should().Contain("b.log");
            console.Output.Should().NotContain("c.txt");
        }

        // T017 DF-005: Traverses subdirectories with --recursive
        [TestMethod]
        public async Task Execute_WithRecursive_ListsAllDepths()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "top.txt"), new MockFileData("top") },
                { Path.Combine(TestPaths.StorageRoot, "sub1", "mid.txt"), new MockFileData("mid") },
                { Path.Combine(TestPaths.StorageRoot, "sub1", "sub2", "deep.txt"), new MockFileData("deep") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("top.txt");
            console.Output.Should().Contain("mid.txt");
            console.Output.Should().Contain("deep.txt");
        }

        // T018 DF-006: Sort by file size
        [TestMethod]
        public async Task Execute_WithSortSize_OrdersByFileSize()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "large.txt"), new MockFileData(new string('x', 300)) },
                { Path.Combine(TestPaths.StorageRoot, "small.txt"), new MockFileData(new string('x', 10)) },
                { Path.Combine(TestPaths.StorageRoot, "medium.txt"), new MockFileData(new string('x', 100)) },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;
            cmd.Sort = "size";

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;
            var smallIdx = output.IndexOf("small.txt");
            var mediumIdx = output.IndexOf("medium.txt");
            var largeIdx = output.IndexOf("large.txt");

            smallIdx.Should().BeLessThan(mediumIdx, "small should appear before medium");
            mediumIdx.Should().BeLessThan(largeIdx, "medium should appear before large");
        }

        // T021 EH-001: Path not found
        [TestMethod]
        public async Task Execute_WithNonexistentPath_DisplaysNotFoundError()
        {
            var fs = new MockFileSystem();
            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = "/nonexistent";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("not found");
            console.Output.Should().Contain("/nonexistent");
        }

        // T022 EH-002: Path is a file (not a dir) and no glob — lists single file
        [TestMethod]
        public async Task Execute_WithFilePath_ListsSingleFile()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "file.txt"), new MockFileData("content") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "file.txt");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("file.txt");
            console.Output.Should().NotContain("not found", "should list the file, not show an error");
        }

        // T025 UX-001 + T026 UX-002 + T027 UX-003: Default list shows files and dirs,
        // dirs suffixed with '/', files have no trailing '/'
        [TestMethod]
        public async Task Execute_DefaultList_ShowsFilesAndDirectoriesWithCorrectSuffixes()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "report.txt"), new MockFileData("report data") },
                { Path.Combine(TestPaths.StorageRoot, "images", "photo.jpg"), new MockFileData("photo data") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;

            // UX-001: Both files and directories appear in output
            output.Should().Contain("report.txt", "files should be listed");
            output.Should().Contain("images", "directories should be listed");

            // UX-002: Directories suffixed with /
            output.Should().Contain("images/", "directories should have trailing /");

            // UX-003: Files have no trailing /
            output.Should().NotContain("report.txt/", "files should not have trailing /");
        }

        // T028 UX-004 + T029 UX-005 + T030 UX-006: Long format shows table with headers,
        // human-readable sizes, and directory size as —
        [TestMethod]
        public async Task Execute_LongFormat_ShowsTableWithHeadersAndFormattedSizes()
        {
            var fileData = new MockFileData(new byte[1_048_576]); // 1 MB
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "report.txt"), fileData },
                { Path.Combine(TestPaths.StorageRoot, "images", "photo.jpg"), new MockFileData("photo") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;
            cmd.Long = true;

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;

            // UX-004: Long format output (headers hidden, borderless table)
            // No header assertions needed — headers are hidden

            // UX-005: File size formatted as human-readable (1 MB file)
            output.Should().MatchRegex(@"1[\.,]0\s*MB", "1 MB file should show as human-readable size");

            // UX-006: Directory size column shows —
            output.Should().Contain("\u2014", "directory size should display as \u2014 (em dash)");
        }

        // T031 UX-007: Tree view shows nested entries — hierarchy visible
        [TestMethod]
        public async Task Execute_Recursive_ShowsNestedEntriesWithHierarchy()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "a", "b", "c.txt"), new MockFileData("deep") },
                { Path.Combine(TestPaths.StorageRoot, "a", "d.txt"), new MockFileData("shallow") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;

            // All entries appear
            output.Should().Contain("c.txt", "deeply nested file should appear");
            output.Should().Contain("d.txt", "shallow nested file should appear");

            // Hierarchy must be visible — relative paths must show parent directories
            // A flat list of just filenames (c.txt, d.txt) is NOT sufficient
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();

            // At least one line must show a path containing both a parent dir and filename
            // (e.g., "a/d.txt" or "a\b\c.txt" or tree indentation)
            lines.Should().Contain(l =>
                (l.Contains("a/d.txt") || l.Contains(@"a\d.txt") || l.Contains("a\\d.txt")),
                "hierarchy should show parent directory for a/d.txt");
        }

        // T032 UX-008 + T033 UX-009: Sort by size ascending and descending
        [TestMethod]
        public async Task Execute_SortBySize_OrdersByFileSizeAscAndDesc()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "big.txt"), new MockFileData(new byte[1_048_576]) },   // 1 MB
                { Path.Combine(TestPaths.StorageRoot, "small.txt"), new MockFileData(new byte[1024]) },       // 1 KB
            });

            // UX-008: --sort size → smallest first
            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;
            cmd.Sort = "size";

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;
            output.IndexOf("small.txt").Should().BeLessThan(output.IndexOf("big.txt"),
                "sort size ascending: small.txt should appear before big.txt");

            // UX-009: --sort size --reverse → largest first
            var console2 = new TestConsole();
            var cmd2 = new LsCommand(fs, new Theme());
            cmd2.SetConsole(console2);
            cmd2.Path = TestPaths.StorageRoot;
            cmd2.Sort = "size";
            cmd2.Reverse = true;

            await cmd2.Execute(new CommandExecutionContext());

            var output2 = console2.Output;
            output2.IndexOf("big.txt").Should().BeLessThan(output2.IndexOf("small.txt"),
                "sort size descending: big.txt should appear before small.txt");
        }

        // T034 UX-010: Sort by modified (oldest first)
        [TestMethod]
        public async Task Execute_SortByModified_OrdersByLastModifiedOldestFirst()
        {
            var olderFile = new MockFileData("older content");
            olderFile.LastWriteTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var newerFile = new MockFileData("newer content");
            newerFile.LastWriteTime = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);

            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "newer.txt"), newerFile },
                { Path.Combine(TestPaths.StorageRoot, "older.txt"), olderFile },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;
            cmd.Sort = "modified";

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;
            output.IndexOf("older.txt").Should().BeLessThan(output.IndexOf("newer.txt"),
                "sort by modified: older file should appear before newer file");
        }

        // T035 UX-011 + T036 UX-012: Sort by name alphabetically and reverse
        [TestMethod]
        public async Task Execute_SortByName_OrdersAlphabeticallyAndReverse()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "z.txt"), new MockFileData("z") },
                { Path.Combine(TestPaths.StorageRoot, "a.txt"), new MockFileData("a") },
            });

            // UX-011: --sort name → a.txt before z.txt
            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;
            cmd.Sort = "name";

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;
            output.IndexOf("a.txt").Should().BeLessThan(output.IndexOf("z.txt"),
                "sort by name: a.txt should appear before z.txt");

            // UX-012: --reverse (no explicit sort, defaults to name) → z.txt before a.txt
            var console2 = new TestConsole();
            var cmd2 = new LsCommand(fs, new Theme());
            cmd2.SetConsole(console2);
            cmd2.Path = TestPaths.StorageRoot;
            cmd2.Reverse = true;

            await cmd2.Execute(new CommandExecutionContext());

            var output2 = console2.Output;
            output2.IndexOf("z.txt").Should().BeLessThan(output2.IndexOf("a.txt"),
                "reverse default sort: z.txt should appear before a.txt");
        }

        // T023 EH-021: SandboxedFileSystem blocks path traversal attempt
        [TestMethod]
        public async Task Execute_WithPathTraversal_DisplaysErrorMessage()
        {
            var storageRoot = TestPaths.StorageRoot;
            var innerFs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.OutsideDir, "passwd"), new MockFileData("secret") },
            });
            var validator = new BitPantry.CommandLine.Remote.SignalR.Server.Files.PathValidator(storageRoot);
            var sandboxedFs = new BitPantry.CommandLine.Remote.SignalR.Server.Files.SandboxedFileSystem(innerFs, validator);

            var console = new TestConsole();
            var cmd = new LsCommand(sandboxedFs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = "../../etc/passwd";

            await cmd.Execute(new CommandExecutionContext());

            // Should show error, not the file content
            console.Output.Should().NotContain("secret");
            // SandboxedFileSystem throws UnauthorizedAccessException, command should catch and display error
            var output = console.Output.ToLowerInvariant();
            (output.Contains("not found") || output.Contains("error") || output.Contains("denied") || output.Contains("not allowed"))
                .Should().BeTrue("should display an error message for path traversal attempt");
        }

        // T156 EH-032: Glob pattern matches nothing — explicit message
        [TestMethod]
        public async Task Execute_GlobNoMatch_DisplaysExplicitMessage()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "readme.txt"), new MockFileData("content") },
            });
            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "*.nomatch");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("No files matching",
                "should display explicit no-matches message when glob finds nothing");
        }

        // BUG: Recursive listing with SandboxedFileSystem shows storage root prefix
        // When Path="." (default), SandboxedFileSystem resolves to absolute storage root
        // but GetRelativePath(".", absoluteEntry) computes relative to CWD, leaking "storage/"
        [TestMethod]
        public async Task Execute_Recursive_WithSandboxedFs_DoesNotShowStorageRootPrefix()
        {
            var storageRoot = TestPaths.StorageRoot;
            var innerFs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "temp", "file.txt"), new MockFileData("content") },
                { Path.Combine(TestPaths.StorageRoot, "readme.txt"), new MockFileData("readme") },
            });
            // Set CWD to parent of storage so GetRelativePath(".", ...) produces "storage\..."
            innerFs.Directory.SetCurrentDirectory(TestPaths.FileSystemRoot);

            var validator = new BitPantry.CommandLine.Remote.SignalR.Server.Files.PathValidator(storageRoot);
            var sandboxedFs = new BitPantry.CommandLine.Remote.SignalR.Server.Files.SandboxedFileSystem(innerFs, validator);

            var console = new TestConsole();
            var cmd = new LsCommand(sandboxedFs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = ".";
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;

            // Should NOT contain the storage root folder name as a prefix
            output.Should().NotContain("storage\\", "recursive listing should not leak storage root directory name");
            output.Should().NotContain("storage/", "recursive listing should not leak storage root directory name");

            // Should show relative paths from within the storage root
            output.Should().Contain("readme.txt");
            output.Should().Contain("file.txt");
        }

        // BUG: Recursive listing uses OS-native path separators (backslash on Windows)
        // Remote FS paths should use consistent forward-slash separators
        [TestMethod]
        public async Task Execute_Recursive_UsesForwardSlashSeparators()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(TestPaths.StorageRoot, "sub1", "sub2", "deep.txt"), new MockFileData("deep") },
                { Path.Combine(TestPaths.StorageRoot, "sub1", "mid.txt"), new MockFileData("mid") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs, new Theme());
            cmd.SetConsole(console);
            cmd.Path = TestPaths.StorageRoot;
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            var lines = console.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();

            // All lines with subdirectory paths should use forward slashes
            var pathLines = lines.Where(l => l.Contains("sub1")).ToList();
            pathLines.Should().NotBeEmpty("should have entries showing subdirectory paths");
            pathLines.Should().AllSatisfy(line =>
                line.Should().NotContain("\\", "paths should use forward-slash '/' not backslash '\\'"));

            // Verify the forward-slash versions ARE present
            console.Output.Should().Contain("sub1/sub2/deep.txt");
            console.Output.Should().Contain("sub1/mid.txt");
        }
    }
}
