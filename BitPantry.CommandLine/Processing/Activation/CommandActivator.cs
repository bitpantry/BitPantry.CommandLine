using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.Parsing.Strings;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BitPantry.CommandLine.Processing.Activation
{
    public class CommandActivator : IDisposable
    {
        private IServiceProvider _svcProvider;

        /// <summary>
        /// Instantiates a new CommandActivator with a provided command dependency container
        /// </summary>
        /// <param name="container">The container to use for injecting commands</param>
        public CommandActivator(IServiceProvider svcProvider)
        {
            _svcProvider = svcProvider;
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

            var cmd = _svcProvider.CreateScope().ServiceProvider.GetRequiredService(resCmd.CommandInfo.Type) as CommandBase;

            // inject property values

            foreach (var info in resCmd.CommandInfo.Arguments)
            {
                if (info.PropertyInfo.GetPropertyInfo().PropertyType == typeof(Option))
                    info.PropertyInfo.SetValue(cmd, new Option(resCmd.InputMap.ContainsKey(info)));
                else if(resCmd.InputMap.ContainsKey(info))
                    info.PropertyInfo.SetValue(cmd, StringParsing.Parse(info.PropertyInfo.GetPropertyInfo().PropertyType, resCmd.InputMap[info].IsPairedWith?.Value));
            }

            // return result

            return new ActivationResult(cmd, resCmd);
        }

        public void Dispose()
        {
            if (_svcProvider != null)
                ((IDisposable)_svcProvider).Dispose(); // assuming the Microsoft provided ServiceProvider continues to implement IDisposable going forward
        }
    }
}
