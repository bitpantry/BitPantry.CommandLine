using BitPantry.CommandLine.API;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Processing.Activation
{
    public class ServiceProviderContainer : IContainer
    {
        private ServiceProvider _provider;

        public ServiceProviderContainer(ServiceProvider provider)
        {
            _provider = provider;
        }

        public IContainerScope CreateScope()
            => new ServiceProviderScope(_provider.CreateScope());

        public void Dispose()
        {
            _provider?.Dispose();
        }


    }
}
