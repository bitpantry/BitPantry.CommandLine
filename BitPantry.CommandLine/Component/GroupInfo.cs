using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Component
{
    /// <summary>
    /// Contains runtime information about a registered command group.
    /// </summary>
    public class GroupInfo
    {
        private readonly List<GroupInfo> _childGroups = new List<GroupInfo>();
        private readonly List<CommandInfo> _commands = new List<CommandInfo>();

        /// <summary>
        /// Creates a new GroupInfo instance.
        /// </summary>
        /// <param name="name">The group name (lowercased, derived from class name).</param>
        /// <param name="description">Human-readable description for help display.</param>
        /// <param name="parent">Parent group, or null for top-level groups.</param>
        /// <param name="markerType">The marker class type decorated with [Group].</param>
        public GroupInfo(string name, string description, GroupInfo parent, Type markerType)
        {
            Name = name;
            Description = description;
            Parent = parent;
            MarkerType = markerType;
        }

        /// <summary>
        /// The group name derived from the marker class name (lowercased).
        /// Example: "MathOperations" → "mathoperations"
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Human-readable description for help display.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Parent group, or null for top-level groups.
        /// </summary>
        public GroupInfo Parent { get; }

        /// <summary>
        /// The marker class type decorated with [Group].
        /// </summary>
        public Type MarkerType { get; }

        /// <summary>
        /// Direct child groups nested within this group.
        /// </summary>
        public IReadOnlyList<GroupInfo> ChildGroups => _childGroups.AsReadOnly();

        /// <summary>
        /// Commands directly contained in this group.
        /// </summary>
        public IReadOnlyList<CommandInfo> Commands => _commands.AsReadOnly();

        /// <summary>
        /// The full hierarchical path (space-separated, matches invocation syntax).
        /// Example: "math advanced"
        /// </summary>
        public string FullPath => Parent == null
            ? Name
            : $"{Parent.FullPath} {Name}";

        /// <summary>
        /// Nesting depth (0 for top-level groups).
        /// </summary>
        public int Depth => Parent == null ? 0 : Parent.Depth + 1;

        /// <summary>
        /// Adds a child group to this group.
        /// </summary>
        /// <param name="childGroup">The child group to add.</param>
        public void AddChildGroup(GroupInfo childGroup)
        {
            _childGroups.Add(childGroup);
        }

        /// <summary>
        /// Adds a command to this group.
        /// </summary>
        /// <param name="command">The command to add.</param>
        public void AddCommand(CommandInfo command)
        {
            _commands.Add(command);
        }
    }
}
