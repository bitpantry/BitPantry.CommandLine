using BitPantry.CommandLine.Component;
using System;
using System.Linq;

namespace BitPantry.CommandLine.Input;

/// <summary>
/// Resolves partial token text against the command registry to determine match type.
/// </summary>
public class TokenMatchResolver
{
    private readonly ICommandRegistry _registry;

    /// <summary>
    /// Creates a new TokenMatchResolver.
    /// </summary>
    /// <param name="registry">The command registry to resolve against.</param>
    public TokenMatchResolver(ICommandRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Resolves a token against the registry to determine its match type.
    /// </summary>
    /// <param name="token">The token text to match.</param>
    /// <param name="context">The parent group context, or null for root-level resolution.</param>
    /// <returns>The match result indicating unique group, unique command, ambiguous, or no match.</returns>
    public TokenMatchResult ResolveMatch(string token, GroupInfo context)
    {
        if (string.IsNullOrEmpty(token))
            return TokenMatchResult.NoMatch;

        var comparison = _registry.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        // Get groups and commands to search based on context
        var groups = context?.ChildGroups ?? _registry.RootGroups;
        var commands = context?.Commands ?? _registry.RootCommands;

        // Check for exact group match
        var exactGroupMatch = groups.FirstOrDefault(g =>
            string.Equals(g.Name, token, comparison));

        if (exactGroupMatch != null)
            return TokenMatchResult.UniqueGroup;

        // Check for exact command match
        var exactCommandMatch = commands.FirstOrDefault(c =>
            string.Equals(c.Name, token, comparison));

        if (exactCommandMatch != null)
            return TokenMatchResult.UniqueCommand;

        // Check for partial prefix matches
        var partialGroupMatches = groups.Where(g =>
            g.Name.StartsWith(token, comparison)).ToList();

        var partialCommandMatches = commands.Where(c =>
            c.Name.StartsWith(token, comparison)).ToList();

        var totalMatches = partialGroupMatches.Count + partialCommandMatches.Count;

        // If multiple matches of any kind, it's ambiguous
        if (totalMatches > 1)
            return TokenMatchResult.Ambiguous;

        // If exactly one group matches and no commands match, it's a unique partial group
        if (partialGroupMatches.Count == 1 && partialCommandMatches.Count == 0)
            return TokenMatchResult.UniqueGroup;

        return TokenMatchResult.NoMatch;
    }
}
