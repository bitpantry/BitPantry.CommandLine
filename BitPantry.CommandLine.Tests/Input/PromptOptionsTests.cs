using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Input
{
    [TestClass]
    public class PromptOptionsTests
    {
        #region CV-012: Name() sets AppName property

        /// <summary>
        /// Implements: CV-012
        /// When PromptOptions.Name() called, Then AppName property set to provided value
        /// </summary>
        [TestMethod]
        public void Name_SetsAppName()
        {
            // Arrange
            var options = new PromptOptions();

            // Act
            options.Name("myapp");

            // Assert
            options.AppName.Should().Be("myapp");
        }

        /// <summary>
        /// Implements: CV-012 (markup support)
        /// When PromptOptions.Name() called with Spectre markup, Then AppName stores markup
        /// </summary>
        [TestMethod]
        public void Name_SupportsMarkup()
        {
            // Arrange
            var options = new PromptOptions();

            // Act
            options.Name("[bold cyan]myapp[/]");

            // Assert
            options.AppName.Should().Be("[bold cyan]myapp[/]");
        }

        /// <summary>
        /// Implements: CV-012 (fluent API)
        /// When PromptOptions.Name() called, Then returns same instance for chaining
        /// </summary>
        [TestMethod]
        public void Name_ReturnsOptionsForChaining()
        {
            // Arrange
            var options = new PromptOptions();

            // Act
            var result = options.Name("test");

            // Assert
            result.Should().BeSameAs(options);
        }

        #endregion

        #region CV-013: WithSuffix() sets Suffix property

        /// <summary>
        /// Implements: CV-013
        /// When PromptOptions.WithSuffix() called, Then Suffix property set to provided value
        /// </summary>
        [TestMethod]
        public void WithSuffix_SetsSuffix()
        {
            // Arrange
            var options = new PromptOptions();

            // Act
            options.WithSuffix("$ ");

            // Assert
            options.Suffix.Should().Be("$ ");
        }

        /// <summary>
        /// Implements: CV-013 (markup support)
        /// When PromptOptions.WithSuffix() called with Spectre markup, Then Suffix stores markup
        /// </summary>
        [TestMethod]
        public void WithSuffix_SupportsMarkup()
        {
            // Arrange
            var options = new PromptOptions();

            // Act
            options.WithSuffix("[green]>[/] ");

            // Assert
            options.Suffix.Should().Be("[green]>[/] ");
        }

        /// <summary>
        /// Implements: CV-013 (fluent API)
        /// When PromptOptions.WithSuffix() called, Then returns same instance for chaining
        /// </summary>
        [TestMethod]
        public void WithSuffix_ReturnsOptionsForChaining()
        {
            // Arrange
            var options = new PromptOptions();

            // Act
            var result = options.WithSuffix("$ ");

            // Assert
            result.Should().BeSameAs(options);
        }

        #endregion

        #region CV-014: Default state has correct values

        /// <summary>
        /// Implements: CV-014
        /// When PromptOptions default state, Then Suffix is "> "
        /// </summary>
        [TestMethod]
        public void DefaultState_SuffixIsDefaultPrompt()
        {
            // Arrange & Act
            var options = new PromptOptions();

            // Assert
            options.Suffix.Should().Be("> ");
        }

        /// <summary>
        /// Implements: CV-014
        /// When PromptOptions default state, Then AppName is null
        /// </summary>
        [TestMethod]
        public void DefaultState_AppNameIsNull()
        {
            // Arrange & Act
            var options = new PromptOptions();

            // Assert
            options.AppName.Should().BeNull();
        }

        #endregion

        #region Fluent API chaining

        /// <summary>
        /// Validates fluent API works correctly for typical usage pattern
        /// </summary>
        [TestMethod]
        public void FluentChaining_CanChainMultipleCalls()
        {
            // Arrange & Act
            var options = new PromptOptions()
                .Name("myapp")
                .WithSuffix("$ ");

            // Assert
            options.AppName.Should().Be("myapp");
            options.Suffix.Should().Be("$ ");
        }

        #endregion
    }
}
