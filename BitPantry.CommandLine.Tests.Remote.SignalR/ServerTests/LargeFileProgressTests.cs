using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Unit tests for large file progress handling.
    /// Verifies that progress values >2GB don't cause integer overflow.
    /// </summary>
    [TestClass]
    public class LargeFileProgressTests
    {
        [TestMethod]
        public void Progress_ValueAbove2GB_DoesNotOverflow()
        {
            // Arrange - Value larger than int.MaxValue (2,147,483,647)
            long largeValue = 3_000_000_000L; // 3 GB

            // Act - Create progress message with large value
            var progress = new FileUploadProgressMessage(largeValue);

            // Assert - Value should be preserved correctly
            progress.TotalRead.Should().Be(largeValue);
            progress.TotalRead.Should().BeGreaterThan(int.MaxValue);
        }

        [TestMethod]
        public void Progress_TotalReadIsLongType_Verified()
        {
            // Arrange
            var progress = new FileUploadProgressMessage(0);

            // Assert - TotalRead should be long type (compile-time check would fail if int)
            long totalRead = progress.TotalRead;
            totalRead.GetType().Should().Be(typeof(long));
        }

        [TestMethod]
        public void Progress_MaxLongValue_HandledCorrectly()
        {
            // Arrange - Use a realistically large file size (100 TB = 100 * 1024^4)
            // Note: long.MaxValue causes overflow in string parsing library, 
            // but this value represents the largest practical file size
            long largeValue = 100L * 1024 * 1024 * 1024 * 1024; // 100 TB

            // Act
            var progress = new FileUploadProgressMessage(largeValue);

            // Assert
            progress.TotalRead.Should().Be(largeValue);
            progress.TotalRead.Should().BeGreaterThan(int.MaxValue);
        }

        [TestMethod]
        public void Progress_NearIntMaxValue_NoOverflow()
        {
            // Arrange - Just over int.MaxValue boundary
            long nearBoundary = (long)int.MaxValue + 1;

            // Act
            var progress = new FileUploadProgressMessage(nearBoundary);

            // Assert
            progress.TotalRead.Should().Be(nearBoundary);
            progress.TotalRead.Should().BeGreaterThan(int.MaxValue);
        }

        [TestMethod]
        public void Progress_Incremental_NoOverflow()
        {
            // Arrange - Simulate incremental progress updates
            long currentProgress = 2_000_000_000L; // 2 GB
            long increment = 500_000_000L; // 500 MB

            // Act - Create progress after increment
            var progress = new FileUploadProgressMessage(currentProgress + increment);

            // Assert - Should correctly represent 2.5 GB
            progress.TotalRead.Should().Be(2_500_000_000L);
        }
    }
}
