using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class ProfileNameProviderTests
    {
        private Mock<IProfileManager> _profileManagerMock = null!;
        private ProfileNameProvider _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _profileManagerMock = new Mock<IProfileManager>();
            _provider = new ProfileNameProvider(_profileManagerMock.Object);
        }

        [TestMethod]
        public void Priority_ShouldBe80()
        {
            // Assert
            _provider.Priority.Should().Be(80);
        }

        [TestMethod]
        public void CanHandle_ShouldReturnFalse_WhenNotArgumentValue()
        {
            // Arrange
            var context = new CompletionContext
            {
                ElementType = CompletionElementType.Command
            };

            // Act
            var result = _provider.CanHandle(context);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void CanHandle_ShouldReturnFalse_WhenNoCompletionAttribute()
        {
            // Arrange
            var context = new CompletionContext
            {
                ElementType = CompletionElementType.ArgumentValue,
                CompletionAttribute = null
            };

            // Act
            var result = _provider.CanHandle(context);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void CanHandle_ShouldReturnTrue_WhenProviderTypeMatches()
        {
            // Arrange
            var context = new CompletionContext
            {
                ElementType = CompletionElementType.ArgumentValue,
                CompletionAttribute = new BitPantry.CommandLine.AutoComplete.Attributes.CompletionAttribute(typeof(ProfileNameProvider))
            };

            // Act
            var result = _provider.CanHandle(context);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task GetCompletionsAsync_ShouldReturnEmpty_WhenNoProfiles()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync())
                .ReturnsAsync(new List<ServerProfile>());

            var context = new CompletionContext
            {
                ElementType = CompletionElementType.ArgumentValue,
                CompletionAttribute = new BitPantry.CommandLine.AutoComplete.Attributes.CompletionAttribute(typeof(ProfileNameProvider))
            };

            // Act
            var result = await _provider.GetCompletionsAsync(context);

            // Assert
            result.Items.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetCompletionsAsync_ShouldReturnAllProfiles()
        {
            // Arrange
            var profiles = new List<ServerProfile>
            {
                new ServerProfile { Name = "production", Uri = "https://prod.example.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.example.com" }
            };
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync())
                .ReturnsAsync(profiles);
            _profileManagerMock.Setup(m => m.GetDefaultProfileAsync())
                .ReturnsAsync((string?)null);

            var context = new CompletionContext
            {
                ElementType = CompletionElementType.ArgumentValue,
                CurrentWord = "",
                CompletionAttribute = new BitPantry.CommandLine.AutoComplete.Attributes.CompletionAttribute(typeof(ProfileNameProvider))
            };

            // Act
            var result = await _provider.GetCompletionsAsync(context);

            // Assert
            result.Items.Should().HaveCount(2);
            result.Items.Select(i => i.DisplayText).Should().Contain("production", "staging");
        }

        [TestMethod]
        public async Task GetCompletionsAsync_ShouldFilterByPrefix()
        {
            // Arrange
            var profiles = new List<ServerProfile>
            {
                new ServerProfile { Name = "production", Uri = "https://prod.example.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.example.com" },
                new ServerProfile { Name = "development", Uri = "https://dev.example.com" }
            };
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync())
                .ReturnsAsync(profiles);
            _profileManagerMock.Setup(m => m.GetDefaultProfileAsync())
                .ReturnsAsync((string?)null);

            var context = new CompletionContext
            {
                ElementType = CompletionElementType.ArgumentValue,
                CurrentWord = "prod",
                CompletionAttribute = new BitPantry.CommandLine.AutoComplete.Attributes.CompletionAttribute(typeof(ProfileNameProvider))
            };

            // Act
            var result = await _provider.GetCompletionsAsync(context);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].DisplayText.Should().Be("production");
        }

        [TestMethod]
        public async Task GetCompletionsAsync_ShouldMarkDefaultProfileFirst()
        {
            // Arrange
            var profiles = new List<ServerProfile>
            {
                new ServerProfile { Name = "alpha", Uri = "https://alpha.example.com" },
                new ServerProfile { Name = "beta", Uri = "https://beta.example.com" }
            };
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync())
                .ReturnsAsync(profiles);
            _profileManagerMock.Setup(m => m.GetDefaultProfileAsync())
                .ReturnsAsync("beta");

            var context = new CompletionContext
            {
                ElementType = CompletionElementType.ArgumentValue,
                CurrentWord = "",
                CompletionAttribute = new BitPantry.CommandLine.AutoComplete.Attributes.CompletionAttribute(typeof(ProfileNameProvider))
            };

            // Act
            var result = await _provider.GetCompletionsAsync(context);

            // Assert
            result.Items.Should().HaveCount(2);
            // Beta should be first due to lower sort priority
            result.Items[0].DisplayText.Should().Be("beta");
            result.Items[0].Description.Should().Contain("(default)");
        }
    }
}
