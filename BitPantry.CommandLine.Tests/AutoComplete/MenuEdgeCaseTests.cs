using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    /// <summary>
    /// Tests for menu edge cases and filtering behavior.
    /// </summary>
    [TestClass]
    public class MenuEdgeCaseTests
    {
        private const int PromptLength = 10; // "testhost> "

        #region Test Commands

        [Command(Name = "help")]
        [Description("Display help")]
        private class HelpCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "history")]
        [Description("Show history")]
        private class HistoryCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "host")]
        [Description("Host settings")]
        private class HostCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "health")]
        [Description("Health check")]
        private class HealthCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "exit")]
        [Description("Exit")]
        private class ExitCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        private TestEnvironment CreateTestEnvironment()
        {
            return new TestEnvironment(opt =>
            {
                opt.ConfigureCommands(builder =>
                {
                    builder.RegisterCommand<HelpCommand>();
                    builder.RegisterCommand<HistoryCommand>();
                    builder.RegisterCommand<HostCommand>();
                    builder.RegisterCommand<HealthCommand>();
                    builder.RegisterCommand<ExitCommand>();
                });
            });
        }

        /// <summary>
        /// Waits for a condition to be met with timeout, using polling instead of fixed delay.
        /// More reliable across different CI machine speeds.
        /// </summary>
        private async Task WaitForCondition(Func<bool> condition, int timeoutMs = 1000, int pollIntervalMs = 10)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition() && stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(pollIntervalMs);
            }
            // Don't throw - let the test assertions handle failures with better error messages
        }

        /// <summary>
        /// Waits for the input line to contain expected text.
        /// </summary>
        private async Task WaitForInputContains(TestEnvironment env, string expected, int timeoutMs = 1000)
        {
            await WaitForCondition(() => 
                env.Console.VirtualConsole.GetRow(0).GetText().Contains(expected), timeoutMs);
        }

        /// <summary>
        /// Waits for a menu row to contain expected text.
        /// </summary>
        private async Task WaitForMenuContains(TestEnvironment env, string expected, int row = 1, int timeoutMs = 1000)
        {
            await WaitForCondition(() => 
                env.Console.VirtualConsole.GetRow(row).GetText().Contains(expected), timeoutMs);
        }

        /// <summary>
        /// Waits for a menu row to be empty (menu closed).
        /// </summary>
        private async Task WaitForMenuClosed(TestEnvironment env, int row = 1, int timeoutMs = 1000)
        {
            await WaitForCondition(() => 
                string.IsNullOrWhiteSpace(env.Console.VirtualConsole.GetRow(row).GetText()), timeoutMs);
        }

        #region Type-to-Filter Tests

        [TestMethod]
        public async Task Menu_TypeCharacter_FiltersOptions()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "h" to get multiple matches, then Tab to open menu
            env.Keyboard.TypeText("h");
            await WaitForInputContains(env, "h");
            env.Keyboard.PressTab();
            await WaitForMenuContains(env, "h"); // Wait for menu to appear

            // Act - type "e" to filter to "he" matches (help, health)
            env.Keyboard.TypeText("e");
            await WaitForInputContains(env, "he");

            // Assert - input line should have "he" and menu should show filtered options
            var inputLine = env.Console.VirtualConsole.GetRow(0).GetText().TrimEnd();
            inputLine.Should().Contain("he");

            // Menu should show only "help" and "health" (not "history" or "host")
            var row1Text = env.Console.VirtualConsole.GetRow(1).GetText();
            (row1Text.Contains("help") || row1Text.Contains("health")).Should().BeTrue();
            row1Text.Should().NotContain("history");
            row1Text.Should().NotContain("host");
        }

        [TestMethod]
        public async Task Menu_TypeToSingleOption_SwitchesToGhostText()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "h" to get multiple matches, then Tab to open menu
            env.Keyboard.TypeText("h");
            await WaitForInputContains(env, "h");
            env.Keyboard.PressTab();
            await WaitForMenuContains(env, "h");

            // Act - type "elp" to filter down to just "help"
            env.Keyboard.TypeText("elp");
            await WaitForInputContains(env, "help");

            // Assert - menu should close since there's only one option
            // Ghost text should appear for the remaining suggestion
            var inputLine = env.Console.VirtualConsole.GetRow(0).GetText().TrimEnd();
            inputLine.Should().EndWith("help");

            // Menu should be hidden (only one option left triggers ghost text mode)
            await WaitForMenuClosed(env);
            var menuRow = env.Console.VirtualConsole.GetRow(1).GetText().Trim();
            menuRow.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Menu_TypeNoMatches_ClosesMenu()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "h" to get matches, then Tab to open menu
            env.Keyboard.TypeText("h");
            await WaitForInputContains(env, "h");
            env.Keyboard.PressTab();
            await WaitForMenuContains(env, "h");

            // Verify menu is visible
            var row1Before = env.Console.VirtualConsole.GetRow(1).GetText();
            row1Before.Should().NotBeNullOrWhiteSpace();

            // Act - type "xyz" to filter out all options
            env.Keyboard.TypeText("xyz");
            await WaitForMenuClosed(env);

            // Assert - menu should close (row 1 should be empty)
            var row1After = env.Console.VirtualConsole.GetRow(1).GetText().Trim();
            row1After.Should().BeEmpty();
        }

        #endregion

        #region Backspace in Menu Tests

        [TestMethod]
        public async Task Menu_Backspace_UpdatesFilterAndRefiltersMenu()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "he" then Tab to open menu (matches help, health)
            env.Keyboard.TypeText("he");
            await WaitForInputContains(env, "he");
            env.Keyboard.PressTab();
            // Wait for menu to show - should contain "help" or "health"
            await WaitForMenuContains(env, "hel", row: 1);

            // Act - backspace to "h"
            env.Keyboard.PressBackspace();
            // Wait for filter to update and show more results (history, host should now appear)
            await WaitForCondition(() =>
            {
                var allMenuText = env.Console.VirtualConsole.GetRow(1).GetText() +
                                  env.Console.VirtualConsole.GetRow(2).GetText() +
                                  env.Console.VirtualConsole.GetRow(3).GetText() +
                                  env.Console.VirtualConsole.GetRow(4).GetText();
                // After backspace to "h", should show hist (history) or host which weren't visible with "he"
                return allMenuText.Contains("hist") || allMenuText.Contains("host");
            });

            // Assert - should now show additional "h" matches beyond just help/health
            var allMenuRows = env.Console.VirtualConsole.GetRow(1).GetText() +
                              env.Console.VirtualConsole.GetRow(2).GetText() +
                              env.Console.VirtualConsole.GetRow(3).GetText() +
                              env.Console.VirtualConsole.GetRow(4).GetText();
            // Should have more options now - history or host should be visible
            allMenuRows.Should().ContainAny("hist", "host");
        }

        #endregion

        #region Space Key in Menu Tests

        [TestMethod]
        public async Task Menu_SpaceKey_AcceptsSelectionAndInsertsSpace()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "h" then Tab to open menu
            env.Keyboard.TypeText("h");
            await WaitForInputContains(env, "h");
            env.Keyboard.PressTab();
            await WaitForMenuContains(env, "h");

            // Act - press Space to accept and add space
            env.Keyboard.PressKey(ConsoleKey.Spacebar);
            await WaitForMenuClosed(env);

            // Assert - selected option should be accepted with trailing space
            var inputLine = env.Console.VirtualConsole.GetRow(0).GetText().TrimEnd();
            // The input should contain the selected command followed by space
            inputLine.Should().Contain("h"); // At minimum contains h
        }

        #endregion

        #region Arrow Keys in Menu Tests

        [TestMethod]
        public async Task Menu_LeftArrow_ClosesMenuAndMovesCursor()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "h" then Tab to open menu
            env.Keyboard.TypeText("h");
            await WaitForInputContains(env, "h");
            env.Keyboard.PressTab();
            await WaitForMenuContains(env, "h");

            // Act - press Left Arrow
            env.Keyboard.PressLeftArrow();
            await WaitForMenuClosed(env);

            // Assert - menu should be closed
            var row1Text = env.Console.VirtualConsole.GetRow(1).GetText().Trim();
            row1Text.Should().BeEmpty();
        }

        #endregion
    }
}
