using BitPantry.CommandLine.Client;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Client
{
    [TestClass]
    public class ClientFileTests
    {
        // Test Validity Check:
        //   Invokes code under test: YES (ClientFile constructor, DisposeAsync)
        //   Breakage detection: YES (if properties or dispose behavior changes, tests fail)
        //   Not a tautology: YES (tests verify actual stream/property/cleanup behavior)

        [TestMethod]
        public void Constructor_SetsProperties()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var fileName = "test.txt";
            var length = 3L;

            // Act
            var clientFile = new ClientFile(stream, fileName, length);

            // Assert
            clientFile.Stream.Should().BeSameAs(stream);
            clientFile.FileName.Should().Be("test.txt");
            clientFile.Length.Should().Be(3L);
        }

        [TestMethod]
        public void Constructor_NullStream_Throws()
        {
            // Act
            Action act = () => new ClientFile(null, "test.txt", 0);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("stream");
        }

        [TestMethod]
        public void Constructor_NullFileName_Throws()
        {
            // Act
            Action act = () => new ClientFile(new MemoryStream(), null, 0);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("fileName");
        }

        [TestMethod]
        public async Task DisposeAsync_DisposesStream()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var clientFile = new ClientFile(stream, "test.txt", 3);

            // Act
            await clientFile.DisposeAsync();

            // Assert — attempting to read a disposed stream throws
            Action act = () => stream.ReadByte();
            act.Should().Throw<ObjectDisposedException>();
        }

        [TestMethod]
        public async Task DisposeAsync_CallsCleanupAction()
        {
            // Arrange
            var cleanupCalled = false;
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var clientFile = new ClientFile(stream, "test.txt", 3, () =>
            {
                cleanupCalled = true;
                return ValueTask.CompletedTask;
            });

            // Act
            await clientFile.DisposeAsync();

            // Assert
            cleanupCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task DisposeAsync_NoCleanupAction_DoesNotThrow()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var clientFile = new ClientFile(stream, "test.txt", 3, cleanupAsync: null);

            // Act
            Func<Task> act = async () => await clientFile.DisposeAsync();

            // Assert
            await act.Should().NotThrowAsync();
        }
    }
}
