using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

/// <summary>
/// Tests for Theme transmission via CreateClientRequest envelope.
/// Verifies that Theme round-trips through the envelope Data dictionary
/// using SpectreStyleJsonConverter via RemoteJsonOptions.
/// </summary>
[TestClass]
public class CreateClientRequestThemeTests
{
    [TestMethod]
    public void Theme_DefaultTheme_RoundTripsViaEnvelope()
    {
        // Arrange
        var theme = new Theme();

        // Act
        var request = new CreateClientRequest(theme);
        var reconstructed = new CreateClientRequest(request.Data);

        // Assert
        reconstructed.Theme.Should().NotBeNull();
        reconstructed.Theme.Group.Foreground.Should().Be(theme.Group.Foreground);
        reconstructed.Theme.Command.Foreground.Should().Be(theme.Command.Foreground);
        reconstructed.Theme.ArgumentName.Foreground.Should().Be(theme.ArgumentName.Foreground);
        reconstructed.Theme.ArgumentValue.Foreground.Should().Be(theme.ArgumentValue.Foreground);
        reconstructed.Theme.GhostText.Decoration.Should().Be(theme.GhostText.Decoration);
        reconstructed.Theme.MenuHighlight.Decoration.Should().Be(theme.MenuHighlight.Decoration);
        reconstructed.Theme.MenuGroup.Foreground.Should().Be(theme.MenuGroup.Foreground);
    }

    [TestMethod]
    public void Theme_CustomTheme_RoundTripsViaEnvelope()
    {
        // Arrange
        var theme = new Theme
        {
            Group = new Style(foreground: Color.Red),
            Command = new Style(foreground: Color.Green, decoration: Decoration.Bold),
            ArgumentName = new Style(foreground: Color.Blue),
            ArgumentAlias = new Style(foreground: Color.Magenta1),
            ArgumentValue = new Style(foreground: Color.Orange1),
            GhostText = new Style(foreground: Color.Grey, decoration: Decoration.Italic),
            Default = new Style(foreground: Color.White),
            MenuHighlight = new Style(background: Color.Blue),
            MenuGroup = new Style(foreground: Color.Yellow),
        };

        // Act
        var request = new CreateClientRequest(theme);
        var reconstructed = new CreateClientRequest(request.Data);

        // Assert
        reconstructed.Theme.Group.Foreground.Should().Be(Color.Red);
        reconstructed.Theme.Command.Foreground.Should().Be(Color.Green);
        reconstructed.Theme.Command.Decoration.Should().HaveFlag(Decoration.Bold);
        reconstructed.Theme.ArgumentName.Foreground.Should().Be(Color.Blue);
        reconstructed.Theme.ArgumentAlias.Foreground.Should().Be(Color.Magenta1);
        reconstructed.Theme.ArgumentValue.Foreground.Should().Be(Color.Orange1);
        reconstructed.Theme.GhostText.Foreground.Should().Be(Color.Grey);
        reconstructed.Theme.GhostText.Decoration.Should().HaveFlag(Decoration.Italic);
        reconstructed.Theme.Default.Foreground.Should().Be(Color.White);
        reconstructed.Theme.MenuHighlight.Background.Should().Be(Color.Blue);
        reconstructed.Theme.MenuGroup.Foreground.Should().Be(Color.Yellow);
    }

    [TestMethod]
    public void Theme_NullTheme_ReturnsNullFromEnvelope()
    {
        // Arrange — construct without theme (using data constructor with empty request)
        var request = new CreateClientRequest(new Dictionary<string, string>());
        request.RequestType = ServerRequestType.CreateClient;

        // Act
        var theme = request.Theme;

        // Assert
        theme.Should().BeNull();
    }

    [TestMethod]
    public void RequestType_WithTheme_IsCreateClient()
    {
        // Arrange & Act
        var request = new CreateClientRequest(new Theme());

        // Assert
        request.RequestType.Should().Be(ServerRequestType.CreateClient);
    }
}
