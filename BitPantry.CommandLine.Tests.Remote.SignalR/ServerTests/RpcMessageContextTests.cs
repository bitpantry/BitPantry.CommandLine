using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using FluentAssertions;
using BitPantry.CommandLine.Remote.SignalR;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class RpcMessageContextTests
    {
        [TestMethod]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            string scope = "testScope";

            // Act
            var context = new RpcMessageContext(scope);

            // Assert
            context.Scope.Should().Be(scope);
            context.CorrelationId.Should().NotBeNull();
        }

        [TestMethod]
        public async Task WaitForCompletion_ShouldReturnCorrectData()
        {
            // Arrange
            var context = new RpcMessageContext("testScope");
            var messageData = new Dictionary<string, string> { { "key", "testData" } };
            var message = new MessageBase(messageData);
            context.SetResponse(message);

            // Act
            var result = await context.WaitForCompletion<MessageBase>();

            // Assert
            result.Data.Should().BeEquivalentTo(message.Data);
        }

        [TestMethod]
        public async Task SetResponse_ShouldSetTaskResult()
        {
            // Arrange
            var context = new RpcMessageContext("testScope");
            var messageData = new Dictionary<string, string> { { "key", "testData" } };
            var message = new MessageBase(messageData);

            // Act
            context.SetResponse(message);
            var result = await context.WaitForCompletion<MessageBase>();

            // Assert
            result.Should().Be(message);
        }

        [TestMethod]
        public async Task AbortWithError_ShouldSetException()
        {
            // Arrange
            var context = new RpcMessageContext("testScope");
            var exception = new Exception("testException");

            // Act
            context.AbortWithError(exception);

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<Exception>(async () => await context.WaitForCompletion<MessageBase>());
            ex.Message.Should().Be("testException");
        }

        [TestMethod]
        public async Task AbortWithRemoteError_ShouldSetRemoteMessagingException()
        {
            // Arrange
            var context = new RpcMessageContext("testScope");
            string errorMessage = "testError";

            // Act
            context.AbortWithRemoteError(errorMessage);

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<RemoteMessagingException>(async () => await context.WaitForCompletion<MessageBase>());
            ex.Message.Should().Be(errorMessage);
            ex.CorrelationId.Should().Be(context.CorrelationId);
        }

        [TestMethod]
        public void SetResponse_ShouldHandleNullMessage()
        {
            // Arrange
            var context = new RpcMessageContext("testScope");

            // Act & Assert
            Action act = () => context.SetResponse(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task SetResponse_ShouldHandleDoubleCompletion()
        {
            // Arrange
            var context = new RpcMessageContext("testScope");
            var messageData = new Dictionary<string, string> { { "key", "testData" } };
            var message = new MessageBase(messageData);

            // Act
            context.SetResponse(message);
            Action act = () => context.SetResponse(message); // Second call should throw

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("Response has already been set.");
            var result = await context.WaitForCompletion<MessageBase>();
            result.Should().Be(message);
        }

        [TestMethod]
        public async Task WaitForCompletion_ShouldHandleTimeout()
        {
            // Arrange
            var context = new RpcMessageContext("testScope");

            // Act & Assert
            var task = context.WaitForCompletion<MessageBase>();
            var completedTask = await Task.WhenAny(task, Task.Delay(1000)); // 1 second timeout
            completedTask.Should().NotBe(task); // Ensure the task did not complete
        }

        [TestMethod]
        public async Task ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var context = new RpcMessageContext("testScope");
            var messageData = new Dictionary<string, string> { { "key", "testData" } };
            var message = new MessageBase(messageData);
            var exception = new Exception("testException");

            // Act
            var tasks = new[]
            {
                Task.Run(() =>
                {
                    try
                    {
                        context.SetResponse(message);
                    }
                    catch (InvalidOperationException) { }
                }),
                Task.Run(() =>
                {
                    try
                    {
                        context.AbortWithError(exception);
                    }
                    catch (InvalidOperationException) { }
                }),
                Task.Run(() =>
                {
                    try
                    {
                        context.AbortWithRemoteError("testError");
                    }
                    catch (InvalidOperationException) { }
                })
            };

            await Task.WhenAll(tasks);

            // Assert
            var resultTask = context.WaitForCompletion<MessageBase>();
            var completedTask = await Task.WhenAny(resultTask, Task.Delay(1000)); // 1 second timeout
            completedTask.Should().Be(resultTask); // Ensure the task completed

            // Verify that subsequent calls throw InvalidOperationException
            Action act1 = () => context.SetResponse(message);
            Action act2 = () => context.AbortWithError(exception);
            Action act3 = () => context.AbortWithRemoteError("testError");

            act1.Should().Throw<InvalidOperationException>();
            act2.Should().Throw<InvalidOperationException>();
            act3.Should().Throw<InvalidOperationException>();
        }
    }
}
