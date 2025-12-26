using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete
{
    /// <summary>
    /// Provides completion suggestions for profile names based on saved server profiles.
    /// </summary>
    /// <remarks>
    /// This provider offers autocomplete for profile-related commands like:
    /// - server profile show [profile-name]
    /// - server profile remove [profile-name]
    /// - server profile set-default [profile-name]
    /// - server connect --profile [profile-name]
    /// </remarks>
    public class ProfileNameProvider : ICompletionProvider
    {
        private readonly IProfileManager _profileManager;

        /// <inheritdoc />
        /// <remarks>
        /// Priority 80 - higher than static values to take precedence for profile arguments.
        /// </remarks>
        public int Priority => 80;

        public ProfileNameProvider(IProfileManager profileManager)
        {
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
        }

        /// <inheritdoc />
        public bool CanHandle(CompletionContext context)
        {
            // Only handle argument values
            if (context.ElementType != CompletionElementType.ArgumentValue)
                return false;

            // Check if the completion attribute specifies this provider
            if (context.CompletionAttribute?.ProviderType != typeof(ProfileNameProvider))
                return false;

            return true;
        }

        /// <inheritdoc />
        public async Task<CompletionResult> GetCompletionsAsync(
            CompletionContext context,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return CompletionResult.Empty;

            var profiles = await _profileManager.GetAllProfilesAsync();
            if (!profiles.Any())
                return CompletionResult.Empty;

            var prefix = context.CurrentWord ?? string.Empty;
            var items = new List<CompletionItem>();
            var defaultProfile = await _profileManager.GetDefaultProfileAsync();

            foreach (var profile in profiles)
            {
                // Filter by prefix (case-insensitive)
                if (!string.IsNullOrEmpty(prefix) &&
                    !profile.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var isDefault = string.Equals(profile.Name, defaultProfile, StringComparison.OrdinalIgnoreCase);
                var description = isDefault ? $"{profile.Uri} (default)" : profile.Uri;

                items.Add(new CompletionItem
                {
                    DisplayText = profile.Name,
                    InsertText = profile.Name,
                    Description = description,
                    Kind = CompletionItemKind.ArgumentValue,
                    SortPriority = isDefault ? -1 : 0 // Default profile first
                });
            }

            // Sort by priority, then alphabetically
            items = items
                .OrderBy(i => i.SortPriority)
                .ThenBy(i => i.DisplayText)
                .ToList();

            return new CompletionResult(items);
        }
    }
}
