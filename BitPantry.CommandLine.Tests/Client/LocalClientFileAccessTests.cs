using BitPantry.CommandLine.Client;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Client
{
    [TestClass]
    public class LocalClientFileAccessTests
    {
        // Test Validity Check:
        //   Invokes code under test: YES (LocalClientFileAccess methods)
        //   Breakage detection: YES (verifies file content, stream data, exceptions, directory creation)
        //   Not a tautology: YES (tests actual file I/O behavior against MockFileSystem)

        private MockFileSystem _fs;
        private LocalClientFileAccess _sut;

        [TestInitialize]
        public void Setup()
        {
            _fs = new MockFileSystem();
            _sut = new LocalClientFileAccess(_fs);
        }

        /// <summary>
        /// Synchronous progress implementation that captures reports immediately
        /// (unlike <see cref="Progress{T}"/> which posts to the thread pool).
        /// </summary>
        private class SyncProgress<T> : IProgress<T>
        {
            private readonly List<T> _reports;
            public SyncProgress(List<T> reports) => _reports = reports;
            public void Report(T value) => _reports.Add(value);
        }

        // ── GetFileAsync ─────────────────────────────────────────────────

        [TestMethod]
        public async Task GetFileAsync_ExistingFile_ReturnsClientFileWithCorrectProperties()
        {
            // Arrange
            var content = "hello world";
            _fs.AddFile("/data/test.txt", new MockFileData(content));

            // Act
            await using var file = await _sut.GetFileAsync("/data/test.txt");

            // Assert
            file.FileName.Should().Be("test.txt");
            file.Length.Should().Be(content.Length);
            file.Stream.Should().NotBeNull();

            using var reader = new StreamReader(file.Stream);
            var readContent = await reader.ReadToEndAsync();
            readContent.Should().Be(content);
        }

        [TestMethod]
        public async Task GetFileAsync_MissingFile_ThrowsFileNotFoundException()
        {
            // Act
            Func<Task> act = async () => await _sut.GetFileAsync("/data/missing.txt");

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [TestMethod]
        public async Task GetFileAsync_ReportsProgressWithTotalBytes()
        {
            // Arrange
            var content = "some content";
            _fs.AddFile("/data/file.txt", new MockFileData(content));
            var reported = new List<FileTransferProgress>();

            // Act
            await using var file = await _sut.GetFileAsync("/data/file.txt", new SyncProgress<FileTransferProgress>(reported));

            // Assert
            reported.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new FileTransferProgress(content.Length, content.Length));
        }

        // ── SaveFileAsync(Stream, ...) ───────────────────────────────────

        [TestMethod]
        public async Task SaveFileAsync_Stream_WritesContentToFile()
        {
            // Arrange
            var content = "file content here";
            _fs.AddDirectory("/output");
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            // Act
            await _sut.SaveFileAsync(stream, "/output/result.txt");

            // Assert
            _fs.File.ReadAllText("/output/result.txt").Should().Be(content);
        }

        [TestMethod]
        public async Task SaveFileAsync_Stream_CreatesParentDirectories()
        {
            // Arrange
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("data"));

            // Act
            await _sut.SaveFileAsync(stream, "/deep/nested/dir/file.txt");

            // Assert
            _fs.Directory.Exists("/deep/nested/dir").Should().BeTrue();
            _fs.File.Exists("/deep/nested/dir/file.txt").Should().BeTrue();
        }

        [TestMethod]
        public async Task SaveFileAsync_Stream_ReportsProgress()
        {
            // Arrange
            var data = new byte[100_000]; // Large enough to trigger multiple buffer reads
            new Random(42).NextBytes(data);
            _fs.AddDirectory("/output");
            using var stream = new MemoryStream(data);
            var reported = new List<FileTransferProgress>();

            // Act
            await _sut.SaveFileAsync(stream, "/output/big.bin", new SyncProgress<FileTransferProgress>(reported));

            // Assert
            reported.Should().NotBeEmpty();
            var last = reported[reported.Count - 1];
            last.BytesTransferred.Should().Be(data.Length);
            last.TotalBytes.Should().Be(data.Length); // MemoryStream supports seeking
        }

        [TestMethod]
        public async Task SaveFileAsync_Stream_RespectsNonSeekableStream()
        {
            // Arrange
            var data = System.Text.Encoding.UTF8.GetBytes("short");
            _fs.AddDirectory("/output");
            using var stream = new NonSeekableStream(data);
            var reported = new List<FileTransferProgress>();

            // Act
            await _sut.SaveFileAsync(stream, "/output/file.txt", new SyncProgress<FileTransferProgress>(reported));

            // Assert
            reported.Should().NotBeEmpty();
            var last = reported[reported.Count - 1];
            last.BytesTransferred.Should().Be(data.Length);
            last.TotalBytes.Should().BeNull(); // Non-seekable stream doesn't know total
        }

        // ── SaveFileAsync(string, string, ...) ──────────────────────────

        [TestMethod]
        public async Task SaveFileAsync_Path_CopiesFileToDestination()
        {
            // Arrange
            var content = "source content";
            _fs.AddFile("/source/data.txt", new MockFileData(content));
            _fs.AddDirectory("/dest");

            // Act
            await _sut.SaveFileAsync("/source/data.txt", "/dest/copy.txt");

            // Assert
            _fs.File.ReadAllText("/dest/copy.txt").Should().Be(content);
        }

        [TestMethod]
        public async Task SaveFileAsync_Path_CreatesParentDirectories()
        {
            // Arrange
            _fs.AddFile("/source/file.txt", new MockFileData("x"));

            // Act
            await _sut.SaveFileAsync("/source/file.txt", "/new/deep/path/file.txt");

            // Assert
            _fs.Directory.Exists("/new/deep/path").Should().BeTrue();
            _fs.File.Exists("/new/deep/path/file.txt").Should().BeTrue();
        }

        [TestMethod]
        public async Task SaveFileAsync_Path_MissingSource_ThrowsFileNotFoundException()
        {
            // Act
            Func<Task> act = async () => await _sut.SaveFileAsync("/nonexistent.txt", "/dest/out.txt");

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [TestMethod]
        public async Task SaveFileAsync_Path_ReportsProgress()
        {
            // Arrange
            var data = new byte[50_000];
            new Random(42).NextBytes(data);
            _fs.AddFile("/source/big.bin", new MockFileData(data));
            _fs.AddDirectory("/dest");
            var reported = new List<FileTransferProgress>();

            // Act
            await _sut.SaveFileAsync("/source/big.bin", "/dest/big.bin", new SyncProgress<FileTransferProgress>(reported));

            // Assert
            reported.Should().NotBeEmpty();
            var last = reported[reported.Count - 1];
            last.BytesTransferred.Should().Be(data.Length);
            last.TotalBytes.Should().Be(data.Length);
        }

        // ── Helper: non-seekable stream ──────────────────────────────────

        private class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] buffer) : base(buffer) { }
            public override bool CanSeek => false;
        }

        // ── Overwrite / Cancellation ─────────────────────────────────────

        [TestMethod]
        public async Task SaveFileAsync_Stream_OverwritesExistingFile()
        {
            // Arrange
            _fs.AddFile("/output/existing.txt", new MockFileData("old content"));
            var newContent = "new content";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(newContent));

            // Act
            await _sut.SaveFileAsync(stream, "/output/existing.txt");

            // Assert
            _fs.File.ReadAllText("/output/existing.txt").Should().Be(newContent);
        }

        [TestMethod]
        public async Task SaveFileAsync_WithCancellation_ThrowsOperationCanceled()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("data"));

            // Act
            Func<Task> act = async () => await _sut.SaveFileAsync(stream, "/output/file.txt", ct: cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
