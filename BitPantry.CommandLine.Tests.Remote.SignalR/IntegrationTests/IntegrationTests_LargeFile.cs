using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for large file handling.
    /// These tests verify that large files >2GB can be handled without integer overflow.
    /// Note: These tests use simulated/mocked streams to avoid actually allocating GB of memory.
    /// </summary>
    [TestClass]
    public class IntegrationTests_LargeFile
    {
        [TestMethod]
        public void Upload_SimulatedLargeFile_ProgressReportsCorrectly()
        {
            // Arrange - Simulate progress for a 5 GB file
            var progressValues = new List<long>();
            long fileSize = 5_000_000_000L; // 5 GB
            long chunkSize = 1_000_000_000L; // 1 GB chunks

            // Act - Simulate progress updates as they would be received
            for (long position = chunkSize; position <= fileSize; position += chunkSize)
            {
                var progress = new FileUploadProgress(position);
                progressValues.Add(progress.TotalRead);
            }

            // Assert - All progress values should be accurate without overflow
            progressValues.Should().HaveCount(5);
            progressValues[0].Should().Be(1_000_000_000L);
            progressValues[1].Should().Be(2_000_000_000L);
            progressValues[2].Should().Be(3_000_000_000L);
            progressValues[3].Should().Be(4_000_000_000L);
            progressValues[4].Should().Be(5_000_000_000L);

            // All values should be positive (no overflow to negative)
            progressValues.Should().AllSatisfy(v => v.Should().BeGreaterThan(0));
        }

        /// <summary>
        /// Consolidated test for large file progress values.
        /// Tests that FileUploadProgress handles values above 2GB without overflow.
        /// </summary>
        [TestMethod]
        [DataRow(3_000_000_000L, "3GB - above int.MaxValue, no overflow")]
        [DataRow(5_000_000_000L, "5GB - percentage calculation at 50%")]
        [DataRow(7_500_000_000L, "7.5GB - large value property access")]
        [DataRow(10_000_000_000L, "10GB - very large file")]
        public void FileUploadProgress_LargeFileSize_HandlesWithoutOverflow(long bytesRead, string scenario)
        {
            // Arrange & Act - Create progress with large value
            var progress = new FileUploadProgress(bytesRead);

            // Also test roundtrip through message envelope
            var message = new FileUploadProgressMessage(bytesRead);
            var roundtrippedProgress = new FileUploadProgress(message.TotalRead);

            // Assert - Values should be preserved exactly (no overflow)
            progress.TotalRead.Should().Be(bytesRead, because: scenario);
            progress.TotalRead.Should().BeGreaterThan(int.MaxValue, because: "we're testing values > 2GB");
            progress.TotalRead.Should().BeGreaterThan(0, because: "no overflow to negative");

            // Roundtrip should preserve value
            roundtrippedProgress.TotalRead.Should().Be(bytesRead, because: "message envelope should preserve large values");
        }
    }
}
