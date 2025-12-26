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
        private const string Suffix = "> ";

        public CompositePrompt(ILogger<CompositePrompt> logger, IEnumerable<IPromptSegment> segments)
        {
            _logger = logger;
            _segments = segments.OrderBy(s => s.Order).ToList();
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
            return string.IsNullOrEmpty(content) ? Suffix : content + " " + Suffix;
        }

        public int GetPromptLength() => new Text(Render()).Length;

        public void Write(IAnsiConsole console) => console.Markup(Render());
    }
}
