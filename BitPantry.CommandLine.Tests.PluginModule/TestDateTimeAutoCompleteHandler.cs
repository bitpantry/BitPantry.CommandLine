using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.PluginModule
{
    /// <summary>
    /// A test autocomplete handler that provides suggestions for DateTime arguments.
    /// Used for testing that modules can register autocomplete handlers.
    /// </summary>
    public class TestDateTimeAutoCompleteHandler : ITypeAutoCompleteHandler
    {
        /// <inheritdoc/>
        public bool CanHandle(Type argumentType)
        {
            return argumentType == typeof(DateTime) || argumentType == typeof(DateTime?);
        }

        /// <inheritdoc/>
        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            var options = new List<AutoCompleteOption>
            {
                new AutoCompleteOption(DateTime.Today.ToString("yyyy-MM-dd"), "{0} (Today)"),
                new AutoCompleteOption(DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"), "{0} (Tomorrow)"),
                new AutoCompleteOption(DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"), "{0} (Yesterday)")
            };

            return Task.FromResult(options);
        }
    }
}
