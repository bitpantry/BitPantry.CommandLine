using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;

namespace BitPantry.CommandLine.Tests.PluginModule
{
    /// <summary>
    /// A module that registers an autocomplete handler for testing.
    /// </summary>
    public class AutoCompleteTestModule : ICommandModule
    {
        /// <inheritdoc/>
        public string Name => "AutoCompleteTestModule";

        /// <inheritdoc/>
        public void Configure(ICommandModuleContext context)
        {
            // Register an autocomplete handler
            context.AutoComplete.Register<TestDateTimeAutoCompleteHandler>();
        }
    }
}
