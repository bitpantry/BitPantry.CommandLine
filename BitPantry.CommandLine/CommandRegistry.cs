using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine
{
    public class CommandRegistry
    {
        private List<CommandInfo> _commands = new List<CommandInfo>();

        public IReadOnlyCollection<CommandInfo> Commands => _commands.AsReadOnly();

        public void RegisterCommand<T>() where T : CommandBase
        {
            RegisterCommand(typeof(T));
        }

        public void RegisterCommand(Type type)
        {
            // describe command

            var info = CommandReflection.Describe(type);

            // check for duplicate command

            var duplicateCmd = Find(info.Namespace, info.Name);
            if (duplicateCmd != null)
                throw new ArgumentException($"Cannot register command type {type.FullName} because a command with the same name is already registered :: {duplicateCmd.Type.FullName}");

            _commands.Add(info);
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
    }
}
