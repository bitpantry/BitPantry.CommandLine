using FluentAssertions;
using System.Security.Cryptography;
using System.Text;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Unit tests for checksum computation and verification functionality.
    /// These tests verify SHA256 hash computation and validation logic.
    /// </summary>
    [TestClass]
    public class ChecksumTests
    {
        /// <summary>
        /// Helper method to compute SHA256 hash incrementally (simulating the server's approach).
        /// </summary>
        private static string ComputeIncrementalHash(Stream stream, int bufferSize = 81920)
        {
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[bufferSize];
            int bytesRead;
            
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                hasher.AppendData(buffer, 0, bytesRead);
            }
            
            return Convert.ToHexString(hasher.GetHashAndReset());
        }

        /// <summary>
        /// Helper method to verify a checksum matches expected value.
        /// </summary>
        private static void VerifyChecksum(string expected, string actual)
        {
            if (string.IsNullOrEmpty(expected))
                throw new ArgumentException("Checksum header is required", nameof(expected));

            // Validate hex format (must be 64 hex characters for SHA256)
            if (expected.Length != 64 || !expected.All(c => Uri.IsHexDigit(c)))
                throw new FormatException("Invalid checksum format. Expected 64 hexadecimal characters.");

            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Checksum mismatch. Expected: {expected}, Actual: {actual}");
        }

        [TestMethod]
        public void ComputeIncrementalHash_ValidStream_ReturnsCorrectSha256()
        {
            // Arrange
            var content = "Hello, World! This is a test file content.";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            
            // Compute expected hash using standard SHA256
            using var sha256 = SHA256.Create();
            var expectedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            var expectedHash = Convert.ToHexString(expectedBytes);

            // Act
            var actualHash = ComputeIncrementalHash(stream);

            // Assert
            actualHash.Should().Be(expectedHash);
        }

        [TestMethod]
        public void ComputeIncrementalHash_EmptyStream_ReturnsEmptyFileHash()
        {
            // Arrange
            using var stream = new MemoryStream();
            
            // Expected hash for empty content
            using var sha256 = SHA256.Create();
            var expectedBytes = sha256.ComputeHash(Array.Empty<byte>());
            var expectedHash = Convert.ToHexString(expectedBytes);

            // Act
            var actualHash = ComputeIncrementalHash(stream);

            // Assert
            actualHash.Should().Be(expectedHash);
            // SHA256 of empty data is a well-known value
            actualHash.Should().Be("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
        }

        [TestMethod]
        public void ComputeIncrementalHash_LargeStream_ComputesCorrectly()
        {
            // Arrange - Create a stream larger than the buffer size
            var largeContent = new string('A', 100000); // 100KB
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeContent));
            
            // Compute expected hash
            using var sha256 = SHA256.Create();
            var expectedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(largeContent));
            var expectedHash = Convert.ToHexString(expectedBytes);

            // Act - Use smaller buffer to force multiple reads
            var actualHash = ComputeIncrementalHash(stream, bufferSize: 1024);

            // Assert
            actualHash.Should().Be(expectedHash);
        }

        [TestMethod]
        public void VerifyChecksum_MatchingHash_Succeeds()
        {
            // Arrange
            var content = "Test content for checksum";
            using var sha256 = SHA256.Create();
            var hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(content)));

            // Act & Assert - Should not throw
            var act = () => VerifyChecksum(hash, hash);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void VerifyChecksum_MismatchedHash_ThrowsException()
        {
            // Arrange
            var expectedHash = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
            var actualHash = "FEDCBA9876543210FEDCBA9876543210FEDCBA9876543210FEDCBA9876543210";

            // Act
            var act = () => VerifyChecksum(expectedHash, actualHash);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Checksum mismatch*");
        }

        [TestMethod]
        public void VerifyChecksum_MissingHeader_ThrowsException()
        {
            // Arrange
            var actualHash = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

            // Act - null header
            var actNull = () => VerifyChecksum(null!, actualHash);
            
            // Act - empty header
            var actEmpty = () => VerifyChecksum("", actualHash);

            // Assert
            actNull.Should().Throw<ArgumentException>()
                .WithMessage("*required*");
            actEmpty.Should().Throw<ArgumentException>()
                .WithMessage("*required*");
        }

        [TestMethod]
        public void VerifyChecksum_InvalidHexFormat_ThrowsException()
        {
            // Arrange
            var actualHash = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
            
            // Various invalid formats
            var tooShort = "ABCDEF";
            var invalidChars = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789GHIJKL";
            var withSpaces = "0123456789ABCDEF 0123456789ABCDEF0123456789ABCDEF0123456789ABCDE";

            // Act & Assert
            var actTooShort = () => VerifyChecksum(tooShort, actualHash);
            actTooShort.Should().Throw<FormatException>()
                .WithMessage("*Invalid checksum format*");

            var actInvalidChars = () => VerifyChecksum(invalidChars, actualHash);
            actInvalidChars.Should().Throw<FormatException>()
                .WithMessage("*Invalid checksum format*");

            var actWithSpaces = () => VerifyChecksum(withSpaces, actualHash);
            actWithSpaces.Should().Throw<FormatException>()
                .WithMessage("*Invalid checksum format*");
        }

        [TestMethod]
        public void VerifyChecksum_CaseInsensitive_Succeeds()
        {
            // Arrange
            var lowerHash = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
            var upperHash = "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789";

            // Act & Assert - Comparison should be case-insensitive
            var act = () => VerifyChecksum(lowerHash, upperHash);
            act.Should().NotThrow();
        }
    }
}
