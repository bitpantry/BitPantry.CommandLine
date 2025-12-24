using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for the GroupInfo class.
    /// T002: Test FullPath computation, parent/child setup
    /// </summary>
    [TestClass]
    public class GroupInfoTests
    {
        [TestMethod]
        public void GroupInfo_Name_ReturnsSetValue()
        {
            // Arrange & Act
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));

            // Assert
            groupInfo.Name.Should().Be("math");
        }

        [TestMethod]
        public void GroupInfo_Description_ReturnsSetValue()
        {
            // Arrange & Act
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));

            // Assert
            groupInfo.Description.Should().Be("Math operations");
        }

        [TestMethod]
        public void GroupInfo_Parent_IsNullForTopLevelGroup()
        {
            // Arrange & Act
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));

            // Assert
            groupInfo.Parent.Should().BeNull();
        }

        [TestMethod]
        public void GroupInfo_MarkerType_ReturnsSetValue()
        {
            // Arrange & Act
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));

            // Assert
            groupInfo.MarkerType.Should().Be(typeof(TestMathGroup));
        }

        [TestMethod]
        public void GroupInfo_FullPath_ForTopLevelGroup_ReturnsName()
        {
            // Arrange
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));

            // Act
            var fullPath = groupInfo.FullPath;

            // Assert
            fullPath.Should().Be("math");
        }

        [TestMethod]
        public void GroupInfo_FullPath_ForNestedGroup_ReturnsSpaceSeparatedPath()
        {
            // Arrange
            var parentGroup = new GroupInfo("files", "File operations", null, typeof(TestFilesGroup));
            var childGroup = new GroupInfo("io", "IO operations", parentGroup, typeof(TestIoGroup));

            // Act
            var fullPath = childGroup.FullPath;

            // Assert
            fullPath.Should().Be("files io");
        }

        [TestMethod]
        public void GroupInfo_FullPath_ForDeeplyNestedGroup_ReturnsFullHierarchy()
        {
            // Arrange
            var level1 = new GroupInfo("level1", "Level 1", null, typeof(TestLevel1Group));
            var level2 = new GroupInfo("level2", "Level 2", level1, typeof(TestLevel2Group));
            var level3 = new GroupInfo("level3", "Level 3", level2, typeof(TestLevel3Group));

            // Act
            var fullPath = level3.FullPath;

            // Assert
            fullPath.Should().Be("level1 level2 level3");
        }

        [TestMethod]
        public void GroupInfo_Depth_ForTopLevelGroup_ReturnsZero()
        {
            // Arrange
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));

            // Act
            var depth = groupInfo.Depth;

            // Assert
            depth.Should().Be(0);
        }

        [TestMethod]
        public void GroupInfo_Depth_ForNestedGroup_ReturnsCorrectDepth()
        {
            // Arrange
            var level1 = new GroupInfo("level1", "Level 1", null, typeof(TestLevel1Group));
            var level2 = new GroupInfo("level2", "Level 2", level1, typeof(TestLevel2Group));
            var level3 = new GroupInfo("level3", "Level 3", level2, typeof(TestLevel3Group));

            // Assert
            level1.Depth.Should().Be(0);
            level2.Depth.Should().Be(1);
            level3.Depth.Should().Be(2);
        }

        [TestMethod]
        public void GroupInfo_ChildGroups_InitializesEmpty()
        {
            // Arrange & Act
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));

            // Assert
            groupInfo.ChildGroups.Should().NotBeNull();
            groupInfo.ChildGroups.Should().BeEmpty();
        }

        [TestMethod]
        public void GroupInfo_Commands_InitializesEmpty()
        {
            // Arrange & Act
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));

            // Assert
            groupInfo.Commands.Should().NotBeNull();
            groupInfo.Commands.Should().BeEmpty();
        }

        [TestMethod]
        public void GroupInfo_AddChildGroup_AddsToCollection()
        {
            // Arrange
            var parentGroup = new GroupInfo("files", "File operations", null, typeof(TestFilesGroup));
            var childGroup = new GroupInfo("io", "IO operations", parentGroup, typeof(TestIoGroup));

            // Act
            parentGroup.AddChildGroup(childGroup);

            // Assert
            parentGroup.ChildGroups.Should().Contain(childGroup);
        }

        [TestMethod]
        public void GroupInfo_AddCommand_AddsToCollection()
        {
            // Arrange
            var groupInfo = new GroupInfo("math", "Math operations", null, typeof(TestMathGroup));
            var commandInfo = Processing.Description.CommandReflection.Describe<TestAddCommand>();

            // Act
            groupInfo.AddCommand(commandInfo);

            // Assert
            groupInfo.Commands.Should().Contain(commandInfo);
        }

        // Test command for AddCommand test
        [API.Command(Name = "add")]
        private class TestAddCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        // Test helper classes
        [Group]
        private class TestMathGroup { }

        [Group]
        private class TestFilesGroup { }

        [Group]
        private class TestIoGroup { }

        [Group]
        private class TestLevel1Group { }

        [Group]
        private class TestLevel2Group { }

        [Group]
        private class TestLevel3Group { }
    }
}
