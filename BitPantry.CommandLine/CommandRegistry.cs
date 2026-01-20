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
        private readonly List<GroupInfo> _localGroups;
        private readonly List<CommandInfo> _remoteCommands = new List<CommandInfo>();
        private readonly List<GroupInfo> _remoteGroups = new List<GroupInfo>();

        /// <inheritdoc/>
        public bool CaseSensitive { get; }

        /// <summary>
        /// Gets the StringComparison type to use based on CaseSensitive setting.
        /// </summary>
        private StringComparison NameComparison => CaseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Creates an immutable command registry from the builder's collections.
        /// </summary>
        /// <param name="commands">The list of registered commands</param>
        /// <param name="groups">The list of registered groups</param>
        /// <param name="caseSensitive">Whether name matching is case-sensitive</param>
        internal CommandRegistry(List<CommandInfo> commands, List<GroupInfo> groups, bool caseSensitive)
        {
            _localCommands = new List<CommandInfo>(commands);
            _localGroups = new List<GroupInfo>(groups);
            CaseSensitive = caseSensitive;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<CommandInfo> Commands => _localCommands.Concat(_remoteCommands).ToList().AsReadOnly();

        /// <inheritdoc/>
        public IReadOnlyList<GroupInfo> Groups => _localGroups.Concat(_remoteGroups).ToList().AsReadOnly();

        /// <inheritdoc/>
        public IReadOnlyList<GroupInfo> RootGroups => Groups.Where(g => g.Parent == null).ToList().AsReadOnly();

        /// <inheritdoc/>
        public IReadOnlyList<CommandInfo> RootCommands => Commands.Where(c => c.Group == null).ToList().AsReadOnly();

        /// <inheritdoc/>
        public GroupInfo FindGroup(string nameOrPath)
        {
            // First try to find by full path
            var byFullPath = Groups.FirstOrDefault(g => g.FullPath.Equals(nameOrPath, NameComparison));
            if (byFullPath != null) return byFullPath;

            // Fall back to simple name match (only for root groups for backward compat)
            return Groups.FirstOrDefault(g => g.Parent == null && g.Name.Equals(nameOrPath, NameComparison));
        }

        /// <inheritdoc/>
        public CommandInfo FindCommand(string name, GroupInfo group = null)
        {
            var cmdInfoQuery = Commands.Where(c =>
                c.Name.Equals(name, NameComparison));

            if (group != null)
                cmdInfoQuery = cmdInfoQuery.Where(c =>
                    c.Group != null && c.Group.MarkerType == group.MarkerType);
            else
                cmdInfoQuery = cmdInfoQuery.Where(c => c.Group == null);

            return cmdInfoQuery.FirstOrDefault();
        }

        /// <inheritdoc/>
        public CommandInfo Find(string fullyQualifiedCommandName)
        {
            // First, try exact match on fully qualified name
            var exactMatch = Commands.FirstOrDefault(c =>
                c.FullyQualifiedName.Equals(fullyQualifiedCommandName, NameComparison));

            if (exactMatch != null)
                return exactMatch;

            // If input has no spaces, try to find by simple name
            if (!fullyQualifiedCommandName.Contains(' '))
            {
                // Find commands that match by name
                var matches = Commands.Where(c =>
                    c.Name.Equals(fullyQualifiedCommandName, NameComparison)).ToList();

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
        public void RegisterCommandsAsRemote(IReadOnlyList<CommandInfo> infos)
        {
            foreach (var info in infos)
            {
                info.IsRemote = true;
                
                // If the command has a serialized group path, we need to restore the Group property
                // by finding or creating the appropriate GroupInfo hierarchy
                if (!string.IsNullOrEmpty(info.SerializedGroupPath))
                {
                    info.Group = EnsureRemoteGroupHierarchy(info.SerializedGroupPath);
                }
                
                // Remove any existing command with the same name in the same group
                var duplicate = FindCommand(info.Name, info.Group);
                if (duplicate != null)
                    _remoteCommands.Remove(duplicate);
                
                _remoteCommands.Add(info);
            }
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
                var existingGroup = _localGroups.Concat(_remoteGroups).FirstOrDefault(g => 
                    g.Name.Equals(part, NameComparison) && 
                    g.Parent == parent);
                    
                if (existingGroup != null)
                {
                    parent = existingGroup;
                }
                else
                {
                    // Create a new remote group
                    var newGroup = new GroupInfo(part, $"Remote group: {part}", parent, null);
                    _remoteGroups.Add(newGroup);
                    
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
            _remoteCommands.Clear();
            _remoteGroups.Clear();
        }
    }
}
