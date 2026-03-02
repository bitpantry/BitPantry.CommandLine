using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Server;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests;

/// <summary>
/// Tests for ThemeHolder — the scoped service that bridges Hub context to DI.
/// </summary>
[TestClass]
public class ThemeHolderTests
{
    [TestMethod]
    public void Theme_Default_ReturnsStandardTheme()
    {
        // Arrange
        var holder = new ThemeHolder();

        // Act
        var theme = holder.Theme;

        // Assert
        theme.Should().NotBeNull();
        theme.Group.Foreground.Should().Be(Color.Cyan);
        theme.Command.Should().Be(Style.Plain);
    }

    [TestMethod]
    public void SetTheme_WithCustomTheme_ReplacesDefault()
    {
        // Arrange
        var holder = new ThemeHolder();
        var custom = new Theme { Group = new Style(foreground: Color.Red) };

        // Act
        holder.SetTheme(custom);

        // Assert
        holder.Theme.Should().BeSameAs(custom);
        holder.Theme.Group.Foreground.Should().Be(Color.Red);
    }

    [TestMethod]
    public void SetTheme_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var holder = new ThemeHolder();

        // Act
        var act = () => holder.SetTheme(null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void SetTheme_CalledTwice_UsesLatest()
    {
        // Arrange
        var holder = new ThemeHolder();
        var first = new Theme { Group = new Style(foreground: Color.Red) };
        var second = new Theme { Group = new Style(foreground: Color.Blue) };

        // Act
        holder.SetTheme(first);
        holder.SetTheme(second);

        // Assert
        holder.Theme.Should().BeSameAs(second);
        holder.Theme.Group.Foreground.Should().Be(Color.Blue);
    }
}
