using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Component;
using Spectre.Console;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Input;

/// <summary>
/// Provides syntax highlighting for command line input by analyzing tokens
/// against the command registry.
/// </summary>
public class SyntaxHighlighter
{
    private readonly ICommandRegistry _registry;
    private readonly TokenMatchResolver _resolver;
    private readonly Theme _theme;

    /// <summary>
    /// Creates a new SyntaxHighlighter.
    /// </summary>
    /// <param name="registry">The command registry to resolve tokens against.</param>
    /// <param name="theme">The theme providing styles for highlighted segments.</param>
    public SyntaxHighlighter(ICommandRegistry registry, Theme theme = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _resolver = new TokenMatchResolver(registry);
        _theme = theme ?? new Theme();
    }

    /// <summary>
    /// Highlights the input string by analyzing tokens and assigning styles.
    /// </summary>
    /// <param name="input">The command line input to highlight.</param>
    /// <returns>A list of styled segments representing the highlighted input.</returns>
    public IReadOnlyList<StyledSegment> Highlight(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<StyledSegment>();

        var segments = new List<StyledSegment>();
        var tokens = Tokenize(input);
        GroupInfo currentGroup = null;
        bool commandSeen = false;

        foreach (var token in tokens)
        {
            // Whitespace tokens get default style
            if (token.IsWhitespace)
            {
                segments.Add(new StyledSegment(token.Text, token.Start, token.End, _theme.Default));
                continue;
            }

            Style style;

            // After a command, tokens are arguments/values
            if (commandSeen)
            {
                style = GetArgumentStyle(token.Text);
            }
            else
            {
                var matchResult = _resolver.ResolveMatch(token.Text, currentGroup);
                style = GetStyleForMatchResult(matchResult);

                // Update context if this was a group match
                if (matchResult == TokenMatchResult.UniqueGroup)
                {
                    currentGroup = FindGroup(token.Text, currentGroup);
                }
                else if (matchResult == TokenMatchResult.UniqueCommand)
                {
                    commandSeen = true;
                }
            }

            segments.Add(new StyledSegment(token.Text, token.Start, token.End, style));
        }

        return segments;
    }

    private Style GetArgumentStyle(string text)
    {
        if (text.StartsWith("--"))
            return _theme.ArgumentName;
        if (text.StartsWith("-"))
            return _theme.ArgumentAlias;
        return _theme.ArgumentValue;
    }

    private Style GetStyleForMatchResult(TokenMatchResult result)
    {
        return result switch
        {
            TokenMatchResult.UniqueGroup => _theme.Group,
            TokenMatchResult.UniqueCommand => _theme.Command,
            _ => _theme.Default
        };
    }

    private GroupInfo FindGroup(string name, GroupInfo context)
    {
        var groups = context?.ChildGroups ?? _registry.RootGroups;
        var comparison = _registry.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        foreach (var group in groups)
        {
            if (string.Equals(group.Name, name, comparison) ||
                group.Name.StartsWith(name, comparison))
            {
                return group;
            }
        }

        return context;
    }

    private static List<TokenInfo> Tokenize(string input)
    {
        var tokens = new List<TokenInfo>();
        var currentStart = -1;
        var isWhitespace = false;
        var inQuote = false;
        char quoteChar = '\0';
        
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];

            // Handle quoted strings - treat as a single token
            if ((c == '"' || c == '\'') && !inQuote)
            {
                // Close any existing token before starting quoted string
                if (currentStart >= 0)
                {
                    tokens.Add(new TokenInfo(input.Substring(currentStart, i - currentStart), currentStart, i, isWhitespace));
                }
                inQuote = true;
                quoteChar = c;
                currentStart = i;
                isWhitespace = false;
                continue;
            }

            if (inQuote)
            {
                if (c == quoteChar)
                {
                    // End of quoted string (include the closing quote)
                    tokens.Add(new TokenInfo(input.Substring(currentStart, i - currentStart + 1), currentStart, i + 1, false));
                    inQuote = false;
                    currentStart = -1;
                }
                continue;
            }

            var charIsWhitespace = char.IsWhiteSpace(c);
            
            if (currentStart < 0)
            {
                // Starting a new token
                currentStart = i;
                isWhitespace = charIsWhitespace;
            }
            else if (charIsWhitespace != isWhitespace)
            {
                // Transition between whitespace and non-whitespace
                tokens.Add(new TokenInfo(input.Substring(currentStart, i - currentStart), currentStart, i, isWhitespace));
                currentStart = i;
                isWhitespace = charIsWhitespace;
            }
        }

        // Final token (including unclosed quotes)
        if (currentStart >= 0)
        {
            tokens.Add(new TokenInfo(input.Substring(currentStart), currentStart, input.Length, isWhitespace && !inQuote));
        }

        return tokens;
    }

    private record TokenInfo(string Text, int Start, int End, bool IsWhitespace = false);
}
