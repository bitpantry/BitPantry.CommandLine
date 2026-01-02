using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.Component;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for command names and command groups.
/// </summary>
/// <remarks>
/// This provider handles:
/// - Empty input: suggests all root-level commands and groups
/// - Partial command names: filters by prefix/contains match
/// - After group name: suggests commands within that group
/// </remarks>
public class CommandCompletionProvider : ICompletionProvider
{
    private readonly CommandRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandCompletionProvider"/> class.
    /// </summary>
    /// <param name="registry">The command registry containing all registered commands.</param>
    public CommandCompletionProvider(CommandRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Handle empty input or command element type
        return context.ElementType == CompletionElementType.Empty ||
               context.ElementType == CompletionElementType.Command;
    }

    /// <inheritdoc />
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(CompletionResult.Empty);

        var items = new List<CompletionItem>();
        var prefix = context.PartialValue ?? string.Empty;

        // Get the parent group path if navigating into groups
        var (groupPath, hasUnrecognizedWords) = GetGroupPathWithValidation(context.InputText);

        // If there are unrecognized words before the partial value, don't offer completions
        if (hasUnrecognizedWords)
        {
            return Task.FromResult(CompletionResult.Empty);
        }

        if (string.IsNullOrEmpty(groupPath))
        {
            // At root level - show root commands and groups
            AddRootCommands(items, prefix);
            AddRootGroups(items, prefix);
        }
        else
        {
            // Within a group - show commands and subgroups in that group
            var group = FindGroup(groupPath);
            if (group != null)
            {
                AddGroupCommands(items, group, prefix);
                AddChildGroups(items, group, prefix);
            }
        }

        // Sort by kind (groups first), then by name
        var sortedItems = items
            .OrderBy(i => i.Kind == CompletionItemKind.CommandGroup ? 0 : 1)
            .ThenBy(i => i.InsertText, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(new CompletionResult(sortedItems, sortedItems.Count));
    }

    /// <summary>
    /// Extracts the group path from the input text with validation.
    /// Returns the group path and whether there are unrecognized words.
    /// </summary>
    /// <remarks>
    /// For "grp1 subgrp ", returns ("grp1 subgrp", false) if both are valid groups.
    /// For "xyznonexistent ", returns ("", true) because "xyznonexistent" is not a valid group.
    /// For "grp1 unknownword ", returns ("grp1", true) because "unknownword" is not valid in grp1.
    /// </remarks>
    private (string GroupPath, bool HasUnrecognizedWords) GetGroupPathWithValidation(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
            return (string.Empty, false);

        var parts = inputText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Check if input ends with space - if so, all parts are complete (no partial at end)
        var endsWithSpace = inputText.EndsWith(" ");
        
        if (parts.Length == 0)
            return (string.Empty, false);
            
        // If no trailing space and only one part, it's the partial command - no group path
        if (!endsWithSpace && parts.Length == 1)
            return (string.Empty, false);

        // Check how many leading parts are groups
        var groupPath = new List<string>();
        GroupInfo currentGroup = null;

        // If ends with space, check all parts for groups; otherwise exclude last part (partial command)
        var partsToCheck = endsWithSpace ? parts.Length : parts.Length - 1;
        var partsChecked = 0;
        
        foreach (var part in parts.Take(partsToCheck))
        {
            partsChecked++;
            var nextGroup = FindChildGroup(currentGroup, part);
            if (nextGroup != null)
            {
                groupPath.Add(part);
                currentGroup = nextGroup;
            }
            else
            {
                // Not a group - check if it's a valid command in the current context
                var command = FindCommand(currentGroup, part);
                if (command != null)
                {
                    // It's a command - stop traversing groups
                    // Check if there are more parts after this command
                    if (partsChecked < partsToCheck)
                    {
                        // There are more words after the command - unrecognized
                        return (string.Join(" ", groupPath), true);
                    }
                    // Command is valid, no more words to check
                    break;
                }
                else
                {
                    // Neither a group nor a command - unrecognized word
                    return (string.Join(" ", groupPath), true);
                }
            }
        }

        return (string.Join(" ", groupPath), false);
    }

    /// <summary>
    /// Finds a command within a group (or at root if group is null).
    /// </summary>
    private CommandInfo FindCommand(GroupInfo group, string name)
    {
        if (group == null)
        {
            // Root level - find root commands
            return _registry.Commands.FirstOrDefault(c => 
                c.Group == null && 
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            // Within group - find commands in this group
            return _registry.Commands.FirstOrDefault(c => 
                c.Group != null &&
                c.Group.Name == group.Name &&
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Extracts the group path from the input text.
    /// </summary>
    /// <remarks>
    /// For "grp1 subgrp cmd", returns "grp1 subgrp".
    /// For "grp1 " (trailing space), returns "grp1" if grp1 is a group.
    /// For "cmd", returns empty string.
    /// </remarks>
    private string GetGroupPath(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
            return string.Empty;

        var parts = inputText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Check if input ends with space - if so, all parts are complete (no partial at end)
        var endsWithSpace = inputText.EndsWith(" ");
        
        if (parts.Length == 0)
            return string.Empty;
            
        // If no trailing space and only one part, it's the partial command - no group path
        if (!endsWithSpace && parts.Length == 1)
            return string.Empty;

        // Check how many leading parts are groups
        var groupPath = new List<string>();
        GroupInfo currentGroup = null;

        // If ends with space, check all parts for groups; otherwise exclude last part (partial command)
        var partsToCheck = endsWithSpace ? parts.Length : parts.Length - 1;
        
        foreach (var part in parts.Take(partsToCheck))
        {
            var nextGroup = FindChildGroup(currentGroup, part);
            if (nextGroup != null)
            {
                groupPath.Add(part);
                currentGroup = nextGroup;
            }
            else
            {
                // Not a group - stop here
                break;
            }
        }

        return string.Join(" ", groupPath);
    }

    /// <summary>
    /// Finds a group by its full path (space-separated).
    /// </summary>
    private GroupInfo FindGroup(string groupPath)
    {
        if (string.IsNullOrWhiteSpace(groupPath))
            return null;

        var parts = groupPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        GroupInfo current = null;

        foreach (var part in parts)
        {
            current = FindChildGroup(current, part);
            if (current == null)
                return null;
        }

        return current;
    }

    /// <summary>
    /// Finds a child group by name within a parent group (or root if parent is null).
    /// </summary>
    private GroupInfo FindChildGroup(GroupInfo parent, string name)
    {
        if (parent == null)
        {
            return _registry.RootGroups.FirstOrDefault(g =>
                string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        return parent.ChildGroups.FirstOrDefault(g =>
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds root-level commands to the completion list.
    /// </summary>
    private void AddRootCommands(List<CompletionItem> items, string prefix)
    {
        foreach (var cmd in _registry.RootCommands)
        {
            if (MatchesPrefix(cmd.Name, prefix))
            {
                items.Add(new CompletionItem
                {
                    InsertText = cmd.Name,
                    Description = cmd.Description ?? string.Empty,
                    Kind = CompletionItemKind.Command
                });
            }
        }
    }

    /// <summary>
    /// Adds root-level groups to the completion list.
    /// </summary>
    private void AddRootGroups(List<CompletionItem> items, string prefix)
    {
        foreach (var group in _registry.RootGroups)
        {
            if (MatchesPrefix(group.Name, prefix))
            {
                items.Add(new CompletionItem
                {
                    InsertText = group.Name,
                    Description = group.Description ?? string.Empty,
                    Kind = CompletionItemKind.CommandGroup
                });
            }
        }
    }

    /// <summary>
    /// Adds commands within a specific group to the completion list.
    /// </summary>
    private void AddGroupCommands(List<CompletionItem> items, GroupInfo group, string prefix)
    {
        foreach (var cmd in group.Commands)
        {
            if (MatchesPrefix(cmd.Name, prefix))
            {
                items.Add(new CompletionItem
                {
                    InsertText = cmd.Name,
                    Description = cmd.Description ?? string.Empty,
                    Kind = CompletionItemKind.Command
                });
            }
        }
    }

    /// <summary>
    /// Adds child groups within a specific group to the completion list.
    /// </summary>
    private void AddChildGroups(List<CompletionItem> items, GroupInfo group, string prefix)
    {
        foreach (var childGroup in group.ChildGroups)
        {
            if (MatchesPrefix(childGroup.Name, prefix))
            {
                items.Add(new CompletionItem
                {
                    InsertText = childGroup.Name,
                    Description = childGroup.Description ?? string.Empty,
                    Kind = CompletionItemKind.CommandGroup
                });
            }
        }
    }

    /// <summary>
    /// Checks if a name matches the given prefix (case-insensitive).
    /// </summary>
    private static bool MatchesPrefix(string name, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return true;

        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
