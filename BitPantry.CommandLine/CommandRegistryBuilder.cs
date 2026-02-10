using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine
{
    /// <summary>
    /// Mutable builder for configuring command registrations before building the application.
    /// Once Build() is called, returns an immutable ICommandRegistry.
    /// </summary>
    public class CommandRegistryBuilder : ICommandRegistryBuilder
    {
        private bool _isBuilt = false;
        private readonly List<CommandInfo> _commands = new List<CommandInfo>();
        private readonly List<GroupInfo> _groups = new List<GroupInfo>();

        /// <inheritdoc/>
        public bool ReplaceDuplicateCommands { get; set; } = false;

        /// <inheritdoc/>
        public bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// Gets the StringComparison type to use based on CaseSensitive setting.
        /// </summary>
        private StringComparison NameComparison => CaseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Registers a group with this CommandRegistry, establishing parent-child relationships for nested groups.
        /// </summary>
        /// <param name="groupType">The marker type decorated with [Group]</param>
        /// <exception cref="ArgumentException">Thrown when the type is not decorated with [Group] attribute</exception>
        internal void RegisterGroup(Type groupType)
        {
            ThrowIfBuilt();

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
        /// Registers a command with this CommandRegistry using a generic type parameter.
        /// </summary>
        /// <typeparam name="T">The type of the command to register</typeparam>
        public void RegisterCommand<T>() where T : CommandBase
        {
            RegisterCommand(typeof(T));
        }

        /// <inheritdoc/>
        public void RegisterCommand(Type type)
        {
            ThrowIfBuilt();

            var info = CommandReflection.Describe(type);
            
            // Determine the group type from [InGroup<T>] attribute
            Type groupType = GetGroupTypeFromAttributes(type);
            
            // Link command to its group if specified
            if (groupType != null)
            {
                var group = _groups.FirstOrDefault(g => g.MarkerType == groupType);
                if (group != null)
                {
                    info.Group = group;
                    group.AddCommand(info);
                }
                else
                {
                    // Auto-register the group if not already registered
                    RegisterGroup(groupType);
                    group = _groups.First(g => g.MarkerType == groupType);
                    info.Group = group;
                    group.AddCommand(info);
                }
            }
            
            HandleDuplicateCommands(info);
            _commands.Add(info);
        }

        /// <summary>
        /// Gets the group type from [InGroup&lt;T&gt;] attribute.
        /// </summary>
        /// <param name="commandType">The command type to inspect</param>
        /// <returns>The group marker type, or null if no group is specified</returns>
        private static Type GetGroupTypeFromAttributes(Type commandType)
        {
            // Check for [InGroup<T>] attribute
            var inGroupAttr = commandType.GetCustomAttributes(false)
                .FirstOrDefault(a => a.GetType().IsGenericType && 
                                     a.GetType().GetGenericTypeDefinition() == typeof(InGroupAttribute<>));
            
            if (inGroupAttr != null)
            {
                // Get the GroupType property from the attribute
                var groupTypeProperty = inGroupAttr.GetType().GetProperty("GroupType");
                return groupTypeProperty?.GetValue(inGroupAttr) as Type;
            }
            
            return null;
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
        /// Returns the CommandInfo specified by the name and optional group (used during build).
        /// </summary>
        private CommandInfo FindCommand(string name, GroupInfo group = null)
        {
            var cmdInfoQuery = _commands.Where(c =>
                c.Name.Equals(name, NameComparison));

            if (group != null)
                cmdInfoQuery = cmdInfoQuery.Where(c =>
                    c.Group != null && c.Group.MarkerType == group.MarkerType);
            else
                cmdInfoQuery = cmdInfoQuery.Where(c => c.Group == null);

            return cmdInfoQuery.FirstOrDefault();
        }

        /// <summary>
        /// Validates the registry configuration and throws if there are errors.
        /// </summary>
        private void Validate()
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
            var rootGroups = _groups.Where(g => g.Parent == null).ToList();
            var rootCommands = _commands.Where(c => c.Group == null).ToList();

            foreach (var group in rootGroups)
            {
                foreach (var cmd in rootCommands)
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

            // FR-027: Reserved name validation
            // Validates global argument reservations (--help/-h, --profile/-P, etc.)
            foreach (var cmd in _commands)
            {
                foreach (var arg in cmd.Arguments)
                {
                    // Global argument reservations
                    foreach (var reservedName in Processing.Parsing.GlobalArguments.ReservedNames)
                    {
                        if (arg.Name.Equals(reservedName, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add($"Reserved name: command '{cmd.Name}' has argument named '{reservedName}'. This is reserved as a global argument.");
                        }
                    }
                    foreach (var reservedAlias in Processing.Parsing.GlobalArguments.ReservedAliases)
                    {
                        if (arg.Alias == reservedAlias)
                        {
                            errors.Add($"Reserved alias: command '{cmd.Name}' argument '{arg.Name}' uses alias '{reservedAlias}'. This is reserved as a global argument.");
                        }
                    }
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(
                    $"Command registry validation failed:\n{string.Join("\n", errors.Select(e => $"  - {e}"))}");
            }
        }

        /// <summary>
        /// Freezes the builder and returns an immutable registry.
        /// Validates the configuration and registers all command types with DI.
        /// </summary>
        /// <param name="services">The service collection to register command types with</param>
        /// <returns>The immutable command registry</returns>
        public ICommandRegistry Build(IServiceCollection services)
        {
            ThrowIfBuilt();
            Validate();
            
            // Register command types with DI during build
            foreach (var cmd in _commands)
            {
                services.AddTransient(cmd.Type);
            }
            
            _isBuilt = true;
            return new CommandRegistry(_commands, _groups, CaseSensitive);
        }

        /// <summary>
        /// Freezes the builder and returns an immutable registry without DI registration.
        /// Use this overload for testing scenarios where DI registration is not needed.
        /// </summary>
        /// <returns>The immutable command registry</returns>
        public ICommandRegistry Build()
        {
            return Build(new ServiceCollection());
        }

        private void ThrowIfBuilt()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Cannot modify the registry after Build() has been called.");
        }
    }
}
