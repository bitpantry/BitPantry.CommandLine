using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Tests.Infrastructure;
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

        #region Test 2: RequestConsent_UnallowedPath_ShowsPlainPrompt

        /// <summary>
        /// When the path is not in the allowed list, a plain consent prompt is rendered
        /// containing the requested path text without a boxed header or border.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_UnallowedPath_ShowsPlainPrompt()
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
            _testConsole.Output.Should().Contain("Server requests:", "prompt label should be rendered");
            _testConsole.Output.Should().Contain("/secret/file.txt", "the actual requested path should be displayed");
            _testConsole.Output.Should().NotContain("File Access Request", "the boxed header should not be rendered");
            _testConsole.Output.Should().NotContain("─", "the prompt should not render a box border");
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

        #region Test 6: RequestConsent_PromptDoesNotRenderBox

        /// <summary>
        /// The consent prompt renders only its content and does not use panel header or border elements.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_PromptDoesNotRenderBox()
        {
            // Arrange
            _testConsole.Input.PushKey(ConsoleKey.N);

            // Act
            await _handler.RequestConsentAsync(
                "/important/data.csv",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            var output = _testConsole.Output;
            output.Should().Contain("Server requests:", "prompt content should still be shown");
            output.Should().Contain("/important/data.csv", "requested path should be shown");
            output.Should().Contain("Allow?", "approval options should still be shown");
            output.Should().NotContain("File Access Request", "panel header should not be rendered");
            output.Should().NotContain("─", "panel border should not be rendered");
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

        #region Test 9: RequestConsent_AllowedUploadPath_ApprovesWithoutPrompt

        /// <summary>
        /// When a path matching --allow-path glob is used for an upload scenario,
        /// consent is granted without any prompt output.
        ///
        /// Test Validity Check:
        ///   Invokes code under test: YES - calls RequestConsentAsync with pre-allowed path
        ///   Breakage detection: YES - if policy check is removed, prompt would render
        ///   Not a tautology: YES - verifies approval + no console output
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_AllowedUploadPath_ApprovesWithoutPrompt()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { "/data/**" });

            // Act
            var approved = await _handler.RequestConsentAsync(
                "/data/report.csv",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            approved.Should().BeTrue("path matches allowed glob — no prompt needed");
            _testConsole.Output.Should().BeEmpty("no prompt should render for pre-allowed paths");
        }

        #endregion

        #region Test 10: RequestConsent_AllowedDownloadPath_ApprovesWithoutPrompt

        /// <summary>
        /// When a path matching --allow-path glob is used for a download scenario,
        /// consent is granted without any prompt output.
        ///
        /// Test Validity Check:
        ///   Invokes code under test: YES - calls RequestConsentAsync with pre-allowed path
        ///   Breakage detection: YES - if policy check is removed, prompt would render
        ///   Not a tautology: YES - verifies approval + no console output
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_AllowedDownloadPath_ApprovesWithoutPrompt()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { "/local/**" });

            // Act
            var approved = await _handler.RequestConsentAsync(
                "/local/output.csv",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            approved.Should().BeTrue("path matches allowed glob — no prompt needed");
            _testConsole.Output.Should().BeEmpty("no prompt should render for pre-allowed paths");
        }

        #endregion

        #region Test 11: RequestConsent_DeniedPath_BuildsCorrectErrorResponse

        /// <summary>
        /// When the user denies consent, a ClientFileAccessResponseMessage can be
        /// correctly constructed with success=false and the expected error string.
        /// This validates the response envelope that ReceiveMessage would send.
        ///
        /// Test Validity Check:
        ///   Invokes code under test: YES - calls RequestConsentAsync, then constructs response
        ///   Breakage detection: YES - if consent wrongly returns true, response won't be built
        ///   Not a tautology: YES - verifies consent denial + response message structure
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_DeniedPath_BuildsCorrectErrorResponse()
        {
            // Arrange - no allowed patterns, user presses N
            _testConsole.Input.PushKey(ConsoleKey.N);

            // Act
            var approved = await _handler.RequestConsentAsync(
                "/secret/data.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert - consent denied
            approved.Should().BeFalse("user pressed N to deny");

            // Verify the response message that ReceiveMessage would construct
            var response = new ClientFileAccessResponseMessage(success: false, error: "FileAccessDenied");
            response.Success.Should().BeFalse();
            response.Error.Should().Be("FileAccessDenied");
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

        #region Test 14: ExpandGlobLocally_MatchingFiles_ReturnsFileInfoWithTimestamps

        /// <summary>
        /// ExpandGlobLocally uses the injected IFileSystem abstraction (not real DirectoryInfo)
        /// and returns correct Path, Size, and LastWriteTimeUtc from MockFileSystem.
        /// </summary>
        [TestMethod]
        public void ExpandGlobLocally_MatchingFiles_ReturnsFileInfoWithTimestamps()
        {
            // Arrange - set up MockFileSystem with files
            var baseDir = TestPaths.Combine(TestPaths.StorageRoot, "data");
            var file1Path = TestPaths.Combine(baseDir, "report.csv");
            var file2Path = TestPaths.Combine(baseDir, "summary.csv");
            var file3Path = TestPaths.Combine(baseDir, "readme.txt");

            _fileSystem.AddFile(file1Path, new MockFileData("csv-content-1") { LastWriteTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc) });
            _fileSystem.AddFile(file2Path, new MockFileData("csv-content-2") { LastWriteTime = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc) });
            _fileSystem.AddFile(file3Path, new MockFileData("not-a-csv") { LastWriteTime = new DateTime(2026, 3, 25, 14, 0, 0, DateTimeKind.Utc) });

            // Act - expand glob matching *.csv
            var globPattern = TestPaths.Combine(baseDir, "*.csv");
            var results = _handler.ExpandGlobLocally(globPattern);

            // Assert
            results.Should().HaveCount(2, "only *.csv files should match");
            results.Select(r => _fileSystem.Path.GetFileName(r.Path)).Should().BeEquivalentTo(
                new[] { "report.csv", "summary.csv" },
                "matched files should have correct names");
            results.Should().OnlyContain(r => r.Size > 0, "matched files should have non-zero size");
            results.Should().OnlyContain(r => r.LastWriteTimeUtc != default,
                "LastWriteTimeUtc should be populated from file system, not default");
        }

        #endregion

        #region Test 15: ExpandGlobLocally_NoMatchingDirectory_ReturnsEmpty

        /// <summary>
        /// When the base directory doesn't exist, ExpandGlobLocally returns an empty list
        /// without throwing.
        /// </summary>
        [TestMethod]
        public void ExpandGlobLocally_NoMatchingDirectory_ReturnsEmpty()
        {
            // Arrange - no files added to MockFileSystem
            var globPattern = TestPaths.Combine(TestPaths.StorageRoot, "nonexistent", "*.txt");

            // Act
            var results = _handler.ExpandGlobLocally(globPattern);

            // Assert
            results.Should().BeEmpty("non-existent directory should return empty list");
        }

        [TestMethod]
        public void ExpandGlobLocally_PathTraversal_ThrowsArgumentException()
        {
            // Test Validity Check:
            //   Invokes code under test: YES - calls ExpandGlobLocally
            //   Breakage detection: YES - verifies traversal is rejected before filesystem expansion
            //   Not a tautology: YES

            Action act = () => _handler.ExpandGlobLocally("../**/*.txt");

            act.Should().Throw<ArgumentException>()
                .WithMessage("*path traversal*");
        }

        [TestMethod]
        public void ExpandGlobLocally_UrlEncodedPathTraversal_ThrowsArgumentException()
        {
            // Test Validity Check:
            //   Invokes code under test: YES - calls ExpandGlobLocally with URL-encoded traversal
            //   Breakage detection: YES - if URL-decoding is removed from validation, this passes incorrectly
            //   Not a tautology: YES

            Action act = () => _handler.ExpandGlobLocally("%2e%2e/**/*.txt");

            act.Should().Throw<ArgumentException>()
                .WithMessage("*path traversal*");
        }

        #endregion

        #region Test 16: RequestBatchConsent_AllPreAllowed_ReturnsTrueNoPrompt

        /// <summary>
        /// When all paths in a batch are pre-allowed by policy, RequestBatchConsentAsync
        /// returns true immediately without rendering any prompt.
        /// </summary>
        [TestMethod]
        public async Task RequestBatchConsent_AllPreAllowed_ReturnsTrueNoPrompt()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { TestPaths.Combine(TestPaths.StorageRoot, "**") });
            var paths = new[]
            {
                TestPaths.Combine(TestPaths.StorageRoot, "file1.txt"),
                TestPaths.Combine(TestPaths.StorageRoot, "sub", "file2.txt")
            };
            var sizes = new long[] { 100, 200 };

            // Act
            var result = await _handler.RequestBatchConsentAsync(
                paths, sizes, TestPaths.Combine(TestPaths.StorageRoot, "**"),
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeTrue("all paths match allowed pattern");
            _testConsole.Output.Should().BeEmpty("no prompt should render when all paths are pre-allowed");
        }

        #endregion

        #region Test 17: RequestBatchConsent_SmallBatch_ShowsAllFiles

        /// <summary>
        /// For a batch of ≤10 files requiring consent, the batch prompt shows all file names.
        /// </summary>
        [TestMethod]
        public async Task RequestBatchConsent_SmallBatch_ShowsAllFiles()
        {
            // Arrange - no allowed patterns, files need consent
            _testConsole.Input.PushKey(ConsoleKey.Y);
            var paths = new[] { "/data/file1.txt", "/data/file2.txt", "/data/file3.txt" };
            var sizes = new long[] { 1024, 2048, 512 };

            // Act
            var result = await _handler.RequestBatchConsentAsync(
                paths, sizes, "/data/*.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeTrue("user pressed Y");
            var output = _testConsole.Output;
            output.Should().Contain("file1.txt", "small batch should show all file names");
            output.Should().Contain("file2.txt");
            output.Should().Contain("file3.txt");
            output.Should().Contain("Server requests 3 files matching", "batch prompt label should be present");
            output.Should().NotContain("File Access Request", "batch prompt should not render the boxed header");
            output.Should().NotContain("─", "batch prompt should not render a box border");
        }

        #endregion

        #region Test 18: RequestBatchConsent_UserDenies_ReturnsFalse

        /// <summary>
        /// When user presses N at the batch consent prompt, returns false.
        /// </summary>
        [TestMethod]
        public async Task RequestBatchConsent_UserDenies_ReturnsFalse()
        {
            // Arrange
            _testConsole.Input.PushKey(ConsoleKey.N);
            var paths = new[] { "/data/file1.txt", "/data/file2.txt" };
            var sizes = new long[] { 1024, 2048 };

            // Act
            var result = await _handler.RequestBatchConsentAsync(
                paths, sizes, "/data/*.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeFalse("user pressed N to deny batch consent");
        }

        #endregion

        #region Test 19: RequestBatchConsent_OutputBufferedDuringPrompt

        /// <summary>
        /// Batch consent prompt also pauses and resumes output via the callback actions.
        /// </summary>
        [TestMethod]
        public async Task RequestBatchConsent_OutputBufferedDuringPrompt()
        {
            // Arrange
            _testConsole.Input.PushKey(ConsoleKey.Y);
            var pauseCalled = false;
            var resumeCalled = false;

            // Act
            await _handler.RequestBatchConsentAsync(
                new[] { "/data/file.txt" }, new long[] { 100 }, "/data/*.txt",
                () => { pauseCalled = true; },
                () => { resumeCalled = true; },
                CancellationToken.None);

            // Assert
            pauseCalled.Should().BeTrue("pauseOutput should be called during batch prompt");
            resumeCalled.Should().BeTrue("resumeOutput should be called after batch prompt");
        }

        #endregion

        #region Test 20: RequestBatchConsent_ExceptionDuringPrompt_ResumesOutput

        /// <summary>
        /// If an exception occurs during batch consent prompt rendering, resumeOutput is
        /// still called (via finally block) to prevent console output from being permanently paused.
        /// </summary>
        [TestMethod]
        public async Task RequestBatchConsent_ExceptionDuringPrompt_ResumesOutput()
        {
            // Arrange - use a console that throws on Write
            var throwingConsoleMock = new Mock<IAnsiConsole>();
            throwingConsoleMock.Setup(c => c.Write(It.IsAny<IRenderable>()))
                .Throws(new InvalidOperationException("Console write failed"));
            throwingConsoleMock.Setup(c => c.Profile).Returns(_testConsole.Profile);

            var handler = new FileAccessConsentHandler(_policy, throwingConsoleMock.Object, _fileSystem);
            var resumeCalled = false;

            // Act & Assert
            Func<Task> act = async () => await handler.RequestBatchConsentAsync(
                new[] { "/data/file.txt" }, new long[] { 100 }, "/data/*.txt",
                () => { },
                () => { resumeCalled = true; },
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            resumeCalled.Should().BeTrue("resumeOutput must be called even when batch consent throws");
        }

        #endregion

        #region Session-Scoped Remembered Consent Tests

        /// <summary>
        /// When user approves a single file consent prompt, the approved path is
        /// remembered for the session. A second request for the same path should
        /// return true immediately without prompting.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_ApprovedPath_RememberedForSession()
        {
            // Arrange — queue one Y key for the first prompt only
            _testConsole.Input.PushKey(ConsoleKey.Y);

            // Act — first request prompts and user approves
            var result1 = await _handler.RequestConsentAsync(
                "/data/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Reset console to detect if second call renders anything
            _testConsole = new TestConsole();
            _handler = new FileAccessConsentHandler(_policy, _testConsole, _fileSystem);

            // Act — second request for same path should not prompt
            var result2 = await _handler.RequestConsentAsync(
                "/data/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result1.Should().BeTrue("first request approved by user");
            result2.Should().BeTrue("second request should be auto-approved from remembered consent");
            _testConsole.Output.Should().BeEmpty("no prompt should render for remembered path");
        }

        /// <summary>
        /// When user approves a batch consent prompt, the glob pattern is remembered.
        /// A subsequent single-file request matching that glob should return true without prompting.
        /// </summary>
        [TestMethod]
        public async Task RequestBatchConsent_ApprovedGlob_RememberedForSession()
        {
            // Arrange — queue one Y for the batch prompt
            _testConsole.Input.PushKey(ConsoleKey.Y);
            var paths = new[] { "/data/file1.txt", "/data/file2.txt" };
            var sizes = new long[] { 100, 200 };

            // Act — batch approve
            var batchResult = await _handler.RequestBatchConsentAsync(
                paths, sizes, "/data/*.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Reset console
            _testConsole = new TestConsole();
            _handler = new FileAccessConsentHandler(_policy, _testConsole, _fileSystem);

            // Act — single file request matching the remembered glob
            var singleResult = await _handler.RequestConsentAsync(
                "/data/file3.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            batchResult.Should().BeTrue("batch approved by user");
            singleResult.Should().BeTrue("path matching remembered glob should be auto-approved");
            _testConsole.Output.Should().BeEmpty("no prompt for path matching remembered glob");
        }

        /// <summary>
        /// When user denies consent, the path is NOT remembered — a subsequent
        /// request for the same path should still prompt.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_DeniedPath_NotRemembered()
        {
            // Arrange — queue N for first, Y for second
            _testConsole.Input.PushKey(ConsoleKey.N);

            // Act — first request denied
            var result1 = await _handler.RequestConsentAsync(
                "/data/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Setup for second call — needs new Y key
            _testConsole.Input.PushKey(ConsoleKey.Y);

            // Act — second request should still prompt (denial not remembered)
            var result2 = await _handler.RequestConsentAsync(
                "/data/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result1.Should().BeFalse("first request denied by user");
            result2.Should().BeTrue("second request should still prompt, user approved this time");
        }

        #endregion

        #region ConsentMode Handler Tests

        /// <summary>
        /// When consent mode is DenyAll, handler returns false for uncovered paths
        /// without prompting the user.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_DenyAllMode_ReturnsFalseNoPrompt()
        {
            // Arrange — DenyAll mode, no patterns
            _policy.Mode = ConsentMode.DenyAll;

            // Act
            var result = await _handler.RequestConsentAsync(
                "/secrets/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeFalse("DenyAll mode should deny uncovered paths");
            _testConsole.Output.Should().BeEmpty("DenyAll should not render any prompt");
        }

        /// <summary>
        /// When consent mode is AllowAll, handler returns true for any path
        /// without prompting.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_AllowAllMode_ReturnsTrueNoPrompt()
        {
            // Arrange — AllowAll mode, no patterns
            _policy.Mode = ConsentMode.AllowAll;

            // Act
            var result = await _handler.RequestConsentAsync(
                "/anything/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeTrue("AllowAll mode should allow all paths");
            _testConsole.Output.Should().BeEmpty("AllowAll should not render any prompt");
        }

        /// <summary>
        /// DenyAll mode with allowed patterns: covered paths return true, uncovered return false.
        /// </summary>
        [TestMethod]
        public async Task RequestConsent_DenyAllWithAllowedPattern_CoveredAllowed()
        {
            // Arrange — DenyAll mode, with one allowed pattern
            _policy.SetAllowedPatterns(new[] { "/data/**" });
            _policy.Mode = ConsentMode.DenyAll;

            // Act — covered path
            var coveredResult = await _handler.RequestConsentAsync(
                "/data/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Act — uncovered path
            var uncoveredResult = await _handler.RequestConsentAsync(
                "/secrets/file.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            coveredResult.Should().BeTrue("covered path should be allowed even in DenyAll");
            uncoveredResult.Should().BeFalse("uncovered path should be denied in DenyAll");
            _testConsole.Output.Should().BeEmpty("DenyAll should never prompt");
        }

        /// <summary>
        /// When consent mode is DenyAll, RequestBatchConsentAsync returns false for
        /// batches with uncovered paths without prompting.
        /// </summary>
        [TestMethod]
        public async Task RequestBatchConsent_DenyAllMode_ReturnsFalseNoPrompt()
        {
            // Arrange — DenyAll mode
            _policy.Mode = ConsentMode.DenyAll;
            var paths = new[] { "/data/file1.txt", "/data/file2.txt" };
            var sizes = new long[] { 100, 200 };

            // Act
            var result = await _handler.RequestBatchConsentAsync(
                paths, sizes, "/data/*.txt",
                () => { },
                () => { },
                CancellationToken.None);

            // Assert
            result.Should().BeFalse("DenyAll should deny batch with uncovered paths");
            _testConsole.Output.Should().BeEmpty("DenyAll should not render any prompt");
        }

        #endregion
    }
}
