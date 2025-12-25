using System;
using BitPantry.CommandLine.AutoComplete.Attributes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Attributes;

/// <summary>
/// Tests for shortcut attributes (FilePathCompletionAttribute, DirectoryPathCompletionAttribute) - SA-001 to SA-005.
/// </summary>
[TestClass]
public class ShortcutAttributeTests
{
    #region SA-001: FilePathCompletionAttribute

    [TestMethod]
    [TestCategory("SA-001")]
    public void FilePathCompletionAttribute_InheritsFromCompletionAttribute()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute();

        // Assert
        attr.Should().BeAssignableTo<CompletionAttribute>();
    }

    [TestMethod]
    [TestCategory("SA-001")]
    public void FilePathCompletionAttribute_DefaultValues()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute();

        // Assert
        attr.Pattern.Should().BeNull();
        attr.IncludeDirectories.Should().BeTrue();
        attr.IncludeHidden.Should().BeFalse();
        attr.BasePath.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("SA-001")]
    public void FilePathCompletionAttribute_WithPattern()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute { Pattern = "*.txt" };

        // Assert
        attr.Pattern.Should().Be("*.txt");
    }

    [TestMethod]
    [TestCategory("SA-001")]
    public void FilePathCompletionAttribute_WithBasePath()
    {
        // Arrange & Act
        var attr = new FilePathCompletionAttribute { BasePath = @"C:\Projects" };

        // Assert
        attr.BasePath.Should().Be(@"C:\Projects");
    }

    #endregion

    #region SA-002: DirectoryPathCompletionAttribute

    [TestMethod]
    [TestCategory("SA-002")]
    public void DirectoryPathCompletionAttribute_InheritsFromCompletionAttribute()
    {
        // Arrange & Act
        var attr = new DirectoryPathCompletionAttribute();

        // Assert
        attr.Should().BeAssignableTo<CompletionAttribute>();
    }

    [TestMethod]
    [TestCategory("SA-002")]
    public void DirectoryPathCompletionAttribute_DefaultValues()
    {
        // Arrange & Act
        var attr = new DirectoryPathCompletionAttribute();

        // Assert
        attr.BasePath.Should().BeNull();
        attr.IncludeHidden.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("SA-002")]
    public void DirectoryPathCompletionAttribute_WithBasePath()
    {
        // Arrange & Act
        var attr = new DirectoryPathCompletionAttribute { BasePath = @"C:\Projects" };

        // Assert
        attr.BasePath.Should().Be(@"C:\Projects");
    }

    #endregion

    #region SA-003: CompletionAttribute with method name

    [TestMethod]
    [TestCategory("SA-003")]
    public void CompletionAttribute_WithMethodName_StoresMethod()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("GetCompletions");

        // Assert
        attr.MethodName.Should().Be("GetCompletions");
        attr.Values.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("SA-003")]
    public void CompletionAttribute_EmptyMethodName_Allowed()
    {
        // Arrange & Act
        var attr = new CompletionAttribute("");

        // Assert
        attr.MethodName.Should().Be("");
    }

    #endregion

    #region SA-004: CompletionAttribute with static values

    [TestMethod]
    [TestCategory("SA-004")]
    public void CompletionAttribute_WithStaticValues_StoresValues()
    {
        // Arrange & Act
        var attr = new CompletionAttribute(new[] { "value1", "value2", "value3" });

        // Assert
        attr.Values.Should().NotBeNull();
        attr.Values.Should().HaveCount(3);
        attr.Values.Should().Contain(new[] { "value1", "value2", "value3" });
        attr.MethodName.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("SA-004")]
    public void CompletionAttribute_EmptyValues_StoresEmptyArray()
    {
        // Arrange & Act
        var attr = new CompletionAttribute(Array.Empty<string>());

        // Assert
        attr.Values.Should().NotBeNull();
        attr.Values.Should().BeEmpty();
    }

    #endregion

    #region SA-005: Attribute target validation

    [TestMethod]
    [TestCategory("SA-005")]
    public void CompletionAttribute_CanApplyToProperty()
    {
        // Check attribute usage
        var usageAttr = typeof(CompletionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        usageAttr.Should().HaveCount(1);
        var usage = (AttributeUsageAttribute)usageAttr[0];
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Property);
    }

    [TestMethod]
    [TestCategory("SA-005")]
    public void FilePathCompletionAttribute_CanApplyToProperty()
    {
        // Check attribute usage
        var usageAttr = typeof(FilePathCompletionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        usageAttr.Should().HaveCount(1);
        var usage = (AttributeUsageAttribute)usageAttr[0];
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Property);
    }

    [TestMethod]
    [TestCategory("SA-005")]
    public void DirectoryPathCompletionAttribute_CanApplyToProperty()
    {
        // Check attribute usage
        var usageAttr = typeof(DirectoryPathCompletionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        usageAttr.Should().HaveCount(1);
        var usage = (AttributeUsageAttribute)usageAttr[0];
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Property);
    }

    #endregion

    #region Attribute on actual properties

    [TestMethod]
    public void Attribute_OnProperty_CanBeRetrieved()
    {
        // Arrange
        var property = typeof(TestCommand).GetProperty(nameof(TestCommand.Format));

        // Act
        var attrs = property.GetCustomAttributes(typeof(CompletionAttribute), false);

        // Assert
        attrs.Should().HaveCount(1);
        var attr = (CompletionAttribute)attrs[0];
        attr.MethodName.Should().Be("GetFormats");
    }

    [TestMethod]
    public void FilePathAttribute_OnProperty_CanBeRetrieved()
    {
        // Arrange
        var property = typeof(TestCommand).GetProperty(nameof(TestCommand.FilePath));

        // Act
        var attrs = property.GetCustomAttributes(typeof(FilePathCompletionAttribute), false);

        // Assert
        attrs.Should().HaveCount(1);
        var attr = (FilePathCompletionAttribute)attrs[0];
        attr.Pattern.Should().Be("*.cs");
    }

    [TestMethod]
    public void DirectoryPathAttribute_OnProperty_CanBeRetrieved()
    {
        // Arrange
        var property = typeof(TestCommand).GetProperty(nameof(TestCommand.Directory));

        // Act
        var attrs = property.GetCustomAttributes(typeof(DirectoryPathCompletionAttribute), false);

        // Assert
        attrs.Should().HaveCount(1);
        var attr = (DirectoryPathCompletionAttribute)attrs[0];
        attr.BasePath.Should().Be(@"C:\Projects");
    }

    [TestMethod]
    public void StaticValuesAttribute_OnProperty_CanBeRetrieved()
    {
        // Arrange
        var property = typeof(TestCommand).GetProperty(nameof(TestCommand.Verbosity));

        // Act
        var attrs = property.GetCustomAttributes(typeof(CompletionAttribute), false);

        // Assert
        attrs.Should().HaveCount(1);
        var attr = (CompletionAttribute)attrs[0];
        attr.Values.Should().Contain(new[] { "quiet", "minimal", "normal", "detailed", "diagnostic" });
    }

    #endregion

    #region Test Helpers

    public class TestCommand
    {
        [Completion("GetFormats")]
        public string Format { get; set; }

        [FilePathCompletion(Pattern = "*.cs")]
        public string FilePath { get; set; }

        [DirectoryPathCompletion(BasePath = @"C:\Projects")]
        public string Directory { get; set; }

        [Completion(new[] { "quiet", "minimal", "normal", "detailed", "diagnostic" })]
        public string Verbosity { get; set; }

        public string[] GetFormats() => new[] { "json", "xml" };
    }

    #endregion
}
