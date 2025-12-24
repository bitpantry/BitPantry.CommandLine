using BitPantry.CommandLine.API;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for the GroupAttribute class.
    /// T001: Test attribute properties, defaults, validation
    /// </summary>
    [TestClass]
    public class GroupAttributeTests
    {
        [TestMethod]
        public void GroupAttribute_DefaultName_IsNull()
        {
            // Arrange & Act
            var attr = new GroupAttribute();

            // Assert
            attr.Name.Should().BeNull();
        }

        [TestMethod]
        public void GroupAttribute_DefaultDescription_IsNull()
        {
            // Arrange & Act
            var attr = new GroupAttribute();

            // Assert
            attr.Description.Should().BeNull();
        }

        [TestMethod]
        public void GroupAttribute_CanSetName()
        {
            // Arrange & Act
            var attr = new GroupAttribute { Name = "custom-name" };

            // Assert
            attr.Name.Should().Be("custom-name");
        }

        [TestMethod]
        public void GroupAttribute_CanSetDescription()
        {
            // Arrange & Act
            var attr = new GroupAttribute { Description = "A group description" };

            // Assert
            attr.Description.Should().Be("A group description");
        }

        [TestMethod]
        public void GroupAttribute_CanBeAppliedToClass()
        {
            // Arrange
            var type = typeof(TestGroupClass);

            // Act
            var attrs = type.GetCustomAttributes(typeof(GroupAttribute), false);

            // Assert
            attrs.Should().HaveCount(1);
            attrs[0].Should().BeOfType<GroupAttribute>();
        }

        [TestMethod]
        public void GroupAttribute_CannotBeAppliedMultipleTimes()
        {
            // Arrange
            var attrUsage = typeof(GroupAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false);

            // Assert
            attrUsage.Should().HaveCount(1);
            var usage = (AttributeUsageAttribute)attrUsage[0];
            usage.AllowMultiple.Should().BeFalse();
        }

        [TestMethod]
        public void GroupAttribute_TargetsClassesOnly()
        {
            // Arrange
            var attrUsage = typeof(GroupAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false);

            // Assert
            attrUsage.Should().HaveCount(1);
            var usage = (AttributeUsageAttribute)attrUsage[0];
            usage.ValidOn.Should().Be(AttributeTargets.Class);
        }

        [TestMethod]
        public void GroupAttribute_WithNameOverride_UsesProvidedName()
        {
            // Arrange
            var type = typeof(TestGroupWithNameOverride);

            // Act
            var attr = (GroupAttribute)type.GetCustomAttributes(typeof(GroupAttribute), false)[0];

            // Assert
            attr.Name.Should().Be("custom");
        }

        // Test helper classes
        [Group]
        private class TestGroupClass { }

        [Group(Name = "custom")]
        private class TestGroupWithNameOverride { }
    }
}
