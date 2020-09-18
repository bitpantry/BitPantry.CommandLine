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

            var duplicateCmd = _commands.Where(c => c.Name.Equals(info.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (duplicateCmd != null)
                throw new ArgumentException($"Cannot register command type {type.FullName} because a command with the same name is already registered :: {duplicateCmd.Type.FullName}");

            _commands.Add(info);
        }
    }
}
