using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Tests.AnsiConsole;
using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
using BitPantry.CommandLine.Tests.AnsiConsole;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class AutoCompleteControllerTests
    {
        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry(services);

            _registry.RegisterCommand<Command>(); // Command
            _registry.RegisterCommand<CommandWithNameAttribute>(); // myCommand
            _registry.RegisterCommand<MultipleArgumentsAndAliases>(); // MultipleArgumentsAndAliases propertyTwo|p prop|X
            _registry.RegisterCommand<CommandWithNamespace>(); // BitPantry.CommandWithNamespace
            _registry.RegisterCommand<DupNameDifferentNamespace>(); // BitPantry.Command

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public void CommandName_Success()
        {
            var console = new TestAnsiConsole();
            var consoleSvc = new TestConsoleService(console);
            console.Write("$ com");

            var ctrl = new AutoCompleteController(console, consoleSvc, _registry, _serviceProvider);
            ctrl.BeginAutoComplete("com", 2);           

            
        }
    }
}
