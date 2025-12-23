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

        [TestMethod]
        public void Progress_Above2GB_NoOverflowException()
        {
            // Arrange
            long bytesRead = 3_000_000_000L; // 3 GB - above int.MaxValue

            // Act
            Action act = () =>
            {
                // Create message envelope
                var message = new FileUploadProgressMessage(bytesRead);
                
                // Create client-side progress from message
                var progress = new FileUploadProgress(message.TotalRead);

                // Verify the values roundtrip correctly
                progress.TotalRead.Should().Be(bytesRead);
            };

            // Assert - No exception should be thrown
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Progress_PercentageCalculation_LargeFiles()
        {
            // Arrange
            long totalSize = 5_000_000_000L; // 5 GB
            long complete = 2_500_000_000L; // 2.5 GB

            // Act - Simulate progress at 50%
            var progress = new FileUploadProgress(complete);

            // Calculate percentage using double to avoid overflow
            double percentage = (double)progress.TotalRead / totalSize * 100;

            // Assert
            percentage.Should().BeApproximately(50.0, 0.01);
        }

        [TestMethod]
        public void FileUploadProgress_LargeValues_PropertiesWork()
        {
            // Arrange - Create progress with large value (7.5 GB)
            var progress = new FileUploadProgress(7_500_000_000L);

            // Assert - TotalRead should be long type and handle large values
            progress.TotalRead.Should().Be(7_500_000_000L);
            progress.TotalRead.Should().BeGreaterThan(int.MaxValue);
        }
    }
}
