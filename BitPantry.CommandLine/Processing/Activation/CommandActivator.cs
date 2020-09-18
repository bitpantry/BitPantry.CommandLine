using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.Parsing.Strings;
using System;

namespace BitPantry.CommandLine.Processing.Activation
{
    public class CommandActivator : IDisposable
    {
        private IContainer _container;

        /// <summary>
        /// Instantiates a new CommandActivator with a default command dependency container (transient scoped)
        /// </summary>
        public CommandActivator() : this(new SystemActivatorContainer()) { }

        /// <summary>
        /// Instantiates a new CommandActivator with a provided command dependency container
        /// </summary>
        /// <param name="container">The container to use for injecting commands</param>
        internal CommandActivator(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Activates the resolved command
        /// </summary>
        /// <param name="resCmd">The resolved command</param>
        /// <returns>An activation result</returns>
        public ActivationResult Activate(ResolvedCommand resCmd)
        {
            // check for errors

            if (!resCmd.IsValid)
                throw new CommandActivationException(resCmd, "Cannot activate command with resolution errors");

            // create command instance

            var cmd = _container.CreateScope().Get(resCmd.CommandInfo.Type);

            // inject property values

            foreach (var info in resCmd.CommandInfo.Arguments)
            {
                if (info.PropertyInfo.PropertyType == typeof(Option))
                    info.PropertyInfo.SetValue(cmd, new Option(resCmd.InputMap.ContainsKey(info)));
                else if(resCmd.InputMap.ContainsKey(info))
                    info.PropertyInfo.SetValue(cmd, StringParsing.Parse(info.DataType, resCmd.InputMap[info].IsPairedWith?.Value));
            }

            // return result

            return new ActivationResult(cmd, resCmd);
        }

        public void Dispose()
        {
            if (_container != null)
                _container.Dispose();
        }
    }
}
