using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Tests.Input
{
    [TestClass]
    public class CompositePromptTests
    {
        private Mock<ILogger<CompositePrompt>> _loggerMock;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<CompositePrompt>>();
        }

        #region CV-001: No segments returns only suffix

        /// <summary>
        /// Implements: CV-001
        /// When CompositePrompt has no segments, Then Render returns only the suffix ("> ")
        /// </summary>
        [TestMethod]
        public void Render_NoSegments_ReturnsOnlySuffix()
        {
            // Arrange
            var prompt = new CompositePrompt(_loggerMock.Object, Enumerable.Empty<IPromptSegment>());

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("> ");
        }

        #endregion

        #region CV-002: Single segment returns segment content + suffix

        /// <summary>
        /// Implements: CV-002
        /// When CompositePrompt has one segment, Then Render returns segment content + suffix
        /// </summary>
        [TestMethod]
        public void Render_SingleSegment_ReturnsSegmentWithSuffix()
        {
            // Arrange
            var segment = CreateSegment("myapp", 0);
            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment });

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("myapp> ");
        }

        #endregion

        #region CV-003: Multiple segments joined with space separator

        /// <summary>
        /// Implements: CV-003
        /// When CompositePrompt has multiple segments, Then Render joins segments with space separator + suffix
        /// </summary>
        [TestMethod]
        public void Render_MultipleSegments_JoinsWithSpaces()
        {
            // Arrange
            var segment1 = CreateSegment("app", 0);
            var segment2 = CreateSegment("@server", 100);
            var segment3 = CreateSegment("[profile]", 110);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment1, segment2, segment3 });

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("app @server [profile]> ");
        }

        #endregion

        #region CV-004: Segments render in Order ascending sequence

        /// <summary>
        /// Implements: CV-004
        /// When Segments have different Order values, Then segments render in Order ascending sequence
        /// </summary>
        [TestMethod]
        public void Render_SegmentsOrderedByOrderProperty()
        {
            // Arrange - segments added in wrong order
            var segment1 = CreateSegment("first", 0);
            var segment2 = CreateSegment("second", 100);
            var segment3 = CreateSegment("third", 50);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment2, segment3, segment1 });

            // Act
            var result = prompt.Render();

            // Assert - should be ordered: first (0), third (50), second (100)
            result.Should().Be("first third second> ");
        }

        #endregion

        #region CV-005: Null segment is skipped

        /// <summary>
        /// Implements: CV-005
        /// When Segment returns null, Then segment is skipped, no gap in output
        /// </summary>
        [TestMethod]
        public void Render_SkipsNullSegments()
        {
            // Arrange
            var segment1 = CreateSegment("app", 0);
            var segment2 = CreateSegment(null, 100);
            var segment3 = CreateSegment("[profile]", 110);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment1, segment2, segment3 });

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("app [profile]> ");
        }

        #endregion

        #region CV-006: Empty string segment is skipped

        /// <summary>
        /// Implements: CV-006
        /// When Segment returns empty string, Then segment is skipped, no gap in output
        /// </summary>
        [TestMethod]
        public void Render_SkipsEmptySegments()
        {
            // Arrange
            var segment1 = CreateSegment("app", 0);
            var segment2 = CreateSegment("", 100);
            var segment3 = CreateSegment("[profile]", 110);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment1, segment2, segment3 });

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("app [profile]> ");
        }

        #endregion

        #region CV-007: Custom suffix appears instead of default

        /// <summary>
        /// Implements: CV-007
        /// When CompositePrompt has custom suffix, Then custom suffix appears instead of default
        /// </summary>
        [TestMethod]
        public void Render_WithCustomSuffix_UsesCustomSuffix()
        {
            // Arrange
            var segment = CreateSegment("app", 0);
            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment }, "$ ");

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("app$ ");
        }

        /// <summary>
        /// Implements: CV-007 (edge case)
        /// When CompositePrompt suffix is null, Then default suffix is used
        /// </summary>
        [TestMethod]
        public void Render_WithNullSuffix_UsesDefaultSuffix()
        {
            // Arrange
            var segment = CreateSegment("app", 0);
            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment }, null);

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("app> ");
        }

        #endregion

        #region CV-008: GetPromptLength returns terminal display length

        /// <summary>
        /// Implements: CV-008
        /// When GetPromptLength called, Then returns terminal display length (handles markup)
        /// </summary>
        [TestMethod]
        public void GetPromptLength_ReturnsCorrectLength()
        {
            // Arrange
            var segment = CreateSegment("app", 0);
            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment });

            // Act
            var length = prompt.GetPromptLength();

            // Assert - "app> " = 5 characters
            length.Should().Be(5);
        }

        #endregion

        #region EH-001: Segment throws exception - caught and skipped

        /// <summary>
        /// Implements: EH-001
        /// When Segment throws exception during Render, Then exception caught, segment skipped, other segments render
        /// </summary>
        [TestMethod]
        public void Render_SegmentThrowsException_ContinuesWithOtherSegments()
        {
            // Arrange
            var segment1 = CreateSegment("app", 0);
            var throwingSegment = new Mock<IPromptSegment>();
            throwingSegment.Setup(s => s.Render()).Throws(new InvalidOperationException("Segment error"));
            throwingSegment.Setup(s => s.Order).Returns(50);
            var segment3 = CreateSegment("[profile]", 110);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment1, throwingSegment.Object, segment3 });

            // Act
            var result = prompt.Render();

            // Assert - should skip the throwing segment
            result.Should().Be("app [profile]> ");
        }

        #endregion

        #region EH-002: Exception logged with segment type name

        /// <summary>
        /// Implements: EH-002
        /// When Segment throws exception, Then warning logged with segment type name
        /// </summary>
        [TestMethod]
        public void Render_SegmentThrowsException_LogsWarning()
        {
            // Arrange
            var throwingSegment = new Mock<IPromptSegment>();
            throwingSegment.Setup(s => s.Render()).Throws(new InvalidOperationException("Test error"));
            throwingSegment.Setup(s => s.Order).Returns(0);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { throwingSegment.Object });

            // Act
            prompt.Render();

            // Assert - verify logger was called with warning level
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region EH-003: All segments throw - returns only suffix

        /// <summary>
        /// Implements: EH-003
        /// When All segments throw exceptions, Then returns only suffix (graceful degradation)
        /// </summary>
        [TestMethod]
        public void Render_AllSegmentsThrow_ReturnsOnlySuffix()
        {
            // Arrange
            var throwing1 = new Mock<IPromptSegment>();
            throwing1.Setup(s => s.Render()).Throws(new Exception("Error 1"));
            throwing1.Setup(s => s.Order).Returns(0);

            var throwing2 = new Mock<IPromptSegment>();
            throwing2.Setup(s => s.Render()).Throws(new Exception("Error 2"));
            throwing2.Setup(s => s.Order).Returns(100);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { throwing1.Object, throwing2.Object });

            // Act
            var result = prompt.Render();

            // Assert - graceful degradation to just suffix
            result.Should().Be("> ");
        }

        #endregion

        #region Additional edge cases

        [TestMethod]
        public void Render_AllNullSegments_ReturnsOnlySuffix()
        {
            // Arrange
            var segment1 = CreateSegment(null, 0);
            var segment2 = CreateSegment(null, 100);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment1, segment2 });

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("> ");
        }

        [TestMethod]
        public void Render_MixedNullAndValidSegments_RendersValidOnly()
        {
            // Arrange
            var segment1 = CreateSegment(null, 0);
            var segment2 = CreateSegment("valid", 50);
            var segment3 = CreateSegment("", 100);
            var segment4 = CreateSegment("also-valid", 150);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment1, segment2, segment3, segment4 });

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("valid also-valid> ");
        }

        #endregion

        #region Helper methods

        private IPromptSegment CreateSegment(string content, int order)
        {
            var mock = new Mock<IPromptSegment>();
            mock.Setup(s => s.Render()).Returns(content);
            mock.Setup(s => s.Order).Returns(order);
            return mock.Object;
        }

        #endregion
    }
}
