using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Provides autocomplete options for profile names.
/// Used with the [AutoComplete] attribute on profile name arguments.
/// </summary>
public class ProfileNameProvider : IAutoCompleteHandler
{
    private readonly IProfileManager _profileManager;

    public ProfileNameProvider(IProfileManager profileManager)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
    }

    /// <summary>
    /// Gets autocomplete options for profile names.
    /// Filters profiles by prefix and marks the default profile with "(default)" indicator.
    /// </summary>
    public async Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var profiles = await _profileManager.GetAllProfilesAsync(cancellationToken);
        var defaultName = await _profileManager.GetDefaultProfileNameAsync(cancellationToken);
        var query = context.QueryString ?? string.Empty;

        return profiles
            .Where(p => p.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(p => new AutoCompleteOption(
                p.Name,
                p.Name.Equals(defaultName, StringComparison.OrdinalIgnoreCase)
                    ? "{0} (default)"
                    : null))
            .ToList();
    }
}
