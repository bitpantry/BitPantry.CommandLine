using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using Moq;
using FluentAssertions;
using BitPantry.CommandLine.Remote.SignalR;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class RpcMessageRegistryTests
    {
        [TestMethod]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();

            // Act
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);

            // Assert
            registry.Should().NotBeNull();
        }

        [TestMethod]
        public void Register_ShouldCreateAndAddRpcMessageContext()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);

            // Act
            var context = registry.Register();

            // Assert
            context.Should().NotBeNull();
            context.Scope.Should().Be("testScope");
        }

        [TestMethod]
        public void Register_ShouldCreateUniqueCorrelationIds()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);

            // Act
            var context1 = registry.Register();
            var context2 = registry.Register();

            // Assert
            context1.CorrelationId.Should().NotBe(context2.CorrelationId);
        }

        [TestMethod]
        public async Task SetResponse_ShouldSetResponseAndRemoveContext()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);
            var context = registry.Register();
            var messageData = new Dictionary<string, string> { { "key", "testData" } };
            var message = new MessageBase(messageData) { CorrelationId = context.CorrelationId };

            // Act
            registry.SetResponse(message);

            // Assert
            var result = await context.WaitForCompletion<MessageBase>();
            result.Should().Be(message);
        }

        [TestMethod]
        public void SetResponse_ShouldDoNothingIfCorrelationIdDoesNotExist()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);
            var messageData = new Dictionary<string, string> { { "key", "testData" } };
            var message = new MessageBase(messageData) { CorrelationId = "nonexistent-id" };

            // Act
            Action act = () => registry.SetResponse(message);

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task AbortWithError_ShouldSetExceptionAndRemoveContext()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);
            var context = registry.Register();
            var exception = new Exception("testException");

            // Act
            registry.AbortWithError(context.CorrelationId, exception);

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<Exception>(async () => await context.WaitForCompletion<MessageBase>());
            ex.Message.Should().Be("testException");
        }

        [TestMethod]
        public void AbortWithError_ShouldDoNothingIfCorrelationIdDoesNotExist()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);
            var exception = new Exception("testException");

            // Act
            Action act = () => registry.AbortWithError("nonexistent-id", exception);

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task AbortWithRemoteError_ShouldSetRemoteMessagingExceptionAndRemoveContext()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);
            var context = registry.Register();
            string errorMessage = "testError";

            // Act
            registry.AbortWithRemoteError(context.CorrelationId, errorMessage);

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<RemoteMessagingException>(async () => await context.WaitForCompletion<MessageBase>());
            ex.Message.Should().Be(errorMessage);
            ex.CorrelationId.Should().Be(context.CorrelationId);
        }

        [TestMethod]
        public void AbortWithRemoteError_ShouldDoNothingIfCorrelationIdDoesNotExist()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);
            string errorMessage = "testError";

            // Act
            Action act = () => registry.AbortWithRemoteError("nonexistent-id", errorMessage);

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task AbortScopeWithRemoteError_ShouldSetRemoteMessagingExceptionAndRemoveAllContextsInScope()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);
            var context1 = registry.Register();
            var context2 = registry.Register();
            string errorMessage = "testError";

            // Act
            registry.AbortScopeWithRemoteError(errorMessage);

            // Assert
            var ex1 = await Assert.ThrowsExceptionAsync<RemoteMessagingException>(async () => await context1.WaitForCompletion<MessageBase>());
            var ex2 = await Assert.ThrowsExceptionAsync<RemoteMessagingException>(async () => await context2.WaitForCompletion<MessageBase>());
            ex1.Message.Should().Be(errorMessage);
            ex1.CorrelationId.Should().Be(context1.CorrelationId);
            ex2.Message.Should().Be(errorMessage);
            ex2.CorrelationId.Should().Be(context2.CorrelationId);
        }

        [TestMethod]
        public void AbortScopeWithRemoteError_ShouldDoNothingIfNoContextsInScope()
        {
            // Arrange
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var registry = new RpcMessageRegistry(rpcScopeMock.Object);
            string errorMessage = "testError";

            // Act
            Action act = () => registry.AbortScopeWithRemoteError(errorMessage);

            // Assert
            act.Should().NotThrow();
        }
    }
}

