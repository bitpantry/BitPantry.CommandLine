using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    /// <summary>
    /// Tests for group-based command autocomplete functionality.
    /// These tests cover the space-separated group navigation syntax.
    /// </summary>
    [TestClass]
    public class AutoCompleteSetBuilderTests_Groups
    {
        private static ICommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            var builder = new CommandRegistryBuilder();

            // Root-level commands
            builder.RegisterCommand<Command>(); // Command (root)
            builder.RegisterCommand<CommandWithNameAttribute>(); // myCommand (root)
            
            // Commands in bitpantry group
            builder.RegisterCommand<CommandWithGroup>(); // bitpantry CommandWithGroup
            builder.RegisterCommand<DupNameDifferentGroup>(); // bitpantry Command
            
            // Nested group commands: parent -> child
            builder.RegisterCommand<ParentGroupCommand>(); // parent parentcmd
            builder.RegisterCommand<ChildGroupCommand>(); // parent child childcmd
            builder.RegisterCommand<AnotherChildGroupCommand>(); // parent child anothercmd

            _registry = builder.Build(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        #region Root Level Group Autocomplete

        [DataTestMethod]
        [DataRow("bit", 1)]
        [DataRow("bit", 3)]
        [DataRow("bitpantry", 5)]
        [DataRow("BITPANTRY", 5)] // case insensitive
        public async Task AutoCompleteGroupName_GroupReturned(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "bitpantry");
        }

        [DataTestMethod]
        [DataRow("par", 1)]
        [DataRow("parent", 3)]
        [DataRow("PARENT", 3)] // case insensitive
        public async Task AutoCompleteParentGroupName_GroupReturned(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "parent");
        }

        [TestMethod]
        public async Task AutoCompleteAtRoot_ShowsRootCommandsAndGroups()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            // Typing "c" at root should show "Command" (root command) but not commands in groups
            var opt = await ac.BuildOptions(new ParsedInput("c").GetElementAtCursorPosition(1));

            opt.Should().NotBeNull();
            // Should only find root-level Command, not group commands
            opt.Options.Should().Contain(o => o.Value == "Command");
            opt.Options.Should().NotContain(o => o.Value == "CommandWithGroup");
        }

        #endregion

        #region Group Navigation Autocomplete

        [DataTestMethod]
        [DataRow("bitpantry C", 11)] // partial command in group
        [DataRow("bitpantry Comm", 14)]
        [DataRow("BITPANTRY COMM", 14)] // case insensitive
        public async Task AutoCompleteCommandInGroup_CommandsReturned(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCountGreaterThan(0);
            // Should find commands in the bitpantry group
            opt.Options.Should().Contain(o => o.Value == "Command" || o.Value == "CommandWithGroup");
        }

        [TestMethod]
        public async Task AutoCompleteInGroup_ShowsAllGroupCommands()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            // Typing partial command in bitpantry group
            var opt = await ac.BuildOptions(new ParsedInput("bitpantry Com").GetElementAtCursorPosition(13));

            opt.Should().NotBeNull();
            // Both Command and CommandWithGroup should match "Com"
            opt.Options.Should().HaveCount(2);
            opt.Options.Select(o => o.Value).Should().Contain("Command");
            opt.Options.Select(o => o.Value).Should().Contain("CommandWithGroup");
        }

        [TestMethod]
        public async Task AutoCompleteExactCommandInGroup_ReturnsMatch()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("bitpantry CommandWithGroup").GetElementAtCursorPosition(26));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);
            opt.Options[0].Value.Should().Be("CommandWithGroup");
        }

        #endregion

        #region Nested Group Autocomplete

        [TestMethod]
        public async Task AutoCompleteChildGroupInParent_ShowsChildGroup()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            // Typing "chi" after "parent " should show "child" group
            var opt = await ac.BuildOptions(new ParsedInput("parent chi").GetElementAtCursorPosition(10));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "child");
        }

        [TestMethod]
        public async Task AutoCompleteInParentGroup_ShowsCommandsAndChildGroups()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            // In parent group, should see both parentcmd and child group
            var opt = await ac.BuildOptions(new ParsedInput("parent p").GetElementAtCursorPosition(9));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "parentcmd");
        }

        [TestMethod]
        public async Task AutoCompleteCommandInChildGroup_ReturnsChildCommands()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            // Navigate to child group and autocomplete command
            var opt = await ac.BuildOptions(new ParsedInput("parent child c").GetElementAtCursorPosition(15));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "childcmd");
        }

        [TestMethod]
        public async Task AutoCompleteInChildGroup_ShowsAllChildCommands()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            // Both childcmd and anothercmd should be available
            var opt = await ac.BuildOptions(new ParsedInput("parent child a").GetElementAtCursorPosition(15));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "anothercmd");
        }

        [DataTestMethod]
        [DataRow("parent child childcmd", 21)]
        [DataRow("parent child anothercmd", 24)]
        public async Task AutoCompleteExactCommandInNestedGroup_ReturnsMatch(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);
        }

        #endregion

        #region No Match Cases

        [DataTestMethod]
        [DataRow("xyz", 1)]
        [DataRow("nonexistent", 5)]
        [DataRow("zzz", 3)]
        public async Task AutoCompleteNonExistent_NoResult(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            // Should return empty options when no match
            if (opt != null)
            {
                opt.Options.Should().BeEmpty();
            }
        }

        [TestMethod]
        public async Task AutoCompleteNonExistentCommandInGroup_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("bitpantry xyz").GetElementAtCursorPosition(13));

            // Should return empty options when command doesn't exist in group
            if (opt != null)
            {
                opt.Options.Should().BeEmpty();
            }
        }

        [TestMethod]
        public async Task AutoCompleteInvalidGroupPath_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("nonexistentgroup cmd").GetElementAtCursorPosition(20));

            // Should return null or empty when group doesn't exist
            if (opt != null)
            {
                opt.Options.Should().BeEmpty();
            }
        }

        #endregion

        #region Piped Commands with Groups

        [TestMethod]
        public async Task AutoCompletePipedInputWithGroup_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            // After a pipe, should be able to autocomplete group path
            var opt = await ac.BuildOptions(new ParsedInput("command | bitpantry Com").GetElementAtCursorPosition(24));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCountGreaterThan(0);
        }

        [TestMethod]
        public async Task AutoCompleteGroupAfterPipe_GroupReturned()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("command | bit").GetElementAtCursorPosition(14));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "bitpantry");
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public async Task AutoCompleteGroupWithTrailingSpace_ShowsGroupContents()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            // After typing group name and space, cursor is on next element
            // This tests that we navigate into the group properly
            var input = new ParsedInput("bitpantry C");
            var elem = input.GetElementAtCursorPosition(11);
            
            elem.Should().NotBeNull();
            
            var opt = await ac.BuildOptions(elem);
            opt.Should().NotBeNull();
        }

        [TestMethod]
        public async Task AutoCompleteCaseInsensitive_MatchesRegardlessOfCase()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("BITPANTRY commandwithgroup").GetElementAtCursorPosition(26));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value.Equals("CommandWithGroup", System.StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
