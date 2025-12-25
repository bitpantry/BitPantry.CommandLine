using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete.Attributes;
using System;

namespace BitPantry.CommandLine.Tests.AutoComplete.Attributes;

[TestClass]
public class CompletionAttributeTests
{
    [TestMethod]
    public void CompletionAttribute_MethodName_ShouldStoreMethodName()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("GetCompletions");

        // Assert
        attr.MethodName.Should().Be("GetCompletions");
        attr.Values.Should().BeNull();
        attr.ProviderType.Should().BeNull();
    }

    [TestMethod]
    public void CompletionAttribute_MethodName_NullShouldThrow()
    {
        // Act
        Action act = () => new CompletionAttribute((string)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void CompletionAttribute_TwoValues_ShouldStoreAsArray()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("value1", "value2");

        // Assert
        attr.Values.Should().BeEquivalentTo(new[] { "value1", "value2" });
        attr.MethodName.Should().BeNull();
        attr.ProviderType.Should().BeNull();
    }

    [TestMethod]
    public void CompletionAttribute_MultipleValues_ShouldStoreAllValues()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("v1", "v2", "v3", "v4", "v5");

        // Assert
        attr.Values.Should().BeEquivalentTo(new[] { "v1", "v2", "v3", "v4", "v5" });
    }

    [TestMethod]
    public void CompletionAttribute_Values_NullFirstShouldThrow()
    {
        // Act
        Action act = () => new CompletionAttribute(null!, "value2");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void CompletionAttribute_Values_NullSecondShouldThrow()
    {
        // Act
        Action act = () => new CompletionAttribute("value1", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void CompletionAttribute_ProviderType_ShouldStoreType()
    {
        // Arrange & Act
        var attr = new CompletionAttribute(typeof(TestProvider));

        // Assert
        attr.ProviderType.Should().Be(typeof(TestProvider));
        attr.MethodName.Should().BeNull();
        attr.Values.Should().BeNull();
    }

    [TestMethod]
    public void CompletionAttribute_ProviderType_NullShouldThrow()
    {
        // Act
        Action act = () => new CompletionAttribute((Type)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void CompletionAttribute_CacheSeconds_DefaultIsZero()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("method");

        // Assert
        attr.CacheSeconds.Should().Be(0);
    }

    [TestMethod]
    public void CompletionAttribute_CacheSeconds_CanBeSet()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("method") { CacheSeconds = 60 };

        // Assert
        attr.CacheSeconds.Should().Be(60);
    }

    [TestMethod]
    public void CompletionAttribute_DisableGhostText_DefaultIsFalse()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("method");

        // Assert
        attr.DisableGhostText.Should().BeFalse();
    }

    [TestMethod]
    public void CompletionAttribute_MaxDisplayItems_DefaultIsTen()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("method");

        // Assert
        attr.MaxDisplayItems.Should().Be(10);
    }

    // Test helper class
    private class TestProvider { }
}

[TestClass]
public class FilePathCompletionAttributeTests
{
    [TestMethod]
    public void FilePathCompletionAttribute_ShouldInheritFromCompletionAttribute()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute();

        // Assert
        attr.Should().BeAssignableTo<CompletionAttribute>();
    }

    [TestMethod]
    public void FilePathCompletionAttribute_ShouldHaveFilePathProvider()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute();

        // Assert
        attr.ProviderType.Should().NotBeNull();
        attr.ProviderType!.Name.Should().Be("FilePathCompletionProvider");
    }

    [TestMethod]
    public void FilePathCompletionAttribute_Pattern_DefaultIsNull()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute();

        // Assert
        attr.Pattern.Should().BeNull();
    }

    [TestMethod]
    public void FilePathCompletionAttribute_Pattern_CanBeSet()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute { Pattern = "*.txt" };

        // Assert
        attr.Pattern.Should().Be("*.txt");
    }

    [TestMethod]
    public void FilePathCompletionAttribute_IncludeDirectories_DefaultIsTrue()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute();

        // Assert
        attr.IncludeDirectories.Should().BeTrue();
    }

    [TestMethod]
    public void FilePathCompletionAttribute_IncludeHidden_DefaultIsFalse()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute();

        // Assert
        attr.IncludeHidden.Should().BeFalse();
    }
}

[TestClass]
public class DirectoryPathCompletionAttributeTests
{
    [TestMethod]
    public void DirectoryPathCompletionAttribute_ShouldInheritFromCompletionAttribute()
    {
        // Arrange & Act
        var attr = new DirectoryPathCompletionAttribute();

        // Assert
        attr.Should().BeAssignableTo<CompletionAttribute>();
    }

    [TestMethod]
    public void DirectoryPathCompletionAttribute_ShouldHaveDirectoryPathProvider()
    {
        // Arrange & Act
        var attr = new DirectoryPathCompletionAttribute();

        // Assert
        attr.ProviderType.Should().NotBeNull();
        attr.ProviderType!.Name.Should().Be("DirectoryPathCompletionProvider");
    }

    [TestMethod]
    public void DirectoryPathCompletionAttribute_IncludeHidden_DefaultIsFalse()
    {
        // Arrange & Act
        var attr = new DirectoryPathCompletionAttribute();

        // Assert
        attr.IncludeHidden.Should().BeFalse();
    }
}
