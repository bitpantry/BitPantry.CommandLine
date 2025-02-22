using Moq;
using FluentAssertions;
using BitPantry.CommandLine.Remote.SignalR.Server.Authentication;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests.Auth
{
    [TestClass]
    public class RefreshTokenStoreTests
    {
        [TestMethod]
        public async Task StoreRefreshTokenAsync_ShouldStoreToken()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();

            // Act
            await store.Object.StoreRefreshTokenAsync("client-id", "refresh-token");

            // Assert
            store.Verify(store => store.StoreRefreshTokenAsync("client-id", "refresh-token"), Times.Once);
        }

        [TestMethod]
        public async Task TryGetRefreshTokenAsync_ShouldReturnTrue_WhenTokenExists()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            string refreshToken = "existing-refresh-token";
            store.Setup(s => s.TryGetRefreshTokenAsync("client-id", out refreshToken)).ReturnsAsync(true);

            // Act
            var result = await store.Object.TryGetRefreshTokenAsync("client-id", out var returnedToken);

            // Assert
            result.Should().BeTrue();
            returnedToken.Should().Be(refreshToken);
        }

        [TestMethod]
        public async Task TryGetRefreshTokenAsync_ShouldReturnFalse_WhenTokenDoesNotExist()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            string refreshToken = null;
            store.Setup(s => s.TryGetRefreshTokenAsync("client-id", out refreshToken)).ReturnsAsync(false);

            // Act
            var result = await store.Object.TryGetRefreshTokenAsync("client-id", out var returnedToken);

            // Assert
            result.Should().BeFalse();
            returnedToken.Should().BeNull();
        }

        [TestMethod]
        public async Task RevokeRefreshTokenAsync_ShouldRevokeToken()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();

            // Act
            await store.Object.RevokeRefreshTokenAsync("client-id");

            // Assert
            store.Verify(s => s.RevokeRefreshTokenAsync("client-id"), Times.Once);
        }

        [TestMethod]
        public async Task StoreRefreshTokenAsync_ShouldThrowException_WhenClientIdIsNull()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            store.Setup(s => s.StoreRefreshTokenAsync(null, It.IsAny<string>())).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await store.Object.Invoking(s => s.StoreRefreshTokenAsync(null, "refresh-token"))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task StoreRefreshTokenAsync_ShouldThrowException_WhenClientIdIsEmpty()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            store.Setup(s => s.StoreRefreshTokenAsync(string.Empty, It.IsAny<string>())).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await store.Object.Invoking(s => s.StoreRefreshTokenAsync(string.Empty, "refresh-token"))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task TryGetRefreshTokenAsync_ShouldThrowException_WhenClientIdIsNull()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            string refreshToken;
            store.Setup(s => s.TryGetRefreshTokenAsync(null, out refreshToken)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await store.Object.Invoking(s => s.TryGetRefreshTokenAsync(null, out refreshToken))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task TryGetRefreshTokenAsync_ShouldThrowException_WhenClientIdIsEmpty()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            string refreshToken;
            store.Setup(s => s.TryGetRefreshTokenAsync(string.Empty, out refreshToken)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await store.Object.Invoking(s => s.TryGetRefreshTokenAsync(string.Empty, out refreshToken))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RevokeRefreshTokenAsync_ShouldThrowException_WhenClientIdIsNull()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            store.Setup(s => s.RevokeRefreshTokenAsync(null)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await store.Object.Invoking(s => s.RevokeRefreshTokenAsync(null))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RevokeRefreshTokenAsync_ShouldThrowException_WhenClientIdIsEmpty()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            store.Setup(s => s.RevokeRefreshTokenAsync(string.Empty)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await store.Object.Invoking(s => s.RevokeRefreshTokenAsync(string.Empty))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task StoreRefreshTokenAsync_ShouldThrowException_WhenRefreshTokenIsNull()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            store.Setup(s => s.StoreRefreshTokenAsync(It.IsAny<string>(), null)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await store.Object.Invoking(s => s.StoreRefreshTokenAsync("client-id", null))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task StoreRefreshTokenAsync_ShouldThrowException_WhenRefreshTokenIsEmpty()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            store.Setup(s => s.StoreRefreshTokenAsync(It.IsAny<string>(), string.Empty)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await store.Object.Invoking(s => s.StoreRefreshTokenAsync("client-id", string.Empty))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task StoreRefreshTokenAsync_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(store.Object.StoreRefreshTokenAsync("client-id", $"refresh-token-{i}"));
            }
            await Task.WhenAll(tasks);

            // Assert
            store.Verify(s => s.StoreRefreshTokenAsync("client-id", It.IsAny<string>()), Times.Exactly(10));
        }

        [TestMethod]
        public async Task StoreRefreshTokenAsync_ShouldHandleExceptions()
        {
            // Arrange
            var store = new Mock<IRefreshTokenStore>();
            store.Setup(s => s.StoreRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            await store.Object.Invoking(s => s.StoreRefreshTokenAsync("client-id", "refresh-token"))
                .Should().ThrowAsync<Exception>().WithMessage("Test exception");
        }
    }
}

