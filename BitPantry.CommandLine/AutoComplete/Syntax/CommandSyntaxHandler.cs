using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;

namespace BitPantry.CommandLine.AutoComplete.Syntax;

/// <summary>
/// Handler for command syntax autocomplete (groups, commands, aliases).
/// Implements IAutoCompleteHandler for consistency with value handlers.
/// </summary>
public class CommandSyntaxHandler : IAutoCompleteHandler
{
    private readonly ICommandRegistry _registry;

    /// <summary>
    /// Creates a new CommandSyntaxHandler.
    /// </summary>
    /// <param name="registry">The command registry to search.</param>
    public CommandSyntaxHandler(ICommandRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var queryString = context.QueryString ?? "";
        var options = new List<AutoCompleteOption>();

        // Parse FullInput to determine if we're in a group context
        var currentGroup = DetermineCurrentGroup(context.FullInput, context.CursorPosition);

        // If we're within a group context, suggest commands and child groups in that group
        if (currentGroup != null)
        {
            // Add matching commands within the current group
            foreach (var command in _registry.Commands)
            {
                if (command.Group?.MarkerType == currentGroup.MarkerType)
                {
                    if (command.Name.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
                    {
                        options.Add(new AutoCompleteOption(command.Name, command.Description));
                    }
                }
            }

            // BUG-001 FIX: Add matching child groups (subgroups) within the current group
            foreach (var childGroup in currentGroup.ChildGroups)
            {
                if (childGroup.Name.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new AutoCompleteOption(childGroup.Name, childGroup.Description, isGroup: true));
                }
            }
        }
        else
        {
            // At root level, suggest matching group names
            foreach (var group in _registry.Groups)
            {
                if (group.Name.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new AutoCompleteOption(group.Name, group.Description, isGroup: true));
                }
            }

            // Also suggest root-level commands (commands with no group)
            foreach (var command in _registry.RootCommands)
            {
                if (command.Name.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new AutoCompleteOption(command.Name, command.Description));
                }
            }
        }

        // Sort alphabetically
        return Task.FromResult(
            options.OrderBy(o => o.Value, StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>
    /// Determines the current group context by parsing the pipe segment containing the cursor.
    /// Returns null if at root level, or the deepest GroupInfo if the segment contains group names.
    /// Walks through nested group hierarchy for paths like "server profile ".
    /// </summary>
    private GroupInfo DetermineCurrentGroup(string fullInput, int cursorPosition)
    {
        if (string.IsNullOrWhiteSpace(fullInput))
            return null;

        // Parse the input and find the segment containing the cursor
        var parsedInput = new ParsedInput(fullInput);
        var parsedCommand = FindSegmentForCursor(parsedInput, cursorPosition);

        if (parsedCommand == null || parsedCommand.Elements.Count == 0)
            return null;

        // Compute the segment's text length for "committed" checks
        var segmentLength = parsedCommand.StringLength;

        // BUG-002 FIX: Walk through all elements to find the deepest group context
        GroupInfo currentGroup = null;

        foreach (var element in parsedCommand.Elements)
        {
            // Skip empty elements
            if (element.ElementType == CommandElementType.Empty)
                continue;

            // Only consider committed elements (those followed by more input or trailing space)
            // An element is "committed" if there's content after it in the segment
            bool isCommitted = element.EndPosition < segmentLength;
            if (!isCommitted)
                break; // Current element is still being typed - stop here

            var tokenValue = element.Value;

            if (currentGroup == null)
            {
                // At root level - try to match a root group
                var rootGroup = _registry.Groups.FirstOrDefault(
                    g => g.Parent == null && g.Name.Equals(tokenValue, StringComparison.OrdinalIgnoreCase));
                
                if (rootGroup != null)
                {
                    currentGroup = rootGroup;
                    continue;
                }
                // First token doesn't match any group - no group context
                break;
            }
            else
            {
                // Inside a group - try to match a child group
                var childGroup = currentGroup.ChildGroups.FirstOrDefault(
                    g => g.Name.Equals(tokenValue, StringComparison.OrdinalIgnoreCase));
                
                if (childGroup != null)
                {
                    currentGroup = childGroup;
                    continue;
                }
                // Token doesn't match any child group - could be a command name, stop walking
                break;
            }
        }

        return currentGroup;
    }

    /// <summary>
    /// Finds the ParsedCommand segment that contains the given cursor position.
    /// Uses the same boundary logic as ParsedInput.GetCursorPositionRelativeToCommandString.
    /// </summary>
    private static ParsedCommand FindSegmentForCursor(ParsedInput parsedInput, int cursorPosition)
    {
        int currentCmdStartPos = 0;

        foreach (var cmd in parsedInput.ParsedCommands)
        {
            if (cursorPosition > currentCmdStartPos && cursorPosition <= currentCmdStartPos + cmd.StringLength + 1)
            {
                return cmd;
            }
            currentCmdStartPos += cmd.StringLength + 1; // +1 for the pipe delimiter
        }

        // Cursor past all segments - use the last segment
        if (parsedInput.ParsedCommands.Count > 0)
        {
            return parsedInput.ParsedCommands[parsedInput.ParsedCommands.Count - 1];
        }

        return null;
    }
}
