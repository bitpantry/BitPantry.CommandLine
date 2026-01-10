namespace BitPantry.CommandLine.Input
{
    /// <summary>
    /// Contributes a segment to the composite prompt.
    /// Implementations are resolved from DI and rendered in Order sequence.
    /// </summary>
    public interface IPromptSegment
    {
        /// <summary>
        /// Order in which this segment appears. Lower values appear first.
        /// </summary>
        /// <remarks>
        /// Convention:
        /// <list type="bullet">
        ///   <item>0-99: Core (application name, base state)</item>
        ///   <item>100-199: Connection state (server, profile)</item>
        ///   <item>200-299: Session state (future extensions)</item>
        ///   <item>300+: Custom/user segments</item>
        /// </list>
        /// </remarks>
        int Order { get; }

        /// <summary>
        /// Renders this segment's content.
        /// </summary>
        /// <returns>
        /// The segment text including any decorators (e.g., "@", "[]"),
        /// or null to skip this segment entirely.
        /// </returns>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>Return null to hide the segment (e.g., when disconnected)</item>
        ///   <item>Return empty string to reserve space with no content</item>
        ///   <item>Include decorators in return value (segment owns its formatting)</item>
        ///   <item>Do not include trailing space (CompositePrompt adds separators)</item>
        /// </list>
        /// </remarks>
        string Render();
    }
}
