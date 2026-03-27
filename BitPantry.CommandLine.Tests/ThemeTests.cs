using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests
{
    /// <summary>
    /// Unit tests for Theme class properties and defaults.
    /// </summary>
    [TestClass]
    public class ThemeTests
    {
        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: YES (creates new Theme, reads TableHeader property)
        ///   Breakage detection: YES (would fail if default foreground/decoration changed)
        ///   Not a tautology: YES (verifies specific style values)
        /// </summary>
        [TestMethod]
        public void Theme_TableHeader_DefaultsToGreyBold()
        {
            // Arrange & Act
            var theme = new Theme();

            // Assert
            theme.TableHeader.Foreground.Should().Be(Color.Grey, "TableHeader should default to Grey foreground");
            theme.TableHeader.Decoration.Should().Be(Decoration.Bold, "TableHeader should default to Bold decoration");
        }

        /// <summary>
        /// Verifies that TableHeader can be customized.
        /// 
        /// Test Validity Check:
        ///   Invokes code under test: YES (sets TableHeader property)
        ///   Breakage detection: YES (would fail if property isn't settable)
        ///   Not a tautology: YES (verifies the custom style is applied)
        /// </summary>
        [TestMethod]
        public void Theme_TableHeader_CanBeCustomized()
        {
            // Arrange
            var customStyle = new Style(foreground: Color.Yellow, decoration: Decoration.Italic);

            // Act
            var theme = new Theme
            {
                TableHeader = customStyle
            };

            // Assert
            theme.TableHeader.Foreground.Should().Be(Color.Yellow);
            theme.TableHeader.Decoration.Should().Be(Decoration.Italic);
        }
    }
}
