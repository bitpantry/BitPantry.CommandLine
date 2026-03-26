using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Tests.PluginModule;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace BitPantry.CommandLine.Tests
{
    /// <summary>
    /// Tests for the ICommandModule system - in-process module registration and assembly loading.
    /// </summary>
    [TestClass]
    public class CommandModuleTests
    {
        #region In-Process Module Registration Tests

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModule&lt;T&gt;() is called
        ///   Breakage detection: [YES] - If commands aren't registered, assertions fail
        ///   Not a tautology: [YES] - Tests actual command registration behavior
        /// </summary>
        [TestMethod]
        public void InstallModule_RegistersCommands_CommandsInRegistry()
        {
            // Arrange & Act
            var app = new CommandLineApplicationBuilder()
                .InstallModule<TestPluginModule>()
                .Build();

            // Assert
            var registry = app.Services.GetRequiredService<ICommandRegistry>();
            registry.Commands.Should().Contain(c => c.Name == "plugin-greet");
            registry.Commands.Should().Contain(c => c.Name == "plugin-echo");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModule for GroupedCommandsModule
        ///   Breakage detection: [YES] - Group and commands must be present with correct hierarchy
        ///   Not a tautology: [YES] - Tests actual group registration via module
        /// </summary>
        [TestMethod]
        public void InstallModule_RegistersGroupedCommands_GroupAndCommandsInRegistry()
        {
            // Arrange & Act
            var app = new CommandLineApplicationBuilder()
                .InstallModule<GroupedCommandsModule>()
                .Build();

            // Assert
            var registry = app.Services.GetRequiredService<ICommandRegistry>();
            
            // Check group is registered
            registry.Groups.Should().Contain(g => g.Name == "plugin-math");
            var group = registry.Groups.First(g => g.Name == "plugin-math");
            
            // Check commands are in the group
            group.Commands.Should().Contain(c => c.Name == "add");
            group.Commands.Should().Contain(c => c.Name == "subtract");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModule with DI service registration
        ///   Breakage detection: [YES] - Service resolution would fail if not registered
        ///   Not a tautology: [YES] - Tests actual DI registration via module context
        /// </summary>
        [TestMethod]
        public void InstallModule_RegistersDIServices_ServicesResolvable()
        {
            // Arrange & Act
            var app = new CommandLineApplicationBuilder()
                .InstallModule<TestPluginModule>()
                .Build();

            // Assert
            var service = app.Services.GetService<IPluginService>();
            service.Should().NotBeNull();
            service!.GetValue().Should().Be("default");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModule with autocomplete handler registration
        ///   Breakage detection: [YES] - Handler type would not be in DI if not registered
        ///   Not a tautology: [YES] - Tests actual autocomplete handler registration via module context
        /// </summary>
        [TestMethod]
        public void InstallModule_RegistersAutoCompleteHandler_HandlerInRegistry()
        {
            // Arrange & Act
            var app = new CommandLineApplicationBuilder()
                .InstallModule<AutoCompleteTestModule>()
                .Build();

            // Assert - The handler type should be registered with DI
            var handler = app.Services.GetService<TestDateTimeAutoCompleteHandler>();
            handler.Should().NotBeNull("autocomplete handler should be registered via module context");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModule with configuration action
        ///   Breakage detection: [YES] - Service value would be "default" if config not applied
        ///   Not a tautology: [YES] - Tests that configuration action runs before Configure
        /// </summary>
        [TestMethod]
        public void InstallModule_WithConfiguration_ConfigurationApplied()
        {
            // Arrange & Act
            var app = new CommandLineApplicationBuilder()
                .InstallModule<TestPluginModule>(m => m.ConfigurableOption = "configured-value")
                .Build();

            // Assert
            var service = app.Services.GetService<IPluginService>();
            service.Should().NotBeNull();
            service!.GetValue().Should().Be("configured-value");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModule for two different modules
        ///   Breakage detection: [YES] - Commands from both must be present
        ///   Not a tautology: [YES] - Tests multi-module installation
        /// </summary>
        [TestMethod]
        public void InstallModule_MultipleModules_AllCommandsRegistered()
        {
            // Arrange & Act
            var app = new CommandLineApplicationBuilder()
                .InstallModule<TestPluginModule>()
                .InstallModule<GroupedCommandsModule>()
                .Build();

            // Assert
            var registry = app.Services.GetRequiredService<ICommandRegistry>();
            
            // Commands from TestPluginModule
            registry.Commands.Should().Contain(c => c.Name == "plugin-greet");
            registry.Commands.Should().Contain(c => c.Name == "plugin-echo");
            
            // Commands from GroupedCommandsModule (via group)
            registry.Groups.Should().Contain(g => g.Name == "plugin-math");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - Two modules registering same command name
        ///   Breakage detection: [YES] - Must throw on duplicate registration
        ///   Not a tautology: [YES] - Tests duplicate command validation
        /// </summary>
        [TestMethod]
        public void InstallModule_DuplicateCommandAcrossModules_ThrowsOnConflict()
        {
            // Arrange
            var builder = new CommandLineApplicationBuilder()
                .InstallModule<DuplicateCommandModule1>();

            // Act & Assert - The exception is thrown during InstallModule, not Build
            // This is correct behavior: duplicate commands are detected at registration time
            Action installDuplicate = () => builder.InstallModule<DuplicateCommandModule2>();
            installDuplicate.Should().Throw<ArgumentException>()
                .WithMessage("*already registered*");
        }

        #endregion

        #region Assembly Loading Tests

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModulesFromDirectory with non-existent path
        ///   Breakage detection: [YES] - Would throw if not implemented as no-op
        ///   Not a tautology: [YES] - Tests graceful handling of missing directory
        /// </summary>
        [TestMethod]
        public void InstallModulesFromDirectory_DirectoryNotExists_NoOp()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var builder = new CommandLineApplicationBuilder()
                .InstallModulesFromDirectory(nonExistentPath);

            // Assert - should not throw and build should succeed
            var app = builder.Build();
            app.Should().NotBeNull();
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModulesFromDirectory with empty directory
        ///   Breakage detection: [YES] - Would throw if directory iteration fails
        ///   Not a tautology: [YES] - Tests handling of empty plugins directory
        /// </summary>
        [TestMethod]
        public void InstallModulesFromDirectory_EmptyDirectory_NoOp()
        {
            // Arrange
            var emptyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(emptyDir);
            
            try
            {
                // Act
                var builder = new CommandLineApplicationBuilder()
                    .InstallModulesFromDirectory(emptyDir);

                // Assert
                var app = builder.Build();
                app.Should().NotBeNull();
            }
            finally
            {
                Directory.Delete(emptyDir, true);
            }
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModuleFromAssembly with invalid path
        ///   Breakage detection: [YES] - Must throw with clear error including path
        ///   Not a tautology: [YES] - Tests error handling for missing assemblies
        /// </summary>
        [TestMethod]
        public void InstallModuleFromAssembly_InvalidPath_ThrowsWithPath()
        {
            // Arrange
            var invalidPath = Path.Combine(Path.GetTempPath(), "nonexistent", "plugin.dll");

            // Act
            Action act = () => new CommandLineApplicationBuilder()
                .InstallModuleFromAssembly(invalidPath);

            // Assert
            act.Should().Throw<FileNotFoundException>()
                .WithMessage($"*{invalidPath}*");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModuleFromAssembly with real plugin DLL
        ///   Breakage detection: [YES] - Plugin commands must appear in registry
        ///   Not a tautology: [YES] - Tests actual assembly loading and module discovery
        /// </summary>
        [TestMethod]
        public void InstallModuleFromAssembly_ValidPath_CommandsRegistered()
        {
            // Arrange - Get the path to the compiled plugin assembly
            // Note: This test requires the plugin module to be built and its output path known
            var pluginAssemblyPath = GetPluginModuleAssemblyPath();
            
            if (!File.Exists(pluginAssemblyPath))
            {
                Assert.Inconclusive($"Plugin assembly not found at: {pluginAssemblyPath}");
            }

            // Act
            var app = new CommandLineApplicationBuilder()
                .InstallModuleFromAssembly(pluginAssemblyPath)
                .Build();

            // Assert
            var registry = app.Services.GetRequiredService<ICommandRegistry>();
            
            // TestPluginModule and GroupedCommandsModule are both in the assembly
            registry.Commands.Should().Contain(c => c.Name == "plugin-greet");
            registry.Commands.Should().Contain(c => c.Name == "plugin-echo");
            registry.Groups.Should().Contain(g => g.Name == "plugin-math");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - InstallModulesFromDirectory with plugin subdirectory
        ///   Breakage detection: [YES] - Commands must appear in registry
        ///   Not a tautology: [YES] - Tests directory-based plugin loading convention
        /// </summary>
        [TestMethod]
        public void InstallModulesFromDirectory_ValidPlugins_CommandsRegistered()
        {
            // Arrange - Create a plugins directory structure
            var pluginsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var pluginSubdir = Path.Combine(pluginsDir, "BitPantry.CommandLine.Tests.PluginModule");
            Directory.CreateDirectory(pluginSubdir);

            try
            {
                // Copy the plugin assembly to the subdirectory
                var sourceAssemblyPath = GetPluginModuleAssemblyPath();
                if (!File.Exists(sourceAssemblyPath))
                {
                    Assert.Inconclusive($"Plugin assembly not found at: {sourceAssemblyPath}");
                }

                var destAssemblyPath = Path.Combine(pluginSubdir, "BitPantry.CommandLine.Tests.PluginModule.dll");
                File.Copy(sourceAssemblyPath, destAssemblyPath);

                // Act
                var app = new CommandLineApplicationBuilder()
                    .InstallModulesFromDirectory(pluginsDir)
                    .Build();

                // Assert
                var registry = app.Services.GetRequiredService<ICommandRegistry>();
                registry.Commands.Should().Contain(c => c.Name == "plugin-greet");
            }
            finally
            {
                Directory.Delete(pluginsDir, true);
            }
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - Loads plugin, gets command, casts to CommandBase
        ///   Breakage detection: [YES] - Cast would fail if type mismatch
        ///   Not a tautology: [YES] - Tests shared type resolution across load contexts
        /// </summary>
        [TestMethod]
        public void ModuleLoadContext_SharedHostTypes_NoTypeMismatch()
        {
            // Arrange
            var pluginAssemblyPath = GetPluginModuleAssemblyPath();
            
            if (!File.Exists(pluginAssemblyPath))
            {
                Assert.Inconclusive($"Plugin assembly not found at: {pluginAssemblyPath}");
            }

            // Act
            var app = new CommandLineApplicationBuilder()
                .InstallModuleFromAssembly(pluginAssemblyPath)
                .Build();

            // Assert - Get a command type from the registry and verify it's assignable to CommandBase
            var registry = app.Services.GetRequiredService<ICommandRegistry>();
            var pluginCommand = registry.Commands.First(c => c.Name == "plugin-greet");
            
            // This verifies the Type in the registry is the correct type from the plugin
            typeof(CommandBase).IsAssignableFrom(pluginCommand.Type).Should().BeTrue();
            
            // Verify we can resolve and cast the command
            var cmd = app.Services.GetService(pluginCommand.Type);
            cmd.Should().NotBeNull();
            (cmd is CommandBase).Should().BeTrue();
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: [YES] - Runs plugin command with DI-injected service
        ///   Breakage detection: [YES] - Command execution would fail without DI
        ///   Not a tautology: [YES] - Tests end-to-end DI injection for plugin commands
        /// </summary>
        [TestMethod]
        public async System.Threading.Tasks.Task InstallModule_CommandExecution_DIInjectionWorks()
        {
            // Arrange
            var app = new CommandLineApplicationBuilder()
                .InstallModule<TestPluginModule>(m => m.ConfigurableOption = "test-value")
                .Build();

            // Act - Run the echo command which depends on IPluginService
            var result = await app.RunOnce("plugin-echo Hello");

            // Assert - The command executed successfully (no exception)
            result.ResultCode.Should().Be(Processing.Execution.RunResultCode.Success);
        }

        #endregion

        #region Helper Methods

        private static string GetPluginModuleAssemblyPath()
        {
            // Navigate from the test assembly to the plugin module assembly
            var testAssemblyDir = AppContext.BaseDirectory;
            
            // The plugin module is in a sibling project with the same configuration
            // e.g., from BitPantry.CommandLine.Tests/bin/Debug/net8.0
            // to BitPantry.CommandLine.Tests.PluginModule/bin/Debug/net8.0
            var solutionDir = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", "..", ".."));
            return Path.Combine(solutionDir, "BitPantry.CommandLine.Tests.PluginModule", "bin", "Debug", "net8.0", 
                "BitPantry.CommandLine.Tests.PluginModule.dll");
        }

        #endregion

        #region Test Helper Modules

        /// <summary>
        /// Module that registers a command named "duplicate-cmd" for conflict testing.
        /// </summary>
        public class DuplicateCommandModule1 : ICommandModule
        {
            public string Name => "DuplicateModule1";

            public void Configure(ICommandModuleContext context)
            {
                context.Commands.RegisterCommand(typeof(DuplicateTestCommand1));
            }
        }

        /// <summary>
        /// Another module that registers a command with the same name for conflict testing.
        /// </summary>
        public class DuplicateCommandModule2 : ICommandModule
        {
            public string Name => "DuplicateModule2";

            public void Configure(ICommandModuleContext context)
            {
                context.Commands.RegisterCommand(typeof(DuplicateTestCommand2));
            }
        }

        [Command(Name = "duplicate-cmd")]
        public class DuplicateTestCommand1 : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "duplicate-cmd")]
        public class DuplicateTestCommand2 : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion
    }
}
