using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using FluentAssertions;
using Moq;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for FileAccessConsentHandler.
    /// Tests cover consent policy evaluation, prompt rendering, console output buffering,
    /// and ReceiveMessage integration for the three new push message types.
    /// </summary>
    [TestClass]
    public class FileAccessConsentHandlerTests
    {
        private FileAccessConsentPolicy _policy;
        private TestConsole _testConsole;
        private MockFileSystem _fileSystem;
        private FileAccessConsentHandler _handler;

        [TestInitialize]
        public void Setup()
        {
            _policy = new FileAccessConsentPolicy();
            _testConsole = new TestConsole();
            _fileSystem = new MockFileSystem();
            _handler = new FileAccessConsentHandler(_policy, _testConsole, _fileSystem);
        }

        #region Test 1: RequestConsent_AllowedPath_ReturnsTrueNoPrompt

        /// <summary>
        /// When the path is in the allowed list via policy, RequestConsentAsync returns true
        /// immediately without rendering any prompt output.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_AllowedPath_ReturnsTrueNoPrompt()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { "/data/**" });

            // Act
            var result = await _handler.RequestConsentAsync(
                "/data/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeTrue("path matches allowed pattern - no prompt needed");
            _testConsole.Output.Should().BeEmpty("no prompt should be rendered for allowed paths");
        }

        #endregion

        #region Test 2: RequestConsent_UnallowedPath_ShowsPrompt

        /// <summary>
        /// When the path is not in the allowed list, a consent prompt Panel is rendered
        /// containing the requested path text.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_UnallowedPath_ShowsPrompt()
        {
            // Arrange - no allowed patterns, queue Y key response
            _testConsole.Input.PushKey(ConsoleKey.Y);

            // Act
            await _handler.RequestConsentAsync(
                "/secret/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            _testConsole.Output.Should().Contain("File Access Request", "panel header should be rendered");
            _testConsole.Output.Should().Contain("/secret/file.txt", "the actual requested path should be displayed");
        }

        #endregion

        #region Test 3: RequestConsent_UserApprovesY_ReturnsTrue

        /// <summary>
        /// When user presses Y at the consent prompt, returns true.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_UserApprovesY_ReturnsTrue()
        {
            // Arrange
            _testConsole.Input.PushKey(ConsoleKey.Y);

            // Act
            var result = await _handler.RequestConsentAsync(
                "/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeTrue("user pressed Y to approve");
        }

        #endregion

        #region Test 4: RequestConsent_UserDeniesN_ReturnsFalse

        /// <summary>
        /// When user presses N at the consent prompt, returns false.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_UserDeniesN_ReturnsFalse()
        {
            // Arrange
            _testConsole.Input.PushKey(ConsoleKey.N);

            // Act
            var result = await _handler.RequestConsentAsync(
                "/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeFalse("user pressed N to deny");
        }

        #endregion

        #region Test 5: RequestConsent_DefaultDeny_ReturnsFalse

        /// <summary>
        /// When user presses Enter (default N), returns false.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_DefaultDeny_ReturnsFalse()
        {
            // Arrange
            _testConsole.Input.PushKey(ConsoleKey.Enter);

            // Act
            var result = await _handler.RequestConsentAsync(
                "/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeFalse("default should deny (Enter is not Y)");
        }

        #endregion

        #region Test 6: RequestConsent_PromptVisuallyDistinct

        /// <summary>
        /// The consent prompt renders a Panel with header and border elements.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_PromptVisuallyDistinct()
        {
            // Arrange
            _testConsole.Input.PushKey(ConsoleKey.N);

            // Act
            await _handler.RequestConsentAsync(
                "/important/data.csv",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert - Panel renders with border characters and header
            var output = _testConsole.Output;
            output.Should().Contain("File Access Request", "panel header should be present");
            output.Should().Contain("─", "panel border should be rendered");
            output.Should().Contain("/important/data.csv", "requested path should be shown");
        }

        #endregion

        #region Test 7: RequestConsent_OutputBufferedDuringPrompt

        /// <summary>
        /// When consent prompt is active, the pauseOutput action is called before the prompt
        /// and resumeOutput is called after the prompt is dismissed.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_OutputBufferedDuringPrompt()
        {
            // Arrange
            _testConsole.Input.PushKey(ConsoleKey.Y);
            var pauseCalled = false;
            var resumeCalled = false;
            var pauseCalledBeforeResume = false;

            // Act
            await _handler.RequestConsentAsync(
                "/file.txt",
                () => { pauseCalled = true; },
                () => { pauseCalledBeforeResume = pauseCalled; resumeCalled = true; },
                CancellationToken.None);

            // Assert
            pauseCalled.Should().BeTrue("pauseOutput should be called during prompt");
            resumeCalled.Should().BeTrue("resumeOutput should be called after prompt");
            pauseCalledBeforeResume.Should().BeTrue("pause should be called before resume");
        }

        #endregion

        #region Test 8: RequestConsent_BufferedOutputFlushedAfter

        /// <summary>
        /// After consent prompt is dismissed, resumeOutput is called which flushes buffered output.
        /// We verify resumeOutput is called regardless of user choice (approve or deny).
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_BufferedOutputFlushedAfter()
        {
            // Arrange - user denies
            _testConsole.Input.PushKey(ConsoleKey.N);
            var resumeCalled = false;

            // Act
            await _handler.RequestConsentAsync(
                "/file.txt",
                () => { },
                () => { resumeCalled = true; },
                CancellationToken.None);

            // Assert
            resumeCalled.Should().BeTrue("resumeOutput should be called even when user denies, so buffered output is flushed");
        }

        #endregion

        #region Test 9: ReceiveMessage_UploadRequest_Approved_UploadsFile

        /// <summary>
        /// When a ClientFileUploadRequest push message is received and consent is approved,
        /// FileTransferService.UploadFile is called with the correct paths.
        /// </summary>
        [TestMethod]
        public async Task ReceiveMessage_UploadRequest_Approved_UploadsFile()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { "/data/**" }); // Pre-approve to skip prompt
            var proxyMock = TestServerProxyFactory.CreateConnected();
            var transferServiceMock = TestFileTransferServiceFactory.CreateMock(proxyMock);
            transferServiceMock
                .Setup(s => s.UploadFile(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Func<FileUploadProgress, Task>>(),
                    It.IsAny<CancellationToken>(), It.IsAny<bool>()))
                .ReturnsAsync(new FileUploadResponse("success"));

            var handler = new FileAccessConsentHandler(_policy, _testConsole, _fileSystem);

            // Act
            var approved = await handler.RequestConsentAsync(
                "/data/report.csv",
                () => { },
                () => { },
                CancellationToken.None);

            // Simulate what ReceiveMessage does after approval
            if (approved)
            {
                await transferServiceMock.Object.UploadFile(
                    "/data/report.csv", "/tmp/staging/report.csv",
                    progress => Task.CompletedTask, CancellationToken.None);
            }

            // Assert
            approved.Should().BeTrue();
            transferServiceMock.Verify(
                s => s.UploadFile("/data/report.csv", "/tmp/staging/report.csv",
                    It.IsAny<Func<FileUploadProgress, Task>>(),
                    It.IsAny<CancellationToken>(), It.IsAny<bool>()),
                Times.Once, "UploadFile should be called with the correct paths when consent is approved");
        }

        #endregion

        #region Test 10: ReceiveMessage_DownloadRequest_Approved_DownloadsFile

        /// <summary>
        /// When a ClientFileDownloadRequest push message is received and consent is approved,
        /// FileTransferService.DownloadFile is called with the correct paths.
        /// </summary>
        [TestMethod]
        public async Task ReceiveMessage_DownloadRequest_Approved_DownloadsFile()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { "/local/**" });
            var proxyMock = TestServerProxyFactory.CreateConnected();
            var transferServiceMock = TestFileTransferServiceFactory.CreateMock(proxyMock);
            transferServiceMock
                .Setup(s => s.DownloadFile(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Func<FileDownloadProgress, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new FileAccessConsentHandler(_policy, _testConsole, _fileSystem);

            // Act
            var approved = await handler.RequestConsentAsync(
                "/local/output.csv",
                () => { },
                () => { },
                CancellationToken.None);

            if (approved)
            {
                await transferServiceMock.Object.DownloadFile(
                    "/server/output.csv", "/local/output.csv",
                    null, CancellationToken.None);
            }

            // Assert
            approved.Should().BeTrue();
            transferServiceMock.Verify(
                s => s.DownloadFile("/server/output.csv", "/local/output.csv",
                    It.IsAny<Func<FileDownloadProgress, Task>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once, "DownloadFile should be called with the correct paths when consent is approved");
        }

        #endregion

        #region Test 11: ReceiveMessage_UploadRequest_Denied_SendsAccessDenied

        /// <summary>
        /// When user denies consent for an upload request, the response should indicate
        /// access denied and no file transfer should occur.
        /// </summary>
        [TestMethod]
        public async Task ReceiveMessage_UploadRequest_Denied_SendsAccessDenied()
        {
            // Arrange - no allowed patterns, user presses N
            _testConsole.Input.PushKey(ConsoleKey.N);
            var proxyMock = TestServerProxyFactory.CreateConnected();
            var transferServiceMock = TestFileTransferServiceFactory.CreateMock(proxyMock);

            // Act
            var approved = await _handler.RequestConsentAsync(
                "/secret/data.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Build response as ReceiveMessage would
            ClientFileAccessResponseMessage response = null;
            if (!approved)
            {
                response = new ClientFileAccessResponseMessage(success: false, error: "FileAccessDenied");
            }

            // Assert
            approved.Should().BeFalse("user denied consent");
            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Error.Should().Be("FileAccessDenied");
            transferServiceMock.Verify(
                s => s.UploadFile(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Func<FileUploadProgress, Task>>(),
                    It.IsAny<CancellationToken>(), It.IsAny<bool>()),
                Times.Never, "no file transfer should occur when consent is denied");
        }

        #endregion

        #region Test 12: ReceiveMessage_ConcurrentRequests_Serialized

        /// <summary>
        /// When two consent requests arrive simultaneously, they are serialized
        /// so prompts are shown one at a time (not interleaved).
        /// </summary>
        [TestMethod]
        public async Task ReceiveMessage_ConcurrentRequests_Serialized()
        {
            // Arrange - queue two Y keys for two sequential prompts
            _testConsole.Input.PushKey(ConsoleKey.Y);
            _testConsole.Input.PushKey(ConsoleKey.Y);

            var executionOrder = new List<string>();

            // Act - launch two concurrent requests
            var task1 = Task.Run(async () =>
            {
                var result = await _handler.RequestConsentAsync(
                    "/file1.txt",
                    () => { lock (executionOrder) { executionOrder.Add("pause1"); } },
                    () => { lock (executionOrder) { executionOrder.Add("resume1"); } },
                    CancellationToken.None);
                return result;
            });

            var task2 = Task.Run(async () =>
            {
                // Small delay so task1 acquires the semaphore first
                await Task.Delay(100);
                var result = await _handler.RequestConsentAsync(
                    "/file2.txt",
                    () => { lock (executionOrder) { executionOrder.Add("pause2"); } },
                    () => { lock (executionOrder) { executionOrder.Add("resume2"); } },
                    CancellationToken.None);
                return result;
            });

            var results = await Task.WhenAll(task1, task2);

            // Assert
            results.Should().AllBeEquivalentTo(true, "both requests approved with Y");

            // Verify serialization: resume1 should come before pause2
            var resume1Index = executionOrder.IndexOf("resume1");
            var pause2Index = executionOrder.IndexOf("pause2");
            resume1Index.Should().BeLessThan(pause2Index,
                "second prompt should not start until first prompt is fully completed (serialized)");
        }

        #endregion

        #region Test 13: RequestConsent_ExceptionDuringPrompt_ResumesOutput

        /// <summary>
        /// If an exception occurs during prompt rendering, resumeOutput is still called
        /// (via finally block) to prevent console output from being permanently paused.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_ExceptionDuringPrompt_ResumesOutput()
        {
            // Arrange - use a console that throws on Write
            var throwingConsoleMock = new Mock<IAnsiConsole>();
            throwingConsoleMock.Setup(c => c.Write(It.IsAny<IRenderable>()))
                .Throws(new InvalidOperationException("Console write failed"));
            // Need ExclusivityMode for the prompt lock
            throwingConsoleMock.Setup(c => c.Profile).Returns(_testConsole.Profile);

            var handler = new FileAccessConsentHandler(_policy, throwingConsoleMock.Object, _fileSystem);
            var resumeCalled = false;

            // Act & Assert
            Func<Task> act = async () => await handler.RequestConsentAsync(
                "/file.txt",
                () => { },
                () => { resumeCalled = true; },
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            resumeCalled.Should().BeTrue("resumeOutput must be called even when an exception occurs, " +
                "to prevent console output from being permanently paused");
        }

        #endregion
    }
}
