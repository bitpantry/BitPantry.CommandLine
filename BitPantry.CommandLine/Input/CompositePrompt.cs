using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Input
{
    /// <summary>
    /// Default prompt implementation that aggregates registered segments.
    /// </summary>
    public class CompositePrompt : IPrompt
    {
        private readonly ILogger<CompositePrompt> _logger;
        private readonly IEnumerable<IPromptSegment> _segments;
        private readonly string _suffix;

        /// <summary>
        /// Creates a new CompositePrompt with the default suffix "&gt; ".
        /// </summary>
        public CompositePrompt(ILogger<CompositePrompt> logger, IEnumerable<IPromptSegment> segments)
            : this(logger, segments, "> ")
        {
        }

        /// <summary>
        /// Creates a new CompositePrompt with a custom suffix.
        /// </summary>
        /// <param name="logger">Logger for error reporting.</param>
        /// <param name="segments">The prompt segments to render.</param>
        /// <param name="suffix">The suffix to append. Supports Spectre.Console markup.</param>
        public CompositePrompt(ILogger<CompositePrompt> logger, IEnumerable<IPromptSegment> segments, string suffix)
        {
            _logger = logger;
            _segments = segments.OrderBy(s => s.Order).ToList();
            _suffix = suffix ?? "> ";
        }

        public string Render()
        {
            var parts = new List<string>();

            foreach (var segment in _segments)
            {
                try
                {
                    var rendered = segment.Render();
                    if (!string.IsNullOrEmpty(rendered))
                    {
                        parts.Add(rendered);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue with other segments
                    _logger.LogWarning(ex, "Error rendering prompt segment {SegmentType}", segment.GetType().Name);
                }
            }

            var content = string.Join(" ", parts);
            return string.IsNullOrEmpty(content) ? _suffix : content + " " + _suffix;
        }

        public int GetPromptLength() => Render().GetTerminalDisplayLength();

        public void Write(IAnsiConsole console) => console.Markup(Render());
    }
}
