using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using System;

namespace BitPantry.CommandLine.Tests.AutoComplete;

[TestClass]
public class CompletionContextTests
{
    [TestMethod]
    public void CompletionContext_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var context = new CompletionContext();

        // Assert
        context.InputText.Should().BeEmpty();
        context.CursorPosition.Should().Be(0);
        context.CommandName.Should().BeNull();
        context.ArgumentName.Should().BeNull();
        context.PartialValue.Should().BeEmpty();
        context.ElementType.Should().Be(CompletionElementType.Empty);
        context.PropertyType.Should().BeNull();
        context.CompletionAttribute.Should().BeNull();
        context.IsRemote.Should().BeFalse();
        context.CommandInstance.Should().BeNull();
        context.ParsedArguments.Should().BeNull();
    }

    [TestMethod]
    public void CompletionContext_WithInitializer_ShouldSetProperties()
    {
        // Arrange & Act
        var context = new CompletionContext
        {
            InputText = "test command",
            CursorPosition = 5,
            CommandName = "test",
            ArgumentName = "--file",
            PartialValue = "com",
            ElementType = CompletionElementType.Command,
            PropertyType = typeof(string),
            IsRemote = true
        };

        // Assert
        context.InputText.Should().Be("test command");
        context.CursorPosition.Should().Be(5);
        context.CommandName.Should().Be("test");
        context.ArgumentName.Should().Be("--file");
        context.PartialValue.Should().Be("com");
        context.ElementType.Should().Be(CompletionElementType.Command);
        context.PropertyType.Should().Be(typeof(string));
        context.IsRemote.Should().BeTrue();
    }
}

[TestClass]
public class CompletionItemTests
{
    [TestMethod]
    public void CompletionItem_WithInsertText_ShouldUseAsDisplayText()
    {
        // Arrange & Act
        var item = new CompletionItem { InsertText = "test" };

        // Assert
        item.InsertText.Should().Be("test");
        item.DisplayText.Should().Be("test"); // Falls back to InsertText
        item.Description.Should().BeNull();
        item.Kind.Should().Be(CompletionItemKind.Command); // Default
        item.SortPriority.Should().Be(0);
        item.MatchScore.Should().Be(0);
        item.MatchRanges.Should().BeEmpty();
    }

    [TestMethod]
    public void CompletionItem_WithExplicitDisplayText_ShouldNotFallBack()
    {
        // Arrange & Act
        var item = new CompletionItem
        {
            InsertText = "insert-value",
            DisplayText = "Display Value"
        };

        // Assert
        item.InsertText.Should().Be("insert-value");
        item.DisplayText.Should().Be("Display Value");
    }

    [TestMethod]
    public void CompletionItem_MatchRanges_DefaultShouldBeEmpty()
    {
        // Arrange & Act
        var item = new CompletionItem { InsertText = "testvalue" };

        // Assert
        item.MatchRanges.Should().BeEmpty();
    }
}

[TestClass]
public class CompletionResultTests
{
    [TestMethod]
    public void CompletionResult_Empty_ShouldHaveNoItems()
    {
        // Act
        var result = CompletionResult.Empty;

        // Assert
        result.Items.Should().BeEmpty();
        result.IsCached.Should().BeFalse();
        result.IsTimedOut.Should().BeFalse();
        result.IsError.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.TotalCount.Should().Be(0);
    }

    [TestMethod]
    public void CompletionResult_TimedOut_ShouldIndicateTimeout()
    {
        // Act
        var result = CompletionResult.TimedOut;

        // Assert
        result.IsTimedOut.Should().BeTrue();
        result.IsError.Should().BeFalse();
    }

    [TestMethod]
    public void CompletionResult_Error_ShouldContainMessage()
    {
        // Act
        var result = CompletionResult.Error("Test error message");

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorMessage.Should().Be("Test error message");
        result.Items.Should().BeEmpty();
    }

    [TestMethod]
    public void CompletionResult_WithItems_ShouldReflectTotalCount()
    {
        // Arrange
        var items = new[]
        {
            new CompletionItem { InsertText = "item1" },
            new CompletionItem { InsertText = "item2" }
        };

        // Act
        var result = new CompletionResult { Items = items, TotalCount = 2 };

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }
}

[TestClass]
public class CompletionElementTypeTests
{
    [TestMethod]
    public void CompletionElementType_ShouldHaveExpectedValues()
    {
        // Assert all expected enum values exist
        Enum.IsDefined(typeof(CompletionElementType), CompletionElementType.Empty).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionElementType), CompletionElementType.Command).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionElementType), CompletionElementType.ArgumentName).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionElementType), CompletionElementType.ArgumentAlias).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionElementType), CompletionElementType.ArgumentValue).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionElementType), CompletionElementType.Positional).Should().BeTrue();
    }
}

[TestClass]
public class CompletionItemKindTests
{
    [TestMethod]
    public void CompletionItemKind_ShouldHaveExpectedValues()
    {
        // Assert all expected enum values exist
        Enum.IsDefined(typeof(CompletionItemKind), CompletionItemKind.Command).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionItemKind), CompletionItemKind.CommandGroup).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionItemKind), CompletionItemKind.ArgumentName).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionItemKind), CompletionItemKind.ArgumentAlias).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionItemKind), CompletionItemKind.ArgumentValue).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionItemKind), CompletionItemKind.File).Should().BeTrue();
        Enum.IsDefined(typeof(CompletionItemKind), CompletionItemKind.Directory).Should().BeTrue();
    }
}
