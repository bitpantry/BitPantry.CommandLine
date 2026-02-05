using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using FluentAssertions;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for ProfileNameProvider autocomplete handler.
    /// </summary>
    [TestClass]
    public class ProfileNameProviderTests
    {
        private Mock<IProfileManager> _profileManagerMock = null!;

        [TestInitialize]
        public void Setup()
        {
            _profileManagerMock = new Mock<IProfileManager>();
            _profileManagerMock.Setup(p => p.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_NullProfileManager_ThrowsArgumentNullException()
        {
            // Act
            var act = () => new ProfileNameProvider(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("profileManager");
        }

        #endregion

        #region GetOptionsAsync Tests (T060-T063)

        /// <summary>
        /// Implements: 009:T060 (AC-001)
        /// When no profiles exist, returns empty list.
        /// </summary>
        [TestMethod]
        public async Task GetOptions_NoProfiles_ReturnsEmpty()
        {
            // Arrange
            _profileManagerMock.Setup(p => p.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<ServerProfile>());
            var provider = new ProfileNameProvider(_profileManagerMock.Object);
            var context = CreateContext("");

            // Act
            var options = await provider.GetOptionsAsync(context);

            // Assert
            options.Should().NotBeNull("should return an empty list, not null");
            options.Should().BeEmpty("no profiles exist");
        }

        /// <summary>
        /// Implements: 009:T061 (AC-002)
        /// When multiple profiles exist, returns all profile names.
        /// </summary>
        [TestMethod]
        public async Task GetOptions_MultipleProfiles_ReturnsAll()
        {
            // Arrange
            var profiles = new[]
            {
                new ServerProfile { Name = "production", Uri = "https://prod.example.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.example.com" },
                new ServerProfile { Name = "development", Uri = "https://dev.example.com" }
            };
            _profileManagerMock.Setup(p => p.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);
            var provider = new ProfileNameProvider(_profileManagerMock.Object);
            var context = CreateContext("");

            // Act
            var options = await provider.GetOptionsAsync(context);

            // Assert
            options.Should().HaveCount(3, "should return all profile names");
            options.Select(o => o.Value).Should().BeEquivalentTo(
                new[] { "production", "staging", "development" },
                "all profile names should be returned");
        }

        /// <summary>
        /// Implements: 009:T062 (AC-003)
        /// When query prefix is provided, filters to matching profiles.
        /// </summary>
        [TestMethod]
        public async Task GetOptions_WithPrefix_FiltersResults()
        {
            // Arrange
            var profiles = new[]
            {
                new ServerProfile { Name = "production", Uri = "https://prod.example.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.example.com" },
                new ServerProfile { Name = "development", Uri = "https://dev.example.com" }
            };
            _profileManagerMock.Setup(p => p.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);
            var provider = new ProfileNameProvider(_profileManagerMock.Object);
            var context = CreateContext("prod");

            // Act
            var options = await provider.GetOptionsAsync(context);

            // Assert
            options.Should().HaveCount(1, "only profiles matching prefix should be returned");
            options.First().Value.Should().Be("production");
        }

        /// <summary>
        /// Implements: 009:T063 (AC-004)
        /// Prefix matching is case-insensitive.
        /// </summary>
        [TestMethod]
        public async Task GetOptions_CaseInsensitivePrefix_Matches()
        {
            // Arrange
            var profiles = new[]
            {
                new ServerProfile { Name = "Production", Uri = "https://prod.example.com" },
                new ServerProfile { Name = "Staging", Uri = "https://staging.example.com" }
            };
            _profileManagerMock.Setup(p => p.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);
            var provider = new ProfileNameProvider(_profileManagerMock.Object);
            var context = CreateContext("PROD"); // uppercase query

            // Act
            var options = await provider.GetOptionsAsync(context);

            // Assert
            options.Should().HaveCount(1, "case-insensitive matching should find Production");
            options.First().Value.Should().Be("Production");
        }

        #endregion

        #region Default Profile Indicator Tests

        /// <summary>
        /// Implements: 009:T064 (AC-005)
        /// Verifies default profile is marked with "(default)" indicator in format.
        /// </summary>
        [TestMethod]
        public async Task GetOptions_DefaultProfile_HasDefaultIndicator()
        {
            // Arrange
            var profiles = new[]
            {
                new ServerProfile { Name = "production", Uri = "https://prod.example.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.example.com" }
            };
            _profileManagerMock.Setup(p => p.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);
            _profileManagerMock.Setup(p => p.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("production");
            var provider = new ProfileNameProvider(_profileManagerMock.Object);
            var context = CreateContext("");

            // Act
            var options = await provider.GetOptionsAsync(context);

            // Assert
            var prodOption = options.First(o => o.Value == "production");
            prodOption.Format.Should().Contain("default", "default profile should have default indicator");
            
            var stagingOption = options.First(o => o.Value == "staging");
            stagingOption.Format.Should().BeNull("non-default profile should not have default indicator");
        }

        /// <summary>
        /// Verifies default name comparison is case-insensitive.
        /// </summary>
        [TestMethod]
        public async Task GetOptions_DefaultProfileCaseInsensitive_HasDefaultIndicator()
        {
            // Arrange
            var profiles = new[]
            {
                new ServerProfile { Name = "Production", Uri = "https://prod.example.com" }
            };
            _profileManagerMock.Setup(p => p.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);
            _profileManagerMock.Setup(p => p.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("PRODUCTION"); // Different case
            var provider = new ProfileNameProvider(_profileManagerMock.Object);
            var context = CreateContext("");

            // Act
            var options = await provider.GetOptionsAsync(context);

            // Assert
            options.First().Format.Should().Contain("default", "case-insensitive default comparison should match");
        }

        #endregion

        #region Test Helpers

        /// <summary>
        /// Test command with profile argument for testing context creation.
        /// </summary>
        [Command]
        private class TestCommandWithProfile : CommandBase
        {
            [Argument]
            public string Profile { get; set; } = string.Empty;

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Creates a minimal AutoCompleteContext for testing.
        /// ProfileNameProvider only uses QueryString, but we need to satisfy the required properties.
        /// </summary>
        private static AutoCompleteContext CreateContext(string queryString)
        {
            var commandInfo = CommandReflection.Describe<TestCommandWithProfile>();
            var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Profile");

            return new AutoCompleteContext
            {
                QueryString = queryString,
                FullInput = $"cmd --Profile {queryString}",
                CursorPosition = $"cmd --Profile {queryString}".Length,
                ArgumentInfo = argumentInfo,
                ProvidedValues = new Dictionary<ArgumentInfo, string>(),
                CommandInfo = commandInfo
            };
        }

        #endregion
    }
}
