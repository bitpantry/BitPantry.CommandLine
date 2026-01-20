using BitPantry.CommandLine.Component;
using System.Collections.Generic;

namespace BitPantry.CommandLine
{
    /// <summary>
    /// Immutable registry used at runtime for command resolution.
    /// Local commands are frozen at build time. Remote commands can be 
    /// added/removed dynamically when connecting to/disconnecting from servers.
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// All registered commands (local + remote).
        /// </summary>
        IReadOnlyCollection<CommandInfo> Commands { get; }

        /// <summary>
        /// All registered groups.
        /// </summary>
        IReadOnlyList<GroupInfo> Groups { get; }

        /// <summary>
        /// Commands at root level (no parent group).
        /// </summary>
        IReadOnlyList<CommandInfo> RootCommands { get; }

        /// <summary>
        /// Groups at root level (no parent group).
        /// </summary>
        IReadOnlyList<GroupInfo> RootGroups { get; }

        /// <summary>
        /// If true, command and group names are matched case-sensitively.
        /// If false (default), matching is case-insensitive.
        /// </summary>
        bool CaseSensitive { get; }

        /// <summary>
        /// Finds a group by name or full path (case-insensitive).
        /// </summary>
        /// <param name="nameOrPath">The name or full path of the group</param>
        /// <returns>The GroupInfo, or null if not found</returns>
        GroupInfo FindGroup(string nameOrPath);

        /// <summary>
        /// Finds a command by name within an optional group.
        /// </summary>
        /// <param name="name">The name of the command</param>
        /// <param name="group">The group the command belongs to, or null for root-level commands</param>
        /// <returns>The CommandInfo, or null if not found</returns>
        CommandInfo FindCommand(string name, GroupInfo group = null);

        /// <summary>
        /// Finds a command by fully qualified name (space-separated path).
        /// </summary>
        /// <param name="fullyQualifiedCommandName">The fully qualified command name</param>
        /// <returns>The CommandInfo, or null if not found</returns>
        CommandInfo Find(string fullyQualifiedCommandName);

        /// <summary>
        /// Registers commands received from a remote server.
        /// These are marked as remote and can be dropped on disconnect.
        /// </summary>
        /// <param name="infos">The command infos from the remote server</param>
        void RegisterCommandsAsRemote(IReadOnlyList<CommandInfo> infos);

        /// <summary>
        /// Drops all remote commands from the registry.
        /// Called when disconnecting from a remote server.
        /// </summary>
        void DropRemoteCommands();
    }
}
