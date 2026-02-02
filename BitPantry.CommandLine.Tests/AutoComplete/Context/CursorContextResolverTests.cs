using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.Component;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Context
{
    /// <summary>
    /// Unit tests for CursorContextResolver.
    /// Validates that cursor position is correctly mapped to semantic context
    /// for autocomplete purposes.
    /// </summary>
    [TestClass]
    public class CursorContextResolverTests
    {
        private ICommandRegistry _registry;
        private CursorContextResolver _resolver;

        #region Test Commands and Groups

        [Group]
        [Description("Server operations")]
        private class ServerGroup
        {
            [Group]
            [Description("File operations on server")]
            public class FilesGroup { }
        }

        [Command(Group = typeof(ServerGroup), Name = "connect")]
        [Description("Connect to server")]
        private class ConnectCommand : CommandBase
        {
            [Argument]
            [Alias('t')]
            [Description("The host to connect to")]
            public string Host { get; set; }

            [Argument]
            [Alias('n')]
            [Description("The port to connect on")]
            public int Port { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(ServerGroup), Name = "disconnect")]
        [Description("Disconnect from server")]
        private class DisconnectCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(ServerGroup.FilesGroup), Name = "download")]
        [Description("Download files from server")]
        private class DownloadCommand : CommandBase
        {
            [Argument(Position = 0)]
            [Description("Remote path to download")]
            public string RemotePath { get; set; }

            [Argument(Position = 1)]
            [Description("Local path to save to")]
            public string LocalPath { get; set; }

            [Argument]
            [Description("Overwrite existing files")]
            public bool Overwrite { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(ServerGroup.FilesGroup), Name = "upload")]
        [Description("Upload files to server")]
        private class UploadCommand : CommandBase
        {
            [Argument(Position = 0)]
            [Alias('s')]
            [Description("Source path")]
            public string Source { get; set; }

            [Argument(Position = 1)]
            [Alias('d')]
            [Description("Destination path")]
            public string Destination { get; set; }

            [Argument]
            [Alias('c')]
            [Description("Use compression")]
            public bool Compress { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "help")]
        [Description("Display help")]
        private class HelpCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "exit")]
        [Description("Exit the application")]
        private class ExitCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with positional enum argument for testing positional satisfaction.
        /// </summary>
        public enum LogLevel { Debug, Info, Warning, Error }

        [Command(Name = "setlevel")]
        [Description("Set the log level")]
        private class SetLevelCommand : CommandBase
        {
            [Argument(Position = 0)]
            [Alias('l')]
            [Description("The log level")]
            public LogLevel Level { get; set; }

            [Argument]
            [Alias('v')]
            [Description("Verbose output")]
            public bool Verbose { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        [TestInitialize]
        public void Setup()
        {
            var builder = new CommandRegistryBuilder();
            builder.RegisterGroup<ServerGroup>();
            builder.RegisterGroup<ServerGroup.FilesGroup>();
            builder.RegisterCommand<ConnectCommand>();
            builder.RegisterCommand<DisconnectCommand>();
            builder.RegisterCommand<DownloadCommand>();
            builder.RegisterCommand<UploadCommand>();
            builder.RegisterCommand<HelpCommand>();
            builder.RegisterCommand<ExitCommand>();
            builder.RegisterCommand<SetLevelCommand>();

            _registry = builder.Build();
            _resolver = new CursorContextResolver(_registry);
        }

        #region Empty and Root Position Tests

        [TestMethod]
        public void Resolve_EmptyInput_ReturnsGroupOrCommandContext()
        {
            // Arrange
            var input = "";
            var cursorPosition = 1;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.GroupOrCommand);
            context.QueryText.Should().BeEmpty();
            context.ReplacementStart.Should().Be(1);
        }

        [TestMethod]
        public void Resolve_NullInput_ReturnsGroupOrCommandContext()
        {
            // Arrange
            string input = null;
            var cursorPosition = 1;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.GroupOrCommand);
        }

        [TestMethod]
        public void Resolve_PartialRootCommand_ReturnsGroupOrCommandWithQuery()
        {
            // Arrange - "hel|" cursor at end of partial input
            var input = "hel";
            var cursorPosition = 3;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.GroupOrCommand);
            context.QueryText.Should().Be("hel");
        }

        [TestMethod]
        public void Resolve_PartialGroup_ReturnsGroupOrCommandWithQuery()
        {
            // Arrange - "ser|" cursor at end, partial match for "server"
            var input = "ser";
            var cursorPosition = 3;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.GroupOrCommand);
            context.QueryText.Should().Be("ser");
        }

        #endregion

        #region Group Navigation Tests

        [TestMethod]
        public void Resolve_AfterGroup_ReturnsCommandOrSubgroupInGroupContext()
        {
            // Arrange - "server |" cursor after group, space before typing
            var input = "server ";
            var cursorPosition = 8;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - could be command (connect, disconnect) or subgroup (files)
            context.ContextType.Should().Be(CursorContextType.CommandOrSubgroupInGroup);
            context.ResolvedGroup.Should().NotBeNull();
            context.ResolvedGroup.Name.Should().Be("server");
        }

        [TestMethod]
        public void Resolve_PartialCommandOrSubgroupInGroup_ReturnsContextWithQuery()
        {
            // Arrange - "server con|" cursor at end of partial command
            var input = "server con";
            var cursorPosition = 10;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - could match command or subgroup starting with "con"
            context.ContextType.Should().Be(CursorContextType.CommandOrSubgroupInGroup);
            context.ResolvedGroup.Should().NotBeNull();
            context.QueryText.Should().Be("con");
        }

        [TestMethod]
        public void Resolve_NestedGroup_ReturnsCommandOrSubgroupInGroupContext()
        {
            // Arrange - "server files |" cursor after nested group
            var input = "server files ";
            var cursorPosition = 14;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - files group could have commands or deeper subgroups
            context.ContextType.Should().Be(CursorContextType.CommandOrSubgroupInGroup);
            context.ResolvedGroup.Should().NotBeNull();
            context.ResolvedGroup.Name.Should().Be("files");
        }

        #endregion

        #region Command Argument Tests

        [TestMethod]
        public void Resolve_AfterCommandWithNoPositionals_ReturnsEmptyContext()
        {
            // Arrange - "server connect |" cursor after command with NO positional parameters
            // Connect only has named arguments (--host, --port), no positional slots
            var input = "server connect ";
            var cursorPosition = 16;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - no positionals means no autocomplete at blank position
            // User must type -- for argument names or - for aliases
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().NotBeNull();
            context.ResolvedCommand.Name.Should().Be("connect");
        }

        [TestMethod]
        public void Resolve_PartialArgumentName_ReturnsArgumentNameWithQuery()
        {
            // Arrange - "server connect --ho|" typing argument name
            var input = "server connect --ho";
            var cursorPosition = 19;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.ArgumentName);
            context.QueryText.Should().Contain("ho");
            context.ResolvedCommand.Should().NotBeNull();
        }

        [TestMethod]
        public void Resolve_PartialAlias_ReturnsArgumentAliasContext()
        {
            // Arrange - "server connect -t|" typing alias
            var input = "server connect -t";
            var cursorPosition = 17;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.ArgumentAlias);
            context.QueryText.Should().Be("t");
        }

        [TestMethod]
        public void Resolve_AfterArgumentName_ReturnsArgumentValueContext()
        {
            // Arrange - "server connect --host |" cursor after argument name
            var input = "server connect --host ";
            var cursorPosition = 23;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - after --host, we're in value position for that argument
            context.ContextType.Should().Be(CursorContextType.ArgumentValue);
            context.ResolvedCommand.Should().NotBeNull();
            context.TargetArgument.Should().NotBeNull();
            context.TargetArgument.Name.Should().Be("Host");
        }

        [TestMethod]
        public void Resolve_DoubleDashPrefix_ReturnsArgumentNameContext()
        {
            // Arrange - "server connect --|" cursor ON the double dash
            // User explicitly typed -- so they want argument name completions
            var input = "server connect --";
            var cursorPosition = 17;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - -- prefix triggers argument name autocomplete
            context.ContextType.Should().Be(CursorContextType.ArgumentName);
            context.QueryText.Should().BeEmpty(); // No characters after --
            context.ResolvedCommand.Should().NotBeNull();
        }

        [TestMethod]
        public void Resolve_DoubleDashWithSpace_ReturnsPositionalContext()
        {
            // Arrange - "server files download -- |" cursor AFTER -- with space
            // The -- is committed as EndOfOptions, so now we're in positional territory
            var input = "server files download -- ";
            var cursorPosition = 25;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - after --, we're in positional value mode
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
        }

        [TestMethod]
        public void Resolve_PartialPrefix_ReturnsArgumentNameContext()
        {
            // Arrange - "server connect --h|" partial argument name after double dash
            var input = "server connect --h";
            var cursorPosition = 18;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - partial argument name
            context.ContextType.Should().Be(CursorContextType.ArgumentName);
            context.QueryText.Should().Be("h");
        }

        [TestMethod]
        public void Resolve_SingleDashPrefix_ReturnsArgumentAliasContext()
        {
            // Arrange - "server connect -|" cursor ON the single dash
            // User explicitly typed - so they want alias completions
            var input = "server connect -";
            var cursorPosition = 16;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - single dash prefix triggers alias autocomplete
            context.ContextType.Should().Be(CursorContextType.ArgumentAlias);
            context.QueryText.Should().BeEmpty(); // No characters after -
            context.ResolvedCommand.Should().NotBeNull();
        }

        [TestMethod]
        public void Resolve_DashAfterEndOfOptions_ReturnsPositionalContext()
        {
            // Arrange - "server files download -- -lit|" cursor on dash-prefixed value after --
            // After EndOfOptions, even dash-prefixed values are positional, not argument names
            var input = "server files download -- -lit";
            var cursorPosition = 29;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - should be positional value, not partial prefix
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
        }

        #endregion

        #region Positional Parameter Tests

        [TestMethod]
        public void Resolve_FirstPositionalSlot_ReturnsPositionalContext()
        {
            // Arrange - "server files download |" cursor at first positional slot
            var input = "server files download ";
            var cursorPosition = 23;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
            context.ResolvedCommand.Should().NotBeNull();
            context.TargetArgument.Should().NotBeNull();
            context.PositionalIndex.Should().Be(0);
        }

        [TestMethod]
        public void Resolve_SecondPositionalSlot_ReturnsCorrectIndex()
        {
            // Arrange - "server files download /remote |" cursor at second positional slot
            var input = "server files download /remote ";
            var cursorPosition = 31;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
            context.PositionalIndex.Should().Be(1);
        }

        [TestMethod]
        public void Resolve_PartialPositionalValue_ReturnsPositionalWithQuery()
        {
            // Arrange - "server files download /rem|" typing positional value
            var input = "server files download /rem";
            var cursorPosition = 26;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
            context.QueryText.Should().Be("/rem");
        }

        [TestMethod]
        public void Resolve_AllPositionalsFilled_ReturnsEmptyContext()
        {
            // Arrange - "server files download /remote ./local |" all positionals consumed
            // Download has 2 positional args (RemotePath, LocalPath) - both are filled
            var input = "server files download /remote ./local ";
            var cursorPosition = 39;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - all positionals filled, no autocomplete at blank position
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().NotBeNull();
        }

        [TestMethod]
        public void Resolve_DoubleDashPrefixWithPositionalCommand_ReturnsArgumentNameContext()
        {
            // Arrange - "server files download --|" on command WITH positionals
            // Even though download has positional args, -- explicitly requests argument names
            var input = "server files download --";
            var cursorPosition = 24;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - -- prefix always triggers argument name autocomplete
            context.ContextType.Should().Be(CursorContextType.ArgumentName);
            context.QueryText.Should().BeEmpty();
            context.ResolvedCommand.Should().NotBeNull();
        }

        [TestMethod]
        public void Resolve_SingleDashPrefixWithPositionalCommand_ReturnsArgumentAliasContext()
        {
            // Arrange - "server files download -|" on command WITH positionals
            // Even though download has positional args, - explicitly requests aliases
            var input = "server files download -";
            var cursorPosition = 23;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - single dash prefix always triggers alias autocomplete
            context.ContextType.Should().Be(CursorContextType.ArgumentAlias);
            context.QueryText.Should().BeEmpty();
            context.ResolvedCommand.Should().NotBeNull();
        }

        #endregion

        #region Used Arguments Tracking Tests

        [TestMethod]
        public void Resolve_WithUsedArgument_ExcludesFromAvailable()
        {
            // Arrange - "server connect --host localhost |" host already used
            var input = "server connect --host localhost ";
            var cursorPosition = 33;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ProvidedValues.Should().NotBeNull();
            context.ProvidedValues.Keys.Should().Contain(a => a.Name == "Host");
        }

        [TestMethod]
        public void Resolve_MultipleUsedArguments_TracksAll()
        {
            // Arrange - "server connect --host localhost --port 8080 |"
            var input = "server connect --host localhost --port 8080 ";
            var cursorPosition = 45;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ProvidedValues.Should().HaveCount(2);
            context.ProvidedValues.Keys.Should().Contain(a => a.Name == "Host");
            context.ProvidedValues.Keys.Should().Contain(a => a.Name == "Port");
        }

        [TestMethod]
        public void Resolve_AliasUsed_TracksInUsedArguments()
        {
            // Arrange - "server connect -t localhost |" alias used
            var input = "server connect -t localhost ";
            var cursorPosition = 29;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ProvidedValues.Keys.Should().Contain(a => a.Alias == 't');
        }

        [TestMethod]
        public void Resolve_CursorTouchingArgumentAlias_ReturnsArgumentAliasContext()
        {
            // Arrange - "server files upload -|c" cursor ON the dash character
            var input = "server files upload -c";
            // With 1-based positions: space at 20, '-' at 21, 'c' at 22
            var cursorPosition = 21; // cursor ON the dash

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - cursor is on the alias token, so this should be alias context
            context.ContextType.Should().Be(CursorContextType.ArgumentAlias,
                "cursor is on -c token, should offer alias completions");
            context.QueryText.Should().Be("c");
            context.ResolvedCommand.Should().NotBeNull();
        }

        [TestMethod]
        public void Resolve_CursorInSpaceBeforeExistingToken_ReturnsEmptyContext()
        {
            // Arrange - "server files upload |-c" cursor in space, but next char is token
            // This is NOT a gap where user can insert - they're adjacent to existing token
            var input = "server files upload -c";
            // With 1-based positions: space at 20, '-' at 21
            var cursorPosition = 20; // cursor ON the space before -c

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - cursor is in space but adjacent to existing token
            // No autocomplete should appear here - it's not a genuine insertion point
            context.ContextType.Should().Be(CursorContextType.Empty,
                "cursor is in space before existing token, no autocomplete");
        }

        [TestMethod]
        public void Resolve_CursorBeforeUsedArgument_TracksArgumentAsUsed()
        {
            // Arrange - "server files upload | -c" cursor in empty space before -c
            // User is inserting positional value before the already-typed -c flag
            // upload has: Source (pos 0), Destination (pos 1), Compress (-c, named)
            var input = "server files upload  -c";
            //           01234567890123456789012
            //                              ^20 (empty space, cursor here)
            // Note: double space required - cursor must be in whitespace, not touching -c
            var cursorPosition = 20;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Debug: What's actually happening?
            // Let's first just verify basic context
            context.ContextType.Should().Be(CursorContextType.PositionalValue, 
                "cursor is at space before -c, should be positional slot");
            context.ResolvedCommand.Should().NotBeNull();
            context.ResolvedCommand.Name.Should().Be("upload");
            
            // Even though -c is AFTER cursor, it should be tracked as used
            // This tests that ProvidedValues looks at ALL elements, not just before cursor
            context.ProvidedValues.Keys.Should().Contain(a => a.Alias == 'c',
                "-c appears after cursor but should still be tracked as used");
            
            // The positional index should be 0 since no positional values
            // have been provided before the cursor
            context.PositionalIndex.Should().Be(0,
                "no positional values exist before cursor position");
            context.TargetArgument.Should().NotBeNull();
            context.TargetArgument.Name.Should().Be("Source");
        }

        [TestMethod]
        public void Resolve_CursorBeforeUsedArgumentWithValue_TracksArgumentAsUsed()
        {
            // Arrange - "server files upload |./source -c" 
            // cursor between command and first positional
            var input = "server files upload  ./source -c";
            var cursorPosition = 20; // cursor at space before ./source

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - should still track -c as used even though it's after cursor
            context.ProvidedValues.Keys.Should().Contain(a => a.Alias == 'c');
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Resolve_CursorInMiddleOfWord_ReturnsCorrectElement()
        {
            // Arrange - "hel|p" cursor in middle of word
            var input = "help";
            var cursorPosition = 3;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ActiveElement.Should().NotBeNull();
        }

        [TestMethod]
        public void Resolve_LeadingWhitespace_HandlesCorrectly()
        {
            // Arrange - "   help" with leading spaces
            var input = "   help";
            var cursorPosition = 7;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.GroupOrCommand);
        }

        [TestMethod]
        public void Resolve_RootCommandCompleted_ReturnsArgumentContext()
        {
            // Arrange - "help |" cursor after root command
            var input = "help ";
            var cursorPosition = 6;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            // help command has no arguments, but should recognize context
            context.ResolvedCommand.Should().NotBeNull();
            context.ResolvedCommand.Name.Should().Be("help");
        }

        [TestMethod]
        public void Resolve_UnknownCommand_ReturnsGroupOrCommandContext()
        {
            // Arrange - "unknowncommand" that doesn't exist
            var input = "unknowncommand";
            var cursorPosition = 14;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert
            context.ContextType.Should().Be(CursorContextType.GroupOrCommand);
        }

        [TestMethod]
        public void Resolve_UnknownCommandWithSpace_ReturnsEmptyContext()
        {
            // Arrange - "unknowncommand |" unknown command with trailing space
            // The space indicates commitment - user is done typing that token
            var input = "unknowncommand ";
            var cursorPosition = 15;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - committed token doesn't resolve to anything known
            // Resolver definitively knows there's no valid autocomplete here
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().BeNull();
            context.ResolvedGroup.Should().BeNull();
        }

        [TestMethod]
        public void Resolve_UnknownGroupAndCommand_ReturnsEmptyContext()
        {
            // Arrange - "unknowngroup unknowncommand |" multiple unknown tokens
            var input = "unknowngroup unknowncommand ";
            var cursorPosition = 28;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - first token already doesn't resolve, so entire path is invalid
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().BeNull();
            context.ResolvedGroup.Should().BeNull();
        }

        [TestMethod]
        public void Resolve_UnknownCommandWithDoubleDash_ReturnsEmptyContext()
        {
            // Arrange - "unknowncommand --|" unknown command with -- prefix
            // Even though -- normally triggers argument name completion,
            // the base command is invalid so no autocomplete is possible
            var input = "unknowncommand --";
            var cursorPosition = 17;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - can't provide argument suggestions for unknown command
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().BeNull();
        }

        [TestMethod]
        public void Resolve_UnknownCommandWithSingleDash_ReturnsEmptyContext()
        {
            // Arrange - "unknowncommand -|" unknown command with - prefix
            var input = "unknowncommand -";
            var cursorPosition = 16;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - can't provide alias suggestions for unknown command
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().BeNull();
        }

        [TestMethod]
        public void Resolve_UnknownCommandWithArgument_ReturnsEmptyContext()
        {
            // Arrange - "unknowncommand --arg |" unknown command with argument
            var input = "unknowncommand --arg ";
            var cursorPosition = 21;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - can't provide value suggestions for unknown command's argument
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().BeNull();
        }

        [TestMethod]
        public void Resolve_UnknownCommandWithArgumentValue_ReturnsEmptyContext()
        {
            // Arrange - "unknowncommand --arg value |" unknown command with full arg
            var input = "unknowncommand --arg value ";
            var cursorPosition = 27;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - entire path is invalid
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().BeNull();
        }

        #endregion

        #region Multiple Spaces Tests

        [TestMethod]
        public void Resolve_MultipleSpacesAfterGroup_ReturnsCommandOrSubgroupContext()
        {
            // Arrange - "server     |" multiple spaces after group
            var input = "server     ";
            var cursorPosition = 12;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - should still recognize group context despite extra spaces
            context.ContextType.Should().Be(CursorContextType.CommandOrSubgroupInGroup);
            context.ResolvedGroup.Should().NotBeNull();
            context.ResolvedGroup.Name.Should().Be("server");
        }

        [TestMethod]
        public void Resolve_MultipleSpacesAfterCommandWithPositionals_ReturnsPositionalContext()
        {
            // Arrange - "server files download       |" multiple spaces after command
            var input = "server files download       ";
            var cursorPosition = 29;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - should still offer positional completions
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
            context.ResolvedCommand.Should().NotBeNull();
            context.ResolvedCommand.Name.Should().Be("download");
            context.PositionalIndex.Should().Be(0);
        }

        [TestMethod]
        public void Resolve_MultipleSpacesAfterArgumentName_ReturnsArgumentValueContext()
        {
            // Arrange - "server connect --host     |" multiple spaces after argument name
            var input = "server connect --host     ";
            var cursorPosition = 27;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - should still recognize we're in value position for --host
            context.ContextType.Should().Be(CursorContextType.ArgumentValue);
            context.TargetArgument.Should().NotBeNull();
            context.TargetArgument.Name.Should().Be("Host");
        }

        [TestMethod]
        public void Resolve_MultipleSpacesBetweenPositionals_ReturnsCorrectIndex()
        {
            // Arrange - "server files download /remote       |" multiple spaces after first positional
            var input = "server files download /remote       ";
            var cursorPosition = 37;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - should recognize second positional slot
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
            context.PositionalIndex.Should().Be(1);
        }

        [TestMethod]
        public void Resolve_MultipleSpacesBetweenGroupAndCommand_ReturnsCorrectContext()
        {
            // Arrange - "server     files     download     |"
            var input = "server     files     download     ";
            var cursorPosition = 35;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - should resolve through multiple spaces to the command
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
            context.ResolvedCommand.Should().NotBeNull();
            context.ResolvedCommand.Name.Should().Be("download");
        }

        [TestMethod]
        public void Resolve_MultipleSpacesAfterCommandWithNoPositionals_ReturnsEmptyContext()
        {
            // Arrange - "server connect       |" command with no positionals
            var input = "server connect       ";
            var cursorPosition = 22;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - no positionals means Empty, regardless of space count
            context.ContextType.Should().Be(CursorContextType.Empty);
            context.ResolvedCommand.Should().NotBeNull();
            context.ResolvedCommand.Name.Should().Be("connect");
        }

        #endregion

        #region Gap 1: Positional Satisfaction via Named Arguments

        /// <summary>
        /// 008:UX-032 - When a positional-capable argument is set by name,
        /// the positional slot should be considered satisfied.
        /// </summary>
        [TestMethod]
        public void Resolve_PositionalSetByName_NoPositionalAutocomplete()
        {
            // Arrange - "setlevel --level Debug |" Level has Position=0, set by name
            var input = "setlevel --level Debug ";
            var cursorPosition = 24;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - positional slot 0 is satisfied via --level, no positional autocomplete
            context.ContextType.Should().Be(CursorContextType.Empty,
                because: "positional slot 0 is satisfied by --level Debug");
            context.ResolvedCommand.Should().NotBeNull();
            context.ResolvedCommand.Name.Should().Be("setlevel");
        }

        /// <summary>
        /// When a positional-capable argument is set by name, it should appear in UsedArguments.
        /// </summary>
        [TestMethod]
        public void Resolve_PositionalSetByName_ArgumentInUsedArguments()
        {
            // Arrange - "setlevel --level Debug |"
            var input = "setlevel --level Debug ";
            var cursorPosition = 24;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - Level argument should be in ProvidedValues
            context.ProvidedValues.Keys.Should().Contain(a => a.Name == "Level",
                because: "--level Debug satisfies the Level argument");
        }

        /// <summary>
        /// When a positional-capable argument is set positionally, it should appear in UsedArguments.
        /// <summary>
        /// Implements: 008:UX-031
        /// When a positional-capable argument is set positionally, it should appear in UsedArguments,
        /// which excludes it from -- suggestions.
        /// </summary>
        [TestMethod]
        public void Resolve_PositionalSetPositionally_ArgumentInUsedArguments()
        {
            // Arrange - "setlevel Debug |" Level has Position=0, set positionally
            var input = "setlevel Debug ";
            var cursorPosition = 16;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - Level argument should be in ProvidedValues  
            context.ProvidedValues.Keys.Should().Contain(a => a.Name == "Level",
                because: "Debug at position 0 satisfies the Level argument");
        }

        /// <summary>
        /// When a positional-capable argument is set by alias, the positional slot should be satisfied.
        /// </summary>
        [TestMethod]
        public void Resolve_PositionalSetByAlias_NoPositionalAutocomplete()
        {
            // Arrange - "setlevel -l Debug |" Level has alias 'l'
            var input = "setlevel -l Debug ";
            var cursorPosition = 19;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - positional slot 0 is satisfied via -l
            context.ContextType.Should().Be(CursorContextType.Empty,
                because: "positional slot 0 is satisfied by -l Debug");
        }

        #endregion

        #region Gap 2: Positional Syntax Validity After Named Arguments

        /// <summary>
        /// 008:UX-034 - After a named argument appears in input, 
        /// positional values are syntactically invalid.
        /// </summary>
        [TestMethod]
        public void Resolve_AfterNamedArg_NoPositionalAutocomplete()
        {
            // Arrange - "server files upload --compress |" 
            // Source (pos 0) and Destination (pos 1) are unfilled but named arg appeared
            var input = "server files upload --compress ";
            var cursorPosition = 32;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - positional syntax is blocked, but --compress is a bool pending value
            // Per 008:UX-015, bool arguments get autocomplete for true/false values
            context.ContextType.Should().Be(CursorContextType.ArgumentValue,
                because: "bool argument --compress is pending a value (true/false autocomplete)");
            context.TargetArgument.Should().NotBeNull();
            context.TargetArgument.Name.Should().Be("Compress");
            context.ResolvedCommand.Should().NotBeNull();
            context.ResolvedCommand.Name.Should().Be("upload");
        }

        /// <summary>
        /// After an alias appears in input, positional values are syntactically invalid.
        /// However, if the alias is for a bool argument pending a value, we get ArgumentValue context.
        /// </summary>
        [TestMethod]
        public void Resolve_AfterAlias_NoPositionalAutocomplete()
        {
            // Arrange - "server files upload -c |" where -c is alias for bool --compress
            var input = "server files upload -c ";
            var cursorPosition = 24;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - positional syntax is blocked, but -c is a bool pending value
            // Per 008:UX-015, bool arguments get autocomplete for true/false values
            context.ContextType.Should().Be(CursorContextType.ArgumentValue,
                because: "bool argument -c (Compress) is pending a value (true/false autocomplete)");
            context.TargetArgument.Should().NotBeNull();
            context.TargetArgument.Name.Should().Be("Compress");
        }

        /// <summary>
        /// 008:UX-033 - When a non-positional named arg is set, unfilled positionals
        /// still can't use positional syntax (must use named).
        /// However, if the named arg is a bool pending a value, we get ArgumentValue context.
        /// </summary>
        [TestMethod]
        public void Resolve_NamedArgSetButPositionalUnfilled_NoPositionalAutocomplete()
        {
            // Arrange - "setlevel --verbose |" Verbose is bool (named-only), Level (pos 0) unfilled
            var input = "setlevel --verbose ";
            var cursorPosition = 20;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - positional syntax is blocked, but --verbose is a bool pending value
            // Per 008:UX-015, bool arguments get autocomplete for true/false values
            context.ContextType.Should().Be(CursorContextType.ArgumentValue,
                because: "bool argument --verbose is pending a value (true/false autocomplete)");
            context.TargetArgument.Should().NotBeNull();
            context.TargetArgument.Name.Should().Be("Verbose");
        }

        /// <summary>
        /// Before any named argument appears, positional autocomplete should work.
        /// </summary>
        [TestMethod]
        public void Resolve_BeforeAnyNamedArg_PositionalAutocompleteWorks()
        {
            // Arrange - "setlevel |" no named args yet
            var input = "setlevel ";
            var cursorPosition = 10;

            // Act
            var context = _resolver.Resolve(input, cursorPosition);

            // Assert - positional autocomplete should be offered for Level (pos 0)
            context.ContextType.Should().Be(CursorContextType.PositionalValue);
            context.TargetArgument.Should().NotBeNull();
            context.TargetArgument.Name.Should().Be("Level");
        }

        #endregion
    }
}
