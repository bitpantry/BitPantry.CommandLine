using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Tracks which arguments have already been used in the current input line.
/// </summary>
/// <remarks>
/// This class parses the input to identify --argName and -a style arguments
/// that have already been specified, allowing completion providers to exclude
/// them from suggestions.
/// </remarks>
public class UsedArgumentTracker
{
    private static readonly Regex LongArgRegex = new(@"--([a-zA-Z][\w-]*)", RegexOptions.Compiled);
    private static readonly Regex ShortArgRegex = new(@"(?<![\\-])-([a-zA-Z])", RegexOptions.Compiled);

    /// <summary>
    /// Extracts the set of argument names already used in the input.
    /// </summary>
    /// <param name="input">The current input line.</param>
    /// <param name="cursorPosition">The cursor position (used to identify current token to exclude).</param>
    /// <returns>A set of argument names (without dashes) that have been used.</returns>
    /// <remarks>
    /// Scans the ENTIRE input line for used arguments, not just text before cursor.
    /// The current token being typed (text from token start to cursor position) is excluded 
    /// from the used set. This ensures arguments appearing anywhere in the input are properly 
    /// excluded from completion suggestions, preventing duplicates.
    /// 
    /// Important: Only the portion of the token that has been typed (before cursor) is 
    /// considered "current". If cursor is at the start of a word, that word is NOT the 
    /// current token - it's an existing token that should be included in used arguments.
    /// </remarks>
    public static HashSet<string> GetUsedArguments(string input, int cursorPosition = -1)
    {
        if (string.IsNullOrEmpty(input))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Determine the effective cursor position
        var effectiveCursor = cursorPosition >= 0 ? Math.Min(cursorPosition, input.Length) : input.Length;

        // Find the start of the current token (the one being typed at cursor)
        // We don't want to count the current token as "used"
        var currentTokenStart = effectiveCursor;
        while (currentTokenStart > 0 && input[currentTokenStart - 1] != ' ')
        {
            currentTokenStart--;
        }

        // The current token being typed is ONLY from tokenStart to cursor position.
        // If cursor is at the start of a word (tokenStart == effectiveCursor), 
        // there's no current token being typed, so nothing to exclude.
        var currentTokenEnd = effectiveCursor;

        var usedArgs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Match --longName arguments from the ENTIRE input
        foreach (Match match in LongArgRegex.Matches(input))
        {
            // Skip if this match overlaps with the current token being typed
            // Only skip if there's actually a token being typed (currentTokenStart < currentTokenEnd)
            if (currentTokenStart < currentTokenEnd && 
                match.Index >= currentTokenStart && match.Index < currentTokenEnd)
                continue;
                
            usedArgs.Add(match.Groups[1].Value);
        }

        // Match -s short arguments from the ENTIRE input
        foreach (Match match in ShortArgRegex.Matches(input))
        {
            // Skip if this match overlaps with the current token being typed
            if (currentTokenStart < currentTokenEnd && 
                match.Index >= currentTokenStart && match.Index < currentTokenEnd)
                continue;
                
            usedArgs.Add(match.Groups[1].Value);
        }

        return usedArgs;
    }

    /// <summary>
    /// Checks if a specific argument name has been used.
    /// </summary>
    /// <param name="input">The current input line.</param>
    /// <param name="argumentName">The argument name to check (without dashes).</param>
    /// <param name="cursorPosition">The cursor position (optional).</param>
    /// <returns>True if the argument has been used, false otherwise.</returns>
    public static bool IsArgumentUsed(string input, string argumentName, int cursorPosition = -1)
    {
        var usedArgs = GetUsedArguments(input, cursorPosition);
        return usedArgs.Contains(argumentName);
    }

    /// <summary>
    /// Detects what type of completion is being requested based on current input.
    /// </summary>
    /// <param name="input">The current input line.</param>
    /// <param name="cursorPosition">The cursor position.</param>
    /// <returns>The detected element type for completion.</returns>
    public static CompletionElementType DetectElementType(string input, int cursorPosition)
    {
        if (string.IsNullOrEmpty(input))
            return CompletionElementType.Empty;

        // Get the text up to cursor
        var inputToCursor = cursorPosition >= 0 && cursorPosition <= input.Length
            ? input.Substring(0, cursorPosition)
            : input;

        // Find the last token (word) being typed
        var lastSpaceIndex = inputToCursor.LastIndexOf(' ');
        var currentToken = lastSpaceIndex >= 0
            ? inputToCursor.Substring(lastSpaceIndex + 1)
            : inputToCursor;

        // Check if typing an argument name (--something)
        if (currentToken.StartsWith("--"))
            return CompletionElementType.ArgumentName;

        // Check if typing an argument alias (-s)
        if (currentToken.StartsWith("-") && !currentToken.StartsWith("--"))
            return CompletionElementType.ArgumentAlias;

        // Check if we're after an argument that needs a value
        // Look for pattern like: --argName <cursor>
        var tokens = inputToCursor.TrimEnd().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 2)
        {
            var previousToken = tokens[tokens.Length - 1];
            if (string.IsNullOrEmpty(currentToken) && 
                (previousToken.StartsWith("--") || previousToken.StartsWith("-")))
            {
                return CompletionElementType.ArgumentValue;
            }
        }

        // If first token or no space, it's likely a command
        if (!inputToCursor.Contains(' '))
            return CompletionElementType.Command;

        // After command with no dashes - could be positional or continuation
        return CompletionElementType.Positional;
    }

    /// <summary>
    /// Gets the current word being typed at the cursor position.
    /// </summary>
    /// <param name="input">The current input line.</param>
    /// <param name="cursorPosition">The cursor position.</param>
    /// <returns>The partial word being typed.</returns>
    public static string GetCurrentWord(string input, int cursorPosition)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var inputToCursor = cursorPosition >= 0 && cursorPosition <= input.Length
            ? input.Substring(0, cursorPosition)
            : input;

        var lastSpaceIndex = inputToCursor.LastIndexOf(' ');
        var currentWord = lastSpaceIndex >= 0
            ? inputToCursor.Substring(lastSpaceIndex + 1)
            : inputToCursor;

        // Strip leading dashes for prefix matching
        if (currentWord.StartsWith("--"))
            return currentWord.Substring(2);
        if (currentWord.StartsWith("-"))
            return currentWord.Substring(1);

        return currentWord;
    }

    /// <summary>
    /// Extracts the command name from the input.
    /// </summary>
    /// <param name="input">The current input line.</param>
    /// <returns>The command name, or null if not identifiable.</returns>
    public static string GetCommandName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var tokens = input.TrimStart().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return null;

        // The first token is typically the command name
        // For grouped commands, might need to combine tokens
        return tokens[0];
    }
}
