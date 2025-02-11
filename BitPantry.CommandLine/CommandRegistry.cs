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

        /// <summary>
        /// The collection of CommandInfos registered with this CommandRegistry
        /// </summary>
        public IReadOnlyCollection<CommandInfo> Commands => _commands.AsReadOnly();

        /// <summary>
        /// Registers a command with this CommandRegistry
        /// </summary>
        /// <typeparam name="T">The type of the command to register</typeparam>
        public void RegisterCommand<T>() where T : CommandBase
        {
            RegisterCommand(typeof(T));
        }

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
            ThrowExceptionIfDuplicate(info);
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
                ThrowExceptionIfDuplicate(info);
                _commands.Add(info);
            }
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

        private void ThrowExceptionIfDuplicate(CommandInfo info)
        {
            var duplicateCmd = Find(info.Namespace, info.Name);
            if (duplicateCmd != null)
                throw new ArgumentException($"Cannot register command type {info.Type.FullName} because a command with the same name is already registered :: {duplicateCmd.Type.FullName}");
        }

        /// <summary>
        /// Returns the CommandInfo specified by the namespace and name
        /// </summary>
        /// <param name="namespace">The command namespace (e.g., "namespace1.namespace2")</param>
        /// <param name="name">The name of the command</param>
        /// <returns>The CommandInfo specified by the namespace and name, or null if the CommandInfo could not be resolved</returns>
        public CommandInfo Find(string @namespace, string name)
        {
            // query

            var cmdInfoQuery = Commands.Where(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (@namespace != null)
                cmdInfoQuery = cmdInfoQuery.Where(c =>
                    c.Namespace != null && c.Namespace.Equals(@namespace, StringComparison.OrdinalIgnoreCase));

            // return what was found

            return cmdInfoQuery.FirstOrDefault();
        }

        /// <summary>
        /// Returns the CommandInfo specified by the fully qualified command name
        /// </summary>
        /// <param name="fullyQualifiedCommandName">The fully qualified command name, including namespace (e.g., "namespace1.namespace2.commandName")</param>
        /// <returns>The CommandInfo specified by the fullyQualifiedCommandName, or null if the CommandInfo could not be resolved</returns>
        public CommandInfo Find(string fullyQualifiedCommandName)
        {
            // parse out namespace and command name

            string cmdNamespace = null;
            var cmdName = fullyQualifiedCommandName;

            if (fullyQualifiedCommandName.IndexOf('.') > 0)
            {
                cmdNamespace = cmdName.Substring(0, cmdName.IndexOf('.'));
                cmdName = cmdName.Substring(cmdName.IndexOf('.') + 1);
            }

            // query

            return Find(cmdNamespace, cmdName);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            foreach (var cmd in _commands)
            {
                if (!cmd.IsRemote)
                    services.AddTransient(cmd.Type);
            }

            _areServicesConfigured = true;
        }
    }
}
