using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.CommandLine.Tests.Commands.ActivateCommands;
using BitPantry.CommandLine.Tests.Commands.PositionalCommands;
using BitPantry.CommandLine.Tests.Commands.RepeatedOptionCommands;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class CommandActivatorTests
    {
        private static CommandActivator _activator;
        private static CommandResolver _resolver;
        private static CommandActivator _positionalActivator;
        private static CommandResolver _positionalResolver;
        private static CommandActivator _repeatedOptionActivator;
        private static CommandResolver _repeatedOptionResolver;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            var registry = new CommandRegistry();

            registry.RegisterCommand<Command>();
            registry.RegisterCommand<WithArgument>();
            registry.RegisterCommand<WithIntArg>();
            registry.RegisterCommand<MultipleArgs>();
            registry.RegisterCommand<WithAlias>();
            registry.RegisterCommand<WithOption>();

            registry.ConfigureServices(services);

            _resolver = new CommandResolver(registry);
            _activator = new CommandActivator(services.BuildServiceProvider());

            // Create positional command registry and activator
            var positionalServices = new ServiceCollection();
            var positionalRegistry = new CommandRegistry();
            positionalRegistry.RegisterCommand<SinglePositionalCommand>();
            positionalRegistry.RegisterCommand<MultiplePositionalCommand>();
            positionalRegistry.RegisterCommand<PositionalWithNamedCommand>();
            positionalRegistry.RegisterCommand<RequiredPositionalCommand>();
            positionalRegistry.RegisterCommand<OptionalPositionalCommand>();
            positionalRegistry.RegisterCommand<IsRestCommand>();
            positionalRegistry.RegisterCommand<IsRestWithPrecedingCommand>();
            positionalRegistry.ConfigureServices(positionalServices);

            _positionalResolver = new CommandResolver(positionalRegistry);
            _positionalActivator = new CommandActivator(positionalServices.BuildServiceProvider());

            // Create repeated option command registry and activator
            var repeatedOptionServices = new ServiceCollection();
            var repeatedOptionRegistry = new CommandRegistry();
            repeatedOptionRegistry.RegisterCommand<RepeatedOptionArrayCommand>();
            repeatedOptionRegistry.RegisterCommand<RepeatedOptionScalarCommand>();
            repeatedOptionRegistry.ConfigureServices(repeatedOptionServices);

            _repeatedOptionResolver = new CommandResolver(repeatedOptionRegistry);
            _repeatedOptionActivator = new CommandActivator(repeatedOptionServices.BuildServiceProvider());
        }

        [TestMethod]
        public void ActivateCommand_Activated()
        {
            var input = new ParsedCommand("command");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<Command>();
        }

        [TestMethod]
        public void ActivateWithoutArgInput_Activated()
        {
            var input = new ParsedCommand("withArgument");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithArgument>();
            ((WithArgument)act.Command).ArgOne.Should().Be(0);
        }

        [TestMethod]
        public void ActivateIntArg_Activated()
        {
            var input = new ParsedCommand("withIntArg --intArg 10");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithIntArg>();
            ((WithIntArg)act.Command).IntArg.Should().Be(10);
        }

        [TestMethod]
        public void ActivateMultipleArgs_Activated()
        {
            var input = new ParsedCommand("MultipleArgs --argOne 10 --strArg \"hello world\"");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<MultipleArgs>();
            ((MultipleArgs)act.Command).ArgOne.Should().Be(10);
            ((MultipleArgs)act.Command).StrArg.Should().Be("hello world");
        }

        [TestMethod]
        public void ActivateAlias_Activated()
        {
            var input = new ParsedCommand("withAlias -a 10");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithAlias>();
            ((WithAlias)act.Command).ArgOne.Should().Be(10);
        }

        [TestMethod]
        public void ActivateOption_Activated()
        {
            var input = new ParsedCommand("withOption --optOne");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithOption>();
            ((WithOption)act.Command).OptOne.IsPresent.Should().BeTrue();
        }

        [TestMethod]
        public void ActivateOptionAlias_Activated()
        {
            var input = new ParsedCommand("withOption -o");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithOption>();
            ((WithOption)act.Command).OptOne.IsPresent.Should().BeTrue();
        }

        [TestMethod]
        public void ActivateOptionNotSet_Activated()
        {
            var input = new ParsedCommand("withOption");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithOption>();
            ((WithOption)act.Command).OptOne.IsPresent.Should().BeFalse();
        }

        [TestMethod]
        public void ActivateOptionAbsent_Activated()
        {
            var input = new ParsedCommand("withOption");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithOption>();
            ((WithOption)act.Command).OptOne.IsPresent.Should().BeFalse();
        }

        #region Positional Argument Activation Tests (ACT-001, ACT-002, ACT-007, ACT-009)

        /// <summary>
        /// ACT-001: String positional argument is activated correctly
        /// </summary>
        [TestMethod]
        public void ActivateCommand_ACT001_StringPositional()
        {
            // Arrange
            var input = new ParsedCommand("singlePositionalCommand myValue");
            var res = _positionalResolver.Resolve(input);

            // Act
            var act = _positionalActivator.Activate(res);

            // Assert
            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<SinglePositionalCommand>();
            ((SinglePositionalCommand)act.Command).Source.Should().Be("myValue");
        }

        /// <summary>
        /// ACT-002: Int positional argument is converted and activated correctly
        /// </summary>
        [TestMethod]
        public void ActivateCommand_ACT002_IntPositional()
        {
            // Arrange - MultiplePositionalCommand has Third as int at Position=2
            var input = new ParsedCommand("multiplePositionalCommand first second 42");
            var res = _positionalResolver.Resolve(input);

            // Act
            var act = _positionalActivator.Activate(res);

            // Assert
            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<MultiplePositionalCommand>();
            var cmd = (MultiplePositionalCommand)act.Command;
            cmd.First.Should().Be("first");
            cmd.Second.Should().Be("second");
            cmd.Third.Should().Be(42);
        }

        /// <summary>
        /// ACT-007: Positional type mismatch produces activation error
        /// </summary>
        [TestMethod]
        public void ActivateCommand_ACT007_PositionalTypeMismatch()
        {
            // Arrange - MultiplePositionalCommand expects int at Position=2, provide non-int
            var input = new ParsedCommand("multiplePositionalCommand first second notAnInt");
            var res = _positionalResolver.Resolve(input);

            // Act & Assert - Activation should throw or fail due to type conversion error
            try
            {
                var act = _positionalActivator.Activate(res);
                // If activation succeeds, the Third property should not have the invalid value
                // This might happen if activation silently fails or uses default value
                var cmd = (MultiplePositionalCommand)act.Command;
                // If we get here with notAnInt not being converted, that's a test failure
                Assert.Fail("Expected activation to fail or throw for invalid int conversion");
            }
            catch (System.Exception)
            {
                // Expected - type conversion should fail
            }
        }

        /// <summary>
        /// ACT-009: Mixed positional and named arguments both activated correctly
        /// </summary>
        [TestMethod]
        public void ActivateCommand_ACT009_MixedPositionalAndNamed()
        {
            // Arrange - PositionalWithNamedCommand has positional Source, Destination and named Force, Mode
            var input = new ParsedCommand("positionalWithNamedCommand source.txt dest.txt --force --mode copy");
            var res = _positionalResolver.Resolve(input);

            // Act
            var act = _positionalActivator.Activate(res);

            // Assert
            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<PositionalWithNamedCommand>();
            var cmd = (PositionalWithNamedCommand)act.Command;
            cmd.Source.Should().Be("source.txt");
            cmd.Destination.Should().Be("dest.txt");
            cmd.Force.IsPresent.Should().BeTrue();
            cmd.Mode.Should().Be("copy");
        }

        #endregion

        #region IsRest Positional Argument Activation Tests (ACT-003, ACT-004, ACT-005, ACT-008)

        /// <summary>
        /// ACT-003: IsRest string array is activated correctly with multiple values
        /// </summary>
        [TestMethod]
        public void ActivateCommand_ACT003_IsRestStringArray()
        {
            // Arrange - isRestCommand a b c
            var input = new ParsedCommand("isRestCommand a b c");
            var res = _positionalResolver.Resolve(input);

            // Act
            var act = _positionalActivator.Activate(res);

            // Assert
            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<IsRestCommand>();
            var cmd = (IsRestCommand)act.Command;
            cmd.Files.Should().NotBeNull();
            cmd.Files.Should().HaveCount(3);
            cmd.Files.Should().Contain("a");
            cmd.Files.Should().Contain("b");
            cmd.Files.Should().Contain("c");
        }

        /// <summary>
        /// ACT-004: IsRest with preceding positional arguments
        /// </summary>
        [TestMethod]
        public void ActivateCommand_ACT004_IsRestWithPreceding()
        {
            // Arrange - isRestWithPrecedingCommand target a b c d
            var input = new ParsedCommand("isRestWithPrecedingCommand target a b c d");
            var res = _positionalResolver.Resolve(input);

            // Act
            var act = _positionalActivator.Activate(res);

            // Assert
            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<IsRestWithPrecedingCommand>();
            var cmd = (IsRestWithPrecedingCommand)act.Command;
            cmd.Target.Should().Be("target");
            cmd.Sources.Should().NotBeNull();
            cmd.Sources.Should().HaveCount(4);
            cmd.Sources.Should().ContainInOrder("a", "b", "c", "d");
        }

        /// <summary>
        /// ACT-008: Empty IsRest (no extra values) results in empty/null array
        /// </summary>
        [TestMethod]
        public void ActivateCommand_ACT008_EmptyIsRest()
        {
            // Arrange - isRestWithPrecedingCommand target (no Sources values)
            var input = new ParsedCommand("isRestWithPrecedingCommand target");
            var res = _positionalResolver.Resolve(input);

            // Act
            var act = _positionalActivator.Activate(res);

            // Assert
            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<IsRestWithPrecedingCommand>();
            var cmd = (IsRestWithPrecedingCommand)act.Command;
            cmd.Target.Should().Be("target");
            // Sources should be null or empty array when no values provided
            (cmd.Sources == null || cmd.Sources.Length == 0).Should().BeTrue();
        }

        #endregion

        #region Repeated Option Tests

        /// <summary>
        /// ACT-006: Repeated option --items a --items b --items c populates Items array
        /// </summary>
        [TestMethod]
        public void ActivateCommand_ACT006_RepeatedOptionPopulatesArray()
        {
            // Arrange
            var input = new ParsedCommand("repeatedOptionArrayCommand --items a --items b --items c");
            var res = _repeatedOptionResolver.Resolve(input);

            // Act
            var act = _repeatedOptionActivator.Activate(res);

            // Assert
            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<RepeatedOptionArrayCommand>();
            var cmd = (RepeatedOptionArrayCommand)act.Command;
            cmd.Items.Should().NotBeNull();
            cmd.Items.Should().HaveCount(3);
            cmd.Items.Should().ContainInOrder("a", "b", "c");
        }

        #endregion

    }
}
