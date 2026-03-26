using BitPantry.CommandLine.Component;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine
{
    /// <summary>
    /// Immutable command registry used at runtime for command resolution.
    /// Local commands are frozen at build time. Remote commands can be 
    /// added/removed dynamically when connecting to/disconnecting from servers.
    /// </summary>
    public class CommandRegistry : ICommandRegistry
    {
        private readonly List<CommandInfo> _localCommands;
        private readonly List<GroupInfo> _groups;
        private readonly List<CommandInfo> _remoteCommands = new List<CommandInfo>();

        /// <summary>
        /// Creates an immutable command registry from the builder's collections.
        /// </summary>
        /// <param name="commands">The list of registered commands</param>
        /// <param name="groups">The list of registered groups</param>
        internal CommandRegistry(List<CommandInfo> commands, List<GroupInfo> groups)
        {
            _localCommands = new List<CommandInfo>(commands);
            _groups = new List<GroupInfo>(groups);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<CommandInfo> Commands => _localCommands.Concat(_remoteCommands).ToList().AsReadOnly();

        /// <inheritdoc/>
        public IReadOnlyList<GroupInfo> Groups => _groups.AsReadOnly();

        /// <inheritdoc/>
        public IReadOnlyList<GroupInfo> RootGroups => Groups.Where(g => g.Parent == null).ToList().AsReadOnly();

        /// <inheritdoc/>
        public IReadOnlyList<CommandInfo> RootCommands => Commands.Where(c => c.Group == null).ToList().AsReadOnly();

        /// <inheritdoc/>
        public GroupInfo FindGroup(string nameOrPath)
        {
            // First try to find by full path
            var byFullPath = Groups.FirstOrDefault(g => g.FullPath.Equals(nameOrPath, StringComparison.Ordinal));
            if (byFullPath != null) return byFullPath;

            // Fall back to simple name match (only for root groups for backward compat)
            return Groups.FirstOrDefault(g => g.Parent == null && g.Name.Equals(nameOrPath, StringComparison.Ordinal));
        }

        /// <inheritdoc/>
        public CommandInfo FindCommand(string name, GroupInfo group = null)
        {
            var cmdInfoQuery = Commands.Where(c =>
                c.Name.Equals(name, StringComparison.Ordinal));

            if (group != null)
                // Use direct object graph navigation - reference equality on the GroupInfo object
                // This works correctly for both local groups (with MarkerType) and remote groups (MarkerType = null)
                cmdInfoQuery = cmdInfoQuery.Where(c => c.Group == group);
            else
                cmdInfoQuery = cmdInfoQuery.Where(c => c.Group == null);

            return cmdInfoQuery.FirstOrDefault();
        }

        /// <inheritdoc/>
        public CommandInfo Find(string fullyQualifiedCommandName)
        {
            // First, try exact match on fully qualified name
            var exactMatch = Commands.FirstOrDefault(c =>
                c.FullyQualifiedName.Equals(fullyQualifiedCommandName, StringComparison.Ordinal));

            if (exactMatch != null)
                return exactMatch;

            // If input has no spaces, try to find by simple name
            if (!fullyQualifiedCommandName.Contains(' '))
            {
                // Find commands that match by name
                var matches = Commands.Where(c =>
                    c.Name.Equals(fullyQualifiedCommandName, StringComparison.Ordinal)).ToList();

                // If exactly one match, return it
                if (matches.Count == 1)
                    return matches[0];

                // If multiple matches, prefer root-level command (no group)
                var rootMatch = matches.FirstOrDefault(c => c.Group == null);
                if (rootMatch != null)
                    return rootMatch;
            }

            return null;
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> RegisterCommandsAsRemote(IReadOnlyList<CommandInfo> infos)
        {
            var skipped = new List<string>();

            foreach (var info in infos)
            {
                info.IsRemote = true;
                
                // If the command has a serialized group path, we need to restore the Group property
                // by finding or creating the appropriate GroupInfo hierarchy
                if (!string.IsNullOrEmpty(info.SerializedGroupPath))
                {
                    info.Group = EnsureRemoteGroupHierarchy(info.SerializedGroupPath);
                }
                
                // Check for existing command with the same name in the same group
                var duplicate = FindCommand(info.Name, info.Group);
                if (duplicate != null)
                {
                    if (!duplicate.IsRemote)
                    {
                        // Local command takes precedence — skip this remote command
                        skipped.Add(info.FullyQualifiedName);
                        continue;
                    }
                    
                    // Replace existing remote command
                    _remoteCommands.Remove(duplicate);
                }
                
                _remoteCommands.Add(info);
                info.Group?.AddCommand(info);
            }

            return skipped.AsReadOnly();
        }

        /// <summary>
        /// Ensures a remote group hierarchy exists for the given path and returns the deepest group.
        /// Creates groups as needed (marked as remote).
        /// </summary>
        /// <param name="groupPath">Space-separated group path (e.g., "math advanced")</param>
        /// <returns>The GroupInfo for the deepest level of the path</returns>
        private GroupInfo EnsureRemoteGroupHierarchy(string groupPath)
        {
            var parts = groupPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            GroupInfo parent = null;
            
            foreach (var part in parts)
            {
                // Check local groups first, then remote groups
                var existingGroup = _groups.FirstOrDefault(g => 
                    g.Name.Equals(part, StringComparison.Ordinal) && 
                    g.Parent == parent);
                    
                if (existingGroup != null)
                {
                    parent = existingGroup;
                }
                else
                {
                    // Create a new remote group
                    var newGroup = new GroupInfo(part, $"Remote group: {part}", parent, null);
                    _groups.Add(newGroup);
                    
                    if (parent != null)
                    {
                        parent.AddChildGroup(newGroup);
                    }
                    
                    parent = newGroup;
                }
            }
            
            return parent;
        }

        /// <inheritdoc/>
        public void DropRemoteCommands()
        {
            // Remove each remote command from its group
            foreach (var cmd in _remoteCommands)
            {
                cmd.Group?.RemoveCommand(cmd);
            }
            _remoteCommands.Clear();

            // Drop empty groups (no commands and no child groups)
            // Iterate in reverse to handle nested empties bottom-up
            for (int i = _groups.Count - 1; i >= 0; i--)
            {
                var group = _groups[i];
                if (group.Commands.Count == 0 && group.ChildGroups.Count == 0)
                {
                    group.Parent?.RemoveChildGroup(group);
                    _groups.RemoveAt(i);
                }
            }
        }
    }
}
