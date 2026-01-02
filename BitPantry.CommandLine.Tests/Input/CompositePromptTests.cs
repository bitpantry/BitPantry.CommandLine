using System;
using System.Collections.Generic;
using System.Linq;
using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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

        [TestMethod]
        public void Render_SingleSegment_ReturnsSegmentWithSuffix()
        {
            // Arrange
            var segmentMock = new Mock<IPromptSegment>();
            segmentMock.Setup(s => s.Render()).Returns("myapp");
            segmentMock.Setup(s => s.Order).Returns(0);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segmentMock.Object });

            // Act
            var result = prompt.Render();

            // Assert
            result.Should().Be("myapp> ");
        }

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

        [TestMethod]
        public void Render_ContinuesIfSegmentThrows()
        {
            // Arrange
            var segment1 = CreateSegment("app", 0);
            var throwingSegment = new Mock<IPromptSegment>();
            throwingSegment.Setup(s => s.Render()).Throws(new Exception("Segment error"));
            throwingSegment.Setup(s => s.Order).Returns(50);
            var segment3 = CreateSegment("[profile]", 110);

            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment1, throwingSegment.Object, segment3 });

            // Act
            var result = prompt.Render();

            // Assert - should skip the throwing segment
            result.Should().Be("app [profile]> ");
        }

        [TestMethod]
        public void GetPromptLength_ReturnsCorrectLength()
        {
            // Arrange
            var segment = CreateSegment("app", 0);
            var prompt = new CompositePrompt(_loggerMock.Object, new[] { segment });

            // Act
            var length = prompt.GetPromptLength();

            // Assert
            length.Should().Be("app> ".Length);
        }

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

        private IPromptSegment CreateSegment(string content, int order)
        {
            var mock = new Mock<IPromptSegment>();
            mock.Setup(s => s.Render()).Returns(content);
            mock.Setup(s => s.Order).Returns(order);
            return mock.Object;
        }
    }
}
