using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Tests.Commands.PositionalCommands;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    /// <summary>
    /// AutoComplete tests for positional arguments
    /// </summary>
    [TestClass]
    public class AutoCompleteSetBuilderTests_Positional
    {
        private static ICommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            var builder = new CommandRegistryBuilder();

            builder.RegisterCommand<PositionalWithAutoCompleteCommand>();
            builder.RegisterCommand<SinglePositionalCommand>(); // No autocomplete function
            builder.RegisterCommand<IsRestCommand>(); // For IsRest tests

            _registry = builder.Build(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// AC-001: First positional slot - cursor after command name invokes pos0 autocomplete
        /// Note: When cursor is at end with trailing space, autocomplete shows command/arg names.
        /// User needs to start typing to get positional autocomplete.
        /// </summary>
        [TestMethod]
        public async Task AC001_FirstPositionalSlot_InvokesPos0AutoComplete()
        {
            // Arrange - "positionalWithAutoCompleteCommand f" with cursor at end (user started typing)
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var input = "positionalWithAutoCompleteCommand f";
            var parsedInput = new ParsedInput(input);
            var element = parsedInput.GetElementAtCursorPosition(input.Length);
            
            // Debug: Show all elements (accessing through ParsedCommands)
            System.Console.WriteLine($"AC001 Input length: {input.Length}");
            var parsedCmd = parsedInput.ParsedCommands.First();
            System.Console.WriteLine($"AC001 Element count: {parsedCmd.Elements.Count}");
            foreach (var el in parsedCmd.Elements)
            {
                System.Console.WriteLine($"  {el.ElementType}: '{el.Value}' @ {el.StartPosition}-{el.EndPosition}");
            }
            System.Console.WriteLine($"AC001 Cursor element type: {element.ElementType}");
            System.Console.WriteLine($"AC001 Cursor element value: '{element.Value}'");

            // Act
            var opt = await ac.BuildOptions(element);

            // Assert
            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "file1.txt");
            opt.Options.Should().Contain(o => o.Value == "file2.txt");
        }

        /// <summary>
        /// AC-002: Second positional slot - with first value typed and partial second value, invokes pos1 autocomplete
        /// Note: When cursor is at end with trailing space after a value, user needs to start typing
        /// to get next positional autocomplete.
        /// </summary>
        [TestMethod]
        public async Task AC002_SecondPositionalSlot_InvokesPos1AutoComplete()
        {
            // Arrange - "positionalWithAutoCompleteCommand file1.txt r" with partial second value
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var input = "positionalWithAutoCompleteCommand file1.txt r";
            var element = new ParsedInput(input).GetElementAtCursorPosition(input.Length);

            // Act
            var opt = await ac.BuildOptions(element);

            // Assert
            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "read");
        }

        /// <summary>
        /// AC-004: No autocomplete function - no suggestions returned
        /// </summary>
        [TestMethod]
        public async Task AC004_NoAutoCompleteFunction_NoSuggestions()
        {
            // Arrange - SinglePositionalCommand has no autocomplete function
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var input = "singlePositionalCommand ";
            var element = new ParsedInput(input).GetElementAtCursorPosition(input.Length);

            // Act
            var opt = await ac.BuildOptions(element);

            // Assert - should be null or no options because there's no autocomplete function
            // (Note: might return group/command suggestions instead, so we check for positional arg suggestions)
            if (opt != null)
            {
                opt.Options.Should().NotContain(o => o.Value.Contains("positional"));
            }
        }

        /// <summary>
        /// AC-005: Context has prior values - when typing partial second positional,
        /// context should contain the first positional value
        /// </summary>
        [TestMethod]
        public async Task AC005_ContextHasPriorValues()
        {
            // Arrange - "positionalWithAutoCompleteCommand file1.txt r" with partial second value
            // The Mode autocomplete function adds "has-context" if prior values exist
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var input = "positionalWithAutoCompleteCommand file1.txt r";
            var element = new ParsedInput(input).GetElementAtCursorPosition(input.Length);

            // Act
            var opt = await ac.BuildOptions(element);

            // Assert - "has-context" should be in options if context was passed correctly
            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "has-context");
        }

        /// <summary>
        /// AC-006: After named option - named arg completion still works
        /// </summary>
        [TestMethod]
        public async Task AC006_AfterNamedOption_NamedArgCompletion()
        {
            // Arrange - "positionalWithAutoCompleteCommand --" cursor position after "--"
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var input = "positionalWithAutoCompleteCommand --";
            var element = new ParsedInput(input).GetElementAtCursorPosition(input.Length);

            // Act
            var opt = await ac.BuildOptions(element);

            // Assert - should suggest argument names (FileName, Mode, Verbose)
            opt.Should().NotBeNull();
            opt.Options.Should().HaveCountGreaterOrEqualTo(1);
            // Check that at least one of the options is a valid argument
            opt.Options.Select(o => o.Value).Should().Contain(v => v.Contains("Verbose", System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// AC-007: Partial positional - filter by partial value
        /// </summary>
        [TestMethod]
        public async Task AC007_PartialPositional_FilteredByPartialValue()
        {
            // Arrange - "positionalWithAutoCompleteCommand fi" with cursor at end
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var input = "positionalWithAutoCompleteCommand fi";
            var element = new ParsedInput(input).GetElementAtCursorPosition(input.Length);

            // Act
            var opt = await ac.BuildOptions(element);

            // Assert - should contain file1.txt and file2.txt (starting with "fi") but highlight current matching
            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "file1.txt");
            opt.Options.Should().Contain(o => o.Value == "file2.txt");
        }
    }
}
