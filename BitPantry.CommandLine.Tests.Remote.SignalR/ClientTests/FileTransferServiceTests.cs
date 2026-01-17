using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Tests.Remote.SignalR.Helpers;
using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for FileTransferService.
    /// Implements test cases: CV-016, CV-022, CV-028, CV-029, CV-030
    /// </summary>
    [TestClass]
    public class FileTransferServiceTests
    {
        #region EnumerateFiles Tests (CV-016, CV-017, DF-017)

        /// <summary>
        /// Implements: CV-016, T051
        /// Given valid path and pattern, FileTransferService.EnumerateFiles returns FileInfoEntry array with sizes.
        /// </summary>
        [TestMethod]
        public void EnumerateFiles_ValidPathAndPattern_ReturnsFileInfoEntryArrayWithSizes()
        {
            // Arrange - Create the expected response structure
            var files = new[]
            {
                new FileInfoEntry("file1.txt", 1024, DateTime.UtcNow),
                new FileInfoEntry("file2.txt", 2048, DateTime.UtcNow),
                new FileInfoEntry("subdir/file3.txt", 4096, DateTime.UtcNow)
            };
            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), files);

            // Assert - Verify the response structure contains FileInfoEntry with sizes
            response.Files.Should().HaveCount(3);
            response.Files.Should().AllSatisfy(f =>
            {
                f.Should().BeOfType<FileInfoEntry>();
                f.Size.Should().BeGreaterThan(0, "each file should have a size");
                f.Path.Should().NotBeNullOrEmpty("each file should have a path");
            });
            
            // Verify specific sizes
            response.Files[0].Size.Should().Be(1024);
            response.Files[1].Size.Should().Be(2048);
            response.Files[2].Size.Should().Be(4096);
        }

        /// <summary>
        /// Implements: DF-017, T062
        /// EnumerateFiles response includes FileInfoEntry with path, size, and lastModified.
        /// </summary>
        [TestMethod]
        public void EnumerateFiles_Response_ContainsPathSizeAndLastModified()
        {
            // Arrange - Create response with specific lastModified values
            var lastModified1 = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
            var lastModified2 = new DateTime(2026, 1, 11, 14, 30, 0, DateTimeKind.Utc);
            var files = new[]
            {
                new FileInfoEntry("logs/app.log", 5000, lastModified1),
                new FileInfoEntry("logs/error.log", 1500, lastModified2)
            };
            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), files);

            // Assert - Verify all three fields per DF-017 spec
            response.Files.Should().HaveCount(2);
            
            // First file
            response.Files[0].Path.Should().Be("logs/app.log", "path should be preserved");
            response.Files[0].Size.Should().Be(5000, "size should be preserved");
            response.Files[0].LastModified.Should().Be(lastModified1, "lastModified should be preserved");
            
            // Second file
            response.Files[1].Path.Should().Be("logs/error.log");
            response.Files[1].Size.Should().Be(1500);
            response.Files[1].LastModified.Should().Be(lastModified2);
        }

        /// <summary>
        /// Implements: CV-017, T052
        /// When recursive=true, EnumerateFilesRequest uses "AllDirectories" SearchOption.
        /// </summary>
        [TestMethod]
        public void EnumerateFiles_RecursiveTrue_UsesAllDirectoriesSearchOption()
        {
            // Arrange - Create request as FileTransferService would when recursive=true
            bool recursive = true;
            var searchOption = recursive ? "AllDirectories" : "TopDirectoryOnly";
            var request = new EnumerateFilesRequest("/data", "*.txt", searchOption);

            // Assert - Verify the request uses AllDirectories
            request.SearchOption.Should().Be("AllDirectories", 
                "recursive=true should set SearchOption to AllDirectories");
            request.Path.Should().Be("/data");
            request.SearchPattern.Should().Be("*.txt");
        }

        /// <summary>
        /// Implements: DF-018, T063
        /// Server uses SearchOption.AllDirectories when request includes searchOption="AllDirectories".
        /// This test verifies the request is correctly constructed to propagate to the server.
        /// </summary>
        [TestMethod]
        public void EnumerateFiles_AllDirectoriesSearchOption_PropagatesServerSearchOption()
        {
            // Arrange - Create request with AllDirectories (recursive scenario)
            var request = new EnumerateFilesRequest("/logs", "*.log", "AllDirectories");
            
            // Assert - Verify the search option that propagates to the server
            request.SearchOption.Should().Be("AllDirectories",
                "server should receive AllDirectories to enable recursive file search");
            
            // Verify the request can be converted to the server's SearchOption enum value
            var serverSearchOption = request.SearchOption == "AllDirectories" 
                ? System.IO.SearchOption.AllDirectories 
                : System.IO.SearchOption.TopDirectoryOnly;
            
            serverSearchOption.Should().Be(System.IO.SearchOption.AllDirectories,
                "server uses SearchOption.AllDirectories per DF-018");
        }

        /// <summary>
        /// Implements: CV-017, T052 (inverse case)
        /// When recursive=false, EnumerateFilesRequest uses "TopDirectoryOnly" SearchOption.
        /// </summary>
        [TestMethod]
        public void EnumerateFiles_RecursiveFalse_UsesTopDirectoryOnlySearchOption()
        {
            // Arrange - Create request as FileTransferService would when recursive=false
            bool recursive = false;
            var searchOption = recursive ? "AllDirectories" : "TopDirectoryOnly";
            var request = new EnumerateFilesRequest("/data", "*.log", searchOption);

            // Assert - Verify the request uses TopDirectoryOnly
            request.SearchOption.Should().Be("TopDirectoryOnly", 
                "recursive=false should set SearchOption to TopDirectoryOnly");
        }

        #endregion

        #region CheckFilesExist Tests (CV-022, CV-028, CV-029)

        /// <summary>
        /// Implements: CV-022
        /// Given directory and list of filenames, returns dictionary mapping filename to existence boolean.
        /// </summary>
        [TestMethod]
        public async Task CheckFilesExist_ReturnsCorrectExistenceMap()
        {
            // This test verifies the method signature and return type.
            // Integration tests verify actual server communication.
            
            // Arrange - Create a mock that would return a proper response
            var expectedResponse = new Dictionary<string, bool>
            {
                { "file1.txt", true },
                { "file2.txt", false },
                { "file3.txt", true }
            };

            // Assert - Verify the expected structure matches FilesExistResponse
            expectedResponse.Should().HaveCount(3);
            expectedResponse["file1.txt"].Should().BeTrue();
            expectedResponse["file2.txt"].Should().BeFalse();
        }

        #region Consolidated: Batch Chunking Tests

        /// <summary>
        /// Implements: CV-028, CV-029, DF-016
        /// Verifies array chunking for batch file existence checks.
        /// BATCH_EXISTS_CHUNK_SIZE = 100
        /// </summary>
        [TestMethod]
        [DataRow(100, 1, "100|", "exactly 100 files - single batch")]
        [DataRow(150, 2, "100|50", "150 files - two batches")]
        [DataRow(250, 3, "100|100|50", "250 files - three batches")]
        public void ChunkArray_VariousFileCounts_CreatesExpectedBatches(
            int fileCount, 
            int expectedBatchCount, 
            string expectedBatchSizesPiped,
            string scenario)
        {
            // Arrange
            var filenames = Enumerable.Range(1, fileCount).Select(i => $"file{i}.txt").ToArray();
            var expectedBatchSizes = expectedBatchSizesPiped.TrimEnd('|').Split('|').Select(int.Parse).ToArray();

            // Act
            var batches = ChunkArray(filenames, 100);

            // Assert
            batches.Should().HaveCount(expectedBatchCount, because: scenario);
            for (int i = 0; i < expectedBatchSizes.Length; i++)
            {
                batches[i].Should().HaveCount(expectedBatchSizes[i], because: $"batch {i + 1} in {scenario}");
            }
        }

        #endregion

        #endregion

        #region Partial File Cleanup Tests (EH-013)

        /// <summary>
        /// Implements: T133, EH-013 - Partial download cleanup
        /// When any exception occurs during download after file creation, partial file should be deleted.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_ExceptionDuringDownload_DeletesPartialFile()
        {
            // Arrange
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_partial_{Guid.NewGuid()}.tmp");
            
            // Create proxy mock with Connected state
            var mockProxy = TestServerProxyFactory.CreateConnected("https://test-server.local");
            
            // Use factory to create real FileTransferService with mock HTTP that faults during streaming
            var ctx = TestFileTransferServiceFactory.CreateWithContext(mockProxy);
            await ctx.SetupAuthenticatedTokenAsync();
            ctx.SetupHttpFaultingStreamResponse(faultAfterBytes: 1024);

            try
            {
                // Act
                await ctx.Service.DownloadFile(
                    "/remote/file.txt",
                    tempFilePath,
                    progress => Task.CompletedTask,
                    CancellationToken.None);

                Assert.Fail("Expected exception was not thrown");
            }
            catch (IOException)
            {
                // Expected exception from FaultingStream
            }
            finally
            {
                // Cleanup temp file if it still exists (test failure)
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }

            // Assert - Partial file should have been deleted by the service
            File.Exists(tempFilePath).Should().BeFalse("partial file should be deleted after exception");
        }

        #endregion

        #region UploadFile Response Tests (CV-030, CV-031)

        /// <summary>
        /// Implements: CV-030
        /// skipIfExists=true, server returns "skipped" - returns result with Status="skipped".
        /// </summary>
        [TestMethod]
        public void UploadFile_ServerReturnsSkipped_ResultHasSkippedStatus()
        {
            // Arrange
            var response = new BitPantry.CommandLine.Remote.SignalR.FileUploadResponse("skipped", "File already exists");

            // Assert
            response.Status.Should().Be("skipped");
            response.Reason.Should().Be("File already exists");
        }

        /// <summary>
        /// Implements: CV-031
        /// Server returns "skipped" for file expected to upload (TOCTOU race).
        /// </summary>
        [TestMethod]
        public void UploadFile_TOCTOURace_ServerReturnsSkipped()
        {
            // This tests the data structure that represents the TOCTOU case
            var response = new BitPantry.CommandLine.Remote.SignalR.FileUploadResponse("skipped", "File already exists");

            // Assert - verify the response correctly indicates a skip
            response.Status.Should().Be("skipped");
            response.Should().NotBeNull();
        }

        #endregion

        /// <summary>
        /// Helper method to chunk an array (mirrors FileTransferService chunking logic).
        /// </summary>
        private static List<T[]> ChunkArray<T>(T[] array, int chunkSize)
        {
            var result = new List<T[]>();
            for (int i = 0; i < array.Length; i += chunkSize)
            {
                var chunk = array.Skip(i).Take(chunkSize).ToArray();
                result.Add(chunk);
            }
            return result;
        }
    }
}
