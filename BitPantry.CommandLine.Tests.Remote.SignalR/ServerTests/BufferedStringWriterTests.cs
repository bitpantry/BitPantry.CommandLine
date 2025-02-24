using BitPantry.CommandLine.Remote.SignalR.Server;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class BufferedStringWriterTests
    {
        [TestMethod]
        public void WriteSingleCharacter_ShouldBeReadable()
        {
            // Arrange
            var writer = new BufferedStringWriter();
            char input = 'A';

            // Act
            writer.Write(input);
            string result = writer.Read(CancellationToken.None);

            // Assert
            result.Should().Be(input.ToString());
        }

        [TestMethod]
        public void WriteString_ShouldBeReadable()
        {
            // Arrange
            var writer = new BufferedStringWriter();
            string input = "Hello, World!";

            // Act
            writer.Write(input);
            string result = writer.Read(CancellationToken.None);

            // Assert
            result.Should().Be(input);
        }

        [TestMethod]
        public void WriteCharArray_ShouldBeReadable()
        {
            // Arrange
            var writer = new BufferedStringWriter();
            char[] input = "Hello, World!".ToCharArray();

            // Act
            writer.Write(input, 0, input.Length);
            string result = writer.Read(CancellationToken.None);

            // Assert
            result.Should().Be(new string(input));
        }

        [TestMethod]
        public void ConcurrentWrites_ShouldBeReadable()
        {
            // Arrange
            var writer = new BufferedStringWriter();
            string[] inputs = { "Hello", " ", "World", "!" };

            // Act
            Parallel.ForEach(inputs, input => writer.Write(input));
            string result = writer.Read(CancellationToken.None);

            // Assert
            // Sort the inputs and the result to ensure they match regardless of order
            var sortedInputs = string.Concat(inputs).OrderBy(c => c).ToArray();
            var sortedResult = result.OrderBy(c => c).ToArray();
            sortedResult.Should().BeEquivalentTo(sortedInputs);
        }

        [TestMethod]
        public void ReadWithCancellation_ShouldReturnNull()
        {
            // Arrange
            var writer = new BufferedStringWriter();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            string result = writer.Read(cts.Token);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ReadWithNoData_ShouldWait()
        {
            // Arrange
            var writer = new BufferedStringWriter();
            var cts = new CancellationTokenSource(100); // Cancel after 100ms

            // Act
            string result = writer.Read(cts.Token);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ReadAfterMultipleWrites_ShouldReturnAllData()
        {
            // Arrange
            var writer = new BufferedStringWriter();
            string[] inputs = { "Hello", " ", "World", "!" };

            // Act
            foreach (var input in inputs)
            {
                writer.Write(input);
            }
            string result = writer.Read(CancellationToken.None);

            // Assert
            result.Should().Be(string.Concat(inputs));
        }
    }
}
