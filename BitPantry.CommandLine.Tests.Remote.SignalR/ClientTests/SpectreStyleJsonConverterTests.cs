using System.Text.Json;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

/// <summary>
/// Tests for SpectreStyleJsonConverter — verifies Style serialization/deserialization
/// through System.Text.Json, and RemoteJsonOptions converter registration.
/// </summary>
[TestClass]
public class SpectreStyleJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new SpectreStyleJsonConverter() }
    };

    #region Serialize Tests

    [TestMethod]
    public void Serialize_CyanForeground_WritesMarkupString()
    {
        // Arrange
        var style = new Style(foreground: Color.Cyan);

        // Act
        var json = JsonSerializer.Serialize(style, Options);

        // Assert — should serialize as a JSON string that round-trips back to Cyan
        // Spectre uses "aqua" as the canonical name for Color.Cyan
        var markup = JsonSerializer.Deserialize<string>(json);
        markup.Should().NotBeNullOrEmpty();
        Style.Parse(markup).Foreground.Should().Be(Color.Cyan);
    }

    [TestMethod]
    public void Serialize_BoldCyanStyle_WritesCompoundMarkup()
    {
        // Arrange
        var style = new Style(foreground: Color.Cyan, decoration: Decoration.Bold);

        // Act
        var json = JsonSerializer.Serialize(style, Options);

        // Assert — Spectre uses "aqua" as the canonical name for Color.Cyan in markup
        var markup = JsonSerializer.Deserialize<string>(json);
        markup.Should().Contain("bold");
        // Round-trip verifies the name maps back correctly regardless of canonical name
        var parsed = Style.Parse(markup);
        parsed.Foreground.Should().Be(Color.Cyan);
        parsed.Decoration.Should().HaveFlag(Decoration.Bold);
    }

    #endregion

    #region Deserialize Tests

    [TestMethod]
    public void Deserialize_CyanMarkup_ReturnsCyanStyle()
    {
        // Arrange
        var json = "\"cyan\"";

        // Act
        var style = JsonSerializer.Deserialize<Style>(json, Options);

        // Assert
        style.Foreground.Should().Be(Color.Cyan);
    }

    [TestMethod]
    public void Deserialize_EmptyString_ReturnsPlainStyle()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var style = JsonSerializer.Deserialize<Style>(json, Options);

        // Assert
        style.Should().Be(Style.Plain);
    }

    [TestMethod]
    public void Deserialize_NullJson_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act — Style is a reference type; JsonSerializer handles null tokens directly
        // without invoking the converter's Read method (HandleNull defaults to false)
        var style = JsonSerializer.Deserialize<Style>(json, Options);

        // Assert — null is the correct representation for "no style"
        style.Should().BeNull();
    }

    #endregion

    #region Round-Trip Tests

    [TestMethod]
    public void RoundTrip_CyanStyle_PreservesForeground()
    {
        // Arrange
        var original = new Style(foreground: Color.Cyan);

        // Act
        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<Style>(json, Options);

        // Assert
        deserialized.Foreground.Should().Be(original.Foreground);
    }

    [TestMethod]
    public void RoundTrip_BoldCyanStyle_PreservesAllProperties()
    {
        // Arrange
        var original = new Style(foreground: Color.Cyan, decoration: Decoration.Bold);

        // Act
        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<Style>(json, Options);

        // Assert
        deserialized.Foreground.Should().Be(original.Foreground);
        deserialized.Decoration.Should().Be(original.Decoration);
    }

    [TestMethod]
    public void RoundTrip_AutoCompleteOptionWithMenuStyle_PreservesStyle()
    {
        // Arrange — real-world scenario: AutoCompleteOption with MenuStyle serialized for remote
        var option = new AutoCompleteOption("docs/", menuStyle: new Style(foreground: Color.Cyan));

        // Act
        var json = JsonSerializer.Serialize(option, RemoteJsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<AutoCompleteOption>(json, RemoteJsonOptions.Default);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be("docs/");
        deserialized.MenuStyle.Should().NotBeNull();
        deserialized.MenuStyle!.Foreground.Should().Be(Color.Cyan);
    }

    #endregion

    #region RemoteJsonOptions Wiring

    [TestMethod]
    public void RemoteJsonOptions_Default_ContainsSpectreStyleJsonConverter()
    {
        // Act
        var converters = RemoteJsonOptions.Default.Converters;

        // Assert
        converters.Should().Contain(c => c is SpectreStyleJsonConverter);
    }

    #endregion
}
