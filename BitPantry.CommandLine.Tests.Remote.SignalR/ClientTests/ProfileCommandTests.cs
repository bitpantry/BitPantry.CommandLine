using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using DescriptionAttribute = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class ProfileCommandTests
    {
        #region ProfileListCommand Tests

        [TestMethod]
        public void ProfileListCommand_HasCorrectGroupAndName()
        {
            // Arrange & Act
            var attr = typeof(ProfileListCommand)
                .GetCustomAttribute<CommandAttribute>();

            // Assert
            attr.Should().NotBeNull();
            attr!.Name.Should().Be("list");
            attr.Group.Should().Be(typeof(ServerGroup.ProfileGroup));
        }

        [TestMethod]
        public void ProfileListCommand_HasDescription()
        {
            // Arrange & Act
            var attr = typeof(ProfileListCommand)
                .GetCustomAttribute<DescriptionAttribute>();

            // Assert
            attr.Should().NotBeNull();
            attr!.Description.Should().Contain("profile");
        }

        #endregion

        #region ProfileAddCommand Tests

        [TestMethod]
        public void ProfileAddCommand_HasCorrectGroupAndName()
        {
            // Arrange & Act
            var attr = typeof(ProfileAddCommand)
                .GetCustomAttribute<CommandAttribute>();

            // Assert
            attr.Should().NotBeNull();
            attr!.Name.Should().Be("add");
            attr.Group.Should().Be(typeof(ServerGroup.ProfileGroup));
        }

        [TestMethod]
        public void ProfileAddCommand_HasNameArgument()
        {
            // Arrange & Act
            var prop = typeof(ProfileAddCommand).GetProperty("Name");
            var argAttr = prop?.GetCustomAttribute<ArgumentAttribute>();

            // Assert
            prop.Should().NotBeNull();
            argAttr.Should().NotBeNull();
            argAttr!.Position.Should().Be(0);
        }

        [TestMethod]
        public void ProfileAddCommand_HasUriArgument()
        {
            // Arrange & Act
            var prop = typeof(ProfileAddCommand).GetProperty("Uri");
            var argAttr = prop?.GetCustomAttribute<ArgumentAttribute>();

            // Assert
            prop.Should().NotBeNull();
            argAttr.Should().NotBeNull();
        }

        [TestMethod]
        public void ProfileAddCommand_HasApiKeyArgument()
        {
            // Arrange & Act
            var prop = typeof(ProfileAddCommand).GetProperty("ApiKey");
            var argAttr = prop?.GetCustomAttribute<ArgumentAttribute>();
            var aliasAttr = prop?.GetCustomAttribute<AliasAttribute>();

            // Assert
            prop.Should().NotBeNull();
            argAttr.Should().NotBeNull();
            aliasAttr.Should().NotBeNull();
            aliasAttr!.Alias.Should().Be('k');
        }

        [TestMethod]
        public void ProfileAddCommand_HasDefaultOption()
        {
            // Arrange & Act
            var prop = typeof(ProfileAddCommand).GetProperty("Default");

            // Assert
            prop.Should().NotBeNull();
            prop!.PropertyType.Should().Be(typeof(Option));
        }

        [TestMethod]
        public void ProfileAddCommand_HasForceOption()
        {
            // Arrange & Act
            var prop = typeof(ProfileAddCommand).GetProperty("Force");
            var aliasAttr = prop?.GetCustomAttribute<AliasAttribute>();

            // Assert
            prop.Should().NotBeNull();
            prop!.PropertyType.Should().Be(typeof(Option));
            aliasAttr.Should().NotBeNull();
            aliasAttr!.Alias.Should().Be('f');
        }

        #endregion

        #region ProfileRemoveCommand Tests

        [TestMethod]
        public void ProfileRemoveCommand_HasCorrectGroupAndName()
        {
            // Arrange & Act
            var attr = typeof(ProfileRemoveCommand)
                .GetCustomAttribute<CommandAttribute>();

            // Assert
            attr.Should().NotBeNull();
            attr!.Name.Should().Be("remove");
            attr.Group.Should().Be(typeof(ServerGroup.ProfileGroup));
        }

        [TestMethod]
        public void ProfileRemoveCommand_HasNameArgument()
        {
            // Arrange & Act
            var prop = typeof(ProfileRemoveCommand).GetProperty("Name");
            var argAttr = prop?.GetCustomAttribute<ArgumentAttribute>();

            // Assert
            prop.Should().NotBeNull();
            argAttr.Should().NotBeNull();
            argAttr!.Position.Should().Be(0);
        }

        #endregion

        #region ProfileShowCommand Tests

        [TestMethod]
        public void ProfileShowCommand_HasCorrectGroupAndName()
        {
            // Arrange & Act
            var attr = typeof(ProfileShowCommand)
                .GetCustomAttribute<CommandAttribute>();

            // Assert
            attr.Should().NotBeNull();
            attr!.Name.Should().Be("show");
            attr.Group.Should().Be(typeof(ServerGroup.ProfileGroup));
        }

        [TestMethod]
        public void ProfileShowCommand_HasNameArgument()
        {
            // Arrange & Act
            var prop = typeof(ProfileShowCommand).GetProperty("Name");
            var argAttr = prop?.GetCustomAttribute<ArgumentAttribute>();

            // Assert
            prop.Should().NotBeNull();
            argAttr.Should().NotBeNull();
            argAttr!.Position.Should().Be(0);
        }

        #endregion

        #region ProfileSetDefaultCommand Tests

        [TestMethod]
        public void ProfileSetDefaultCommand_HasCorrectGroupAndName()
        {
            // Arrange & Act
            var attr = typeof(ProfileSetDefaultCommand)
                .GetCustomAttribute<CommandAttribute>();

            // Assert
            attr.Should().NotBeNull();
            attr!.Name.Should().Be("set-default");
            attr.Group.Should().Be(typeof(ServerGroup.ProfileGroup));
        }

        [TestMethod]
        public void ProfileSetDefaultCommand_HasNameArgument()
        {
            // Arrange & Act
            var prop = typeof(ProfileSetDefaultCommand).GetProperty("Name");
            var argAttr = prop?.GetCustomAttribute<ArgumentAttribute>();

            // Assert
            prop.Should().NotBeNull();
            argAttr.Should().NotBeNull();
            argAttr!.Position.Should().Be(0);
        }

        #endregion

        #region ProfileSetKeyCommand Tests

        [TestMethod]
        public void ProfileSetKeyCommand_HasCorrectGroupAndName()
        {
            // Arrange & Act
            var attr = typeof(ProfileSetKeyCommand)
                .GetCustomAttribute<CommandAttribute>();

            // Assert
            attr.Should().NotBeNull();
            attr!.Name.Should().Be("set-key");
            attr.Group.Should().Be(typeof(ServerGroup.ProfileGroup));
        }

        [TestMethod]
        public void ProfileSetKeyCommand_HasNameArgument()
        {
            // Arrange & Act
            var prop = typeof(ProfileSetKeyCommand).GetProperty("Name");
            var argAttr = prop?.GetCustomAttribute<ArgumentAttribute>();

            // Assert
            prop.Should().NotBeNull();
            argAttr.Should().NotBeNull();
            argAttr!.Position.Should().Be(0);
        }

        [TestMethod]
        public void ProfileSetKeyCommand_HasApiKeyArgument()
        {
            // Arrange & Act
            var prop = typeof(ProfileSetKeyCommand).GetProperty("ApiKey");
            var argAttr = prop?.GetCustomAttribute<ArgumentAttribute>();

            // Assert
            prop.Should().NotBeNull();
            argAttr.Should().NotBeNull();
        }

        #endregion

        #region ServerGroup Structure Tests

        [TestMethod]
        public void ServerGroup_HasProfileGroup()
        {
            // Arrange & Act
            var profileGroupType = typeof(ServerGroup.ProfileGroup);
            var groupAttr = profileGroupType.GetCustomAttribute<GroupAttribute>();

            // Assert
            groupAttr.Should().NotBeNull();
            groupAttr!.Name.Should().Be("profile");
        }

        [TestMethod]
        public void ServerGroup_HasCorrectName()
        {
            // Arrange & Act
            var groupAttr = typeof(ServerGroup).GetCustomAttribute<GroupAttribute>();

            // Assert
            groupAttr.Should().NotBeNull();
            groupAttr!.Name.Should().Be("server");
        }

        #endregion
    }
}
