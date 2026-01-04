using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine
{
    public class CommandRegistry
    {
        private bool _areServicesConfigured = false;
        private List<CommandInfo> _commands = new List<CommandInfo>();
        private List<GroupInfo> _groups = new List<GroupInfo>();

        /// <summary>
        /// The collection of CommandInfos registered with this CommandRegistry
        /// </summary>
        public IReadOnlyCollection<CommandInfo> Commands => _commands.AsReadOnly();

        /// <summary>
        /// The collection of GroupInfos registered with this CommandRegistry
        /// </summary>
        public IReadOnlyList<GroupInfo> Groups => _groups.AsReadOnly();

        /// <summary>
        /// Returns only the root-level groups (groups without a parent)
        /// </summary>
        public IReadOnlyList<GroupInfo> RootGroups => _groups.Where(g => g.Parent == null).ToList().AsReadOnly();

        /// <summary>
        /// Returns only the root-level commands (commands not in a group)
        /// </summary>
        public IReadOnlyList<CommandInfo> RootCommands => _commands.Where(c => c.Group == null).ToList().AsReadOnly();

        /// <summary>
        /// Registers a group with this CommandRegistry using a generic type parameter.
        /// </summary>
        /// <typeparam name="T">The marker type decorated with [Group]</typeparam>
        /// <exception cref="ArgumentException">Thrown when the type is not decorated with [Group] attribute</exception>
        public void RegisterGroup<T>() => RegisterGroup(typeof(T));

        /// <summary>
        /// Registers a group with this CommandRegistry, establishing parent-child relationships for nested groups.
        /// </summary>
        /// <param name="groupType">The marker type decorated with [Group]</param>
        /// <exception cref="ArgumentException">Thrown when the type is not decorated with [Group] attribute</exception>
        public void RegisterGroup(Type groupType)
        {
            if (_areServicesConfigured)
                throw new InvalidOperationException("Services have already been configured for this registry. Add groups before configuring services");

            // Check if already registered
            if (_groups.Any(g => g.MarkerType == groupType))
                return;

            var groupAttr = groupType.GetCustomAttributes(typeof(GroupAttribute), false)
                .FirstOrDefault() as GroupAttribute;

            if (groupAttr == null)
                throw new ArgumentException($"Type {groupType.FullName} is not decorated with [Group] attribute", nameof(groupType));

            // Check for parent group (nested class)
            GroupInfo parentGroup = null;
            if (groupType.DeclaringType != null)
            {
                var parentGroupAttr = groupType.DeclaringType.GetCustomAttributes(typeof(GroupAttribute), false)
                    .FirstOrDefault() as GroupAttribute;
                
                if (parentGroupAttr != null)
                {
                    // Parent is also a group - register it first (recursive)
                    RegisterGroup(groupType.DeclaringType);
                    parentGroup = _groups.FirstOrDefault(g => g.MarkerType == groupType.DeclaringType);
                }
            }

            var name = !string.IsNullOrEmpty(groupAttr.Name)
                ? groupAttr.Name
                : groupType.Name.ToLowerInvariant().Replace("group", "");

            // Get description from [Description] attribute on the class
            var descAttr = groupType.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;
            var description = descAttr?.Description;

            var groupInfo = new GroupInfo(name, description, parentGroup, groupType);
            _groups.Add(groupInfo);

            // If has parent, add to parent's child groups
            if (parentGroup != null)
            {
                parentGroup.AddChildGroup(groupInfo);
            }
        }

        /// <summary>
        /// Finds a group by name or full path (case-insensitive).
        /// Supports both simple names ("math") and full paths ("files io").
        /// </summary>
        /// <param name="nameOrPath">The name or full path of the group</param>
        /// <returns>The GroupInfo, or null if not found</returns>
        public GroupInfo FindGroup(string nameOrPath)
        {
            // First try to find by full path
            var byFullPath = _groups.FirstOrDefault(g => g.FullPath.Equals(nameOrPath, NameComparison));
            if (byFullPath != null) return byFullPath;

            // Fall back to simple name match (only for root groups for backward compat)
            return _groups.FirstOrDefault(g => g.Parent == null && g.Name.Equals(nameOrPath, NameComparison));
        }

        /// <summary>
        /// Registers a command with this CommandRegistry
        /// </summary>
        /// <typeparam name="T">The type of the command to register</typeparam>
        public void RegisterCommand<T>() where T : CommandBase
        {
            RegisterCommand(typeof(T));
        }

        /// <summary>
        /// If true, the registration of a duplicate command will replace the existing command. If false (default), an attempt to register a duplicate command
        /// will throw an exception. Duplicate commands are commands that are identified by the same group and command name.
        /// </summary>
        public bool ReplaceDuplicateCommands { get; set; } = false;

        /// <summary>
        /// If true, command and group names are matched case-sensitively.
        /// If false (default), matching is case-insensitive (e.g., "Math Add" matches "math add").
        /// </summary>
        public bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// Gets the StringComparison type to use based on CaseSensitive setting.
        /// </summary>
        private StringComparison NameComparison => CaseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Registers a command with this CommandRegistry
        /// </summary>
        /// <param name="type">The tyep of command to register</param>
        /// <exception cref="ArgumentException">Thrown when command with duplicate namespace and name is already registered with this CommandRegistry</exception>
        public void RegisterCommand(Type type)
        {
            if (_areServicesConfigured)
                throw new InvalidOperationException("Services have already been configured for this registry. Add commands before configuring services");

            var info = CommandReflection.Describe(type);
            
            // Link command to its group if specified
            var cmdAttr = type.GetCustomAttributes(typeof(CommandAttribute), false)
                .FirstOrDefault() as CommandAttribute;
            if (cmdAttr?.Group != null)
            {
                var group = _groups.FirstOrDefault(g => g.MarkerType == cmdAttr.Group);
                if (group != null)
                {
                    info.Group = group;
                    group.AddCommand(info);
                }
                else
                {
                    // Auto-register the group if not already registered
                    RegisterGroup(cmdAttr.Group);
                    group = _groups.First(g => g.MarkerType == cmdAttr.Group);
                    info.Group = group;
                    group.AddCommand(info);
                }
            }
            
            HandleDuplicateCommands(info);
            _commands.Add(info);
        }

        /// <summary>
        /// Registers the command as a remote command - the command info is marked as remote and added to the command collection (but not added to the service collection since the type doesn't exist)
        /// </summary>
        /// <param name="infos">The command infos to register</param>
        public void RegisterCommandsAsRemote(List<CommandInfo> infos)
        {
            foreach (var info in infos)
            {
                info.IsRemote = true;
                
                // If the command has a serialized group path, we need to restore the Group property
                // by finding or creating the appropriate GroupInfo hierarchy
                if (!string.IsNullOrEmpty(info.SerializedGroupPath))
                {
                    info.Group = EnsureRemoteGroupHierarchy(info.SerializedGroupPath);
                    info.Group.AddCommand(info);
                }
                
                HandleDuplicateCommands(info);
                _commands.Add(info);
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
                var existingGroup = _groups.FirstOrDefault(g => 
                    g.Name.Equals(part, StringComparison.OrdinalIgnoreCase) && 
                    g.Parent == parent);
                    
                if (existingGroup != null)
                {
                    parent = existingGroup;
                }
                else
                {
                    // Create a new remote group (no description - remote groups don't have descriptions yet)
                    var newGroup = new GroupInfo(part, null, parent, null);
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

        /// <summary>
        /// Drops all remote commands from the registry
        /// </summary>
        public void DropRemoteCommands()
        {
            var remoteCommands = _commands.Where(c => c.IsRemote).ToList();
            foreach (var item in remoteCommands)
                _commands.Remove(item);
        }

        private void HandleDuplicateCommands(CommandInfo info)
        {
            var duplicateCmd = FindCommand(info.Name, info.Group);
            if (ReplaceDuplicateCommands)
            {
                _commands.Remove(duplicateCmd);
            }
            else
            {
                if (duplicateCmd != null)
                    throw new ArgumentException($"Cannot register command type {info.Type.FullName} because a command with the same name is already registered :: {duplicateCmd.Type.FullName}");
            }
        }

        /// <summary>
        /// Returns the CommandInfo specified by the name and optional group
        /// </summary>
        /// <param name="name">The name of the command</param>
        /// <param name="group">The group the command belongs to, or null for root-level commands</param>
        /// <returns>The CommandInfo specified by the name and group, or null if the CommandInfo could not be resolved</returns>
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

        /// <summary>
        /// Returns the CommandInfo specified by the fully qualified command name (space-separated)
        /// or by simple name for root-level commands or when only one command matches.
        /// </summary>
        /// <param name="fullyQualifiedCommandName">The fully qualified command name (e.g., "group subgroup commandName") or simple command name</param>
        /// <returns>The CommandInfo specified by the fullyQualifiedCommandName, or null if the CommandInfo could not be resolved</returns>
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

        /// <summary>
        /// Validates the registry configuration and throws if there are errors.
        /// Called automatically during ConfigureServices.
        /// </summary>
        public void Validate()
        {
            var errors = new List<string>();

            // FR-022: Empty group validation (no commands AND no subgroups)
            foreach (var group in _groups)
            {
                if (group.Commands.Count == 0 && group.ChildGroups.Count == 0)
                {
                    errors.Add($"Group '{group.FullPath}' is empty - it has no commands and no subgroups.");
                }
            }

            // Name collision detection: command and group with same name at same level
            foreach (var group in RootGroups)
            {
                foreach (var cmd in RootCommands)
                {
                    if (group.Name.Equals(cmd.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Name collision: root-level group '{group.Name}' conflicts with root-level command '{cmd.Name}'.");
                    }
                }
            }

            // Check within each group for command/subgroup name collisions
            foreach (var group in _groups)
            {
                foreach (var childGroup in group.ChildGroups)
                {
                    foreach (var cmd in group.Commands)
                    {
                        if (childGroup.Name.Equals(cmd.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add($"Name collision in group '{group.FullPath}': subgroup '{childGroup.Name}' conflicts with command '{cmd.Name}'.");
                        }
                    }
                }
            }

            // FR-027: Reserved name validation - arguments named 'help' or with alias 'h'
            foreach (var cmd in _commands)
            {
                foreach (var arg in cmd.Arguments)
                {
                    if (arg.Name.Equals("help", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Reserved name: command '{cmd.Name}' has argument named 'help'. This is reserved for the help system.");
                    }
                    if (arg.Alias == 'h' || arg.Alias == 'H')
                    {
                        errors.Add($"Reserved alias: command '{cmd.Name}' argument '{arg.Name}' uses alias 'h'. This is reserved for help.");
                    }
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(
                    $"Command registry validation failed:\n{string.Join("\n", errors.Select(e => $"  - {e}"))}");
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Validate before configuring
            Validate();

            foreach (var cmd in _commands)
            {
                if (!cmd.IsRemote)
                    services.AddTransient(cmd.Type);
            }

            _areServicesConfigured = true;
        }
    }
}
