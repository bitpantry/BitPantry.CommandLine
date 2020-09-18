using BitPantry.CommandLine.API;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Processing.Activation
{
    public class ServiceProviderScope : IContainerScope
    {
        private IServiceScope _scope;

        internal ServiceProviderScope(IServiceScope scope)
        {
            _scope = scope;
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }

        public CommandBase Get(Type commandType)
            => (CommandBase) _scope.ServiceProvider.GetRequiredService(commandType);
    }
}
