using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Used to build and manage a set of auto complete options for a given input string at a given position (one-based index).
    /// </summary>
    public class AutoCompleteOptionSetBuilder : IDisposable
    {
        private readonly CommandRegistry _registry;
        private readonly IServerProxy _serverProxy;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of hte AutoCompleteOptionsBuilder
        /// </summary>
        /// <param name="registry">The command registry to use for auto complete values of registered command elements</param>
        /// <param name="serverProxy">The server proxy to use for auto completion of argument values</param>
        /// <param name="serviceProvider">The service provider to use for instantiating command objects to execute auto complete functions</param>
        public AutoCompleteOptionSetBuilder(CommandRegistry registry, IServerProxy serverProxy, IServiceProvider serviceProvider)
        {
            _registry = registry;
            _serverProxy = serverProxy;
            _serviceProvider = serviceProvider;
        }


        /// <summary>
        /// Builds options for the parsed element under the cursor
        /// </summary>
        /// <returns>A Task</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the CommandElementType of the parsed element is not a case in the BuildOptions switch</exception>
        public async Task<AutoCompleteOptionSet> BuildOptions(ParsedCommandElement parsedElement, CancellationToken token = default)
        {
            switch (parsedElement.ElementType)
            {
                case CommandElementType.Command:
                    // For Command elements, first try positional arg completion, then fall back to command name completion
                    var positionalFromCommand = await BuildOptions_PositionalArgument(parsedElement, token);
                    if (positionalFromCommand != null) return positionalFromCommand;
                    return BuildOptions_Command(parsedElement);
                case CommandElementType.PositionalValue:
                    // PositionalValue could be a group/command path element or an actual positional argument
                    return await BuildOptions_PositionalValue(parsedElement, token);
                case CommandElementType.ArgumentName:
                    return BuildOptions_ArgumentName(parsedElement);
                case CommandElementType.ArgumentAlias:
                    return BuildOptions_Alias(parsedElement);
                case CommandElementType.ArgumentValue:
                    return await BuildOptions_ArgumentValue(parsedElement, token);
                case CommandElementType.Unexpected:
                    return BuildOptions_Unexpected(parsedElement);
                case CommandElementType.Empty:
                    // Empty could be position for named arg value OR positional arg
                    // Try named arg value first, then fall back to positional arg
                    return await BuildOptions_Empty(parsedElement, token);
                case CommandElementType.EndOfOptions:
                    // For autocomplete, bare "--" could be the user starting to type an argument name.
                    // Treat it like an incomplete argument name prefix for autocomplete purposes.
                    return BuildOptions_ArgumentName(parsedElement);
                default:
                    throw new ArgumentOutOfRangeException($"No case defined for {typeof(CommandElementType).FullName}");
            }
        }

        /// <summary>
        /// Handles Empty element type - could be for named argument value or positional argument
        /// </summary>
        private async Task<AutoCompleteOptionSet> BuildOptions_Empty(ParsedCommandElement parsedElement, CancellationToken token)
        {
            // First try named argument value (existing behavior)
            var result = await BuildOptions_ArgumentValue(parsedElement, token);
            if (result != null) return result;

            // Otherwise try positional argument autocomplete
            return await BuildOptions_PositionalArgument(parsedElement, token);
        }

        /// <summary>
        /// If the parsed element is an unexpected element, it may be an alias ('-') or argument ('--') prefix
        /// </summary>
        private AutoCompleteOptionSet BuildOptions_Unexpected(ParsedCommandElement parsedElement)
        {
            if (parsedElement.Raw.Equals(CommandInputParsingConstants.ElementPrefixArgument))
                return BuildOptions_ArgumentName(parsedElement);

            if (parsedElement.Raw.Equals(CommandInputParsingConstants.ElementPrefixAlias))
                return BuildOptions_Alias(parsedElement);

            return null;
        }

        /// <summary>
        /// Build options for a positional value element (could be group/command path or actual positional argument)
        /// </summary>
        /// <remarks>
        /// PositionalValue elements after a Command could be part of a group path (e.g., "myGroup myCommand")
        /// or actual positional arguments for a command. This method first tries group/command completion,
        /// then falls back to positional argument autocomplete.
        /// </remarks>
        private async Task<AutoCompleteOptionSet> BuildOptions_PositionalValue(ParsedCommandElement parsedElement, CancellationToken token = default)
        {
            var currentValue = parsedElement.Value;
            var optList = new List<AutoCompleteOption>();
            
            // Get all command and positional elements that form the potential command path
            var pathElements = parsedElement.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.Command || e.ElementType == CommandElementType.PositionalValue)
                .TakeWhile(e => e.Index <= parsedElement.Index)
                .ToList();
            var currentIndex = pathElements.IndexOf(parsedElement);
            
            // Build the group path from preceding elements
            var precedingGroupTokens = currentIndex > 0 
                ? pathElements.Take(currentIndex).Select(e => e.Value).ToList() 
                : new List<string>();
            
            // Navigate to the current group based on preceding tokens
            Component.GroupInfo currentGroup = null;
            if (precedingGroupTokens.Count > 0)
            {
                // Build the full path and find the group
                var groupPath = string.Join(" ", precedingGroupTokens);
                currentGroup = _registry.FindGroup(groupPath);
                if (currentGroup == null)
                {
                    // The preceding path doesn't match any group - this might be positional arguments
                    // Try to find a command and invoke positional argument autocomplete
                    return await BuildOptions_PositionalArgument(parsedElement, token);
                }
            }
            
            // Now suggest groups and commands at this level
            if (currentGroup == null)
            {
                // At root level - suggest root groups and root commands
                var matchingGroups = _registry.RootGroups
                    .Where(g => g.Name.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(g => g.Name)
                    .Select(g => new AutoCompleteOption(g.Name))
                    .ToList();
                
                var matchingCommands = _registry.RootCommands
                    .Where(c => c.Name.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(c => c.Name)
                    .Select(c => new AutoCompleteOption(c.Name))
                    .ToList();
                
                optList.AddRange(matchingGroups);
                optList.AddRange(matchingCommands);
            }
            else
            {
                // Inside a group - suggest child groups and commands in this group
                var matchingGroups = currentGroup.ChildGroups
                    .Where(g => g.Name.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(g => g.Name)
                    .Select(g => new AutoCompleteOption(g.Name))
                    .ToList();
                
                var matchingCommands = currentGroup.Commands
                    .Where(c => c.Name.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(c => c.Name)
                    .Select(c => new AutoCompleteOption(c.Name))
                    .ToList();
                
                optList.AddRange(matchingGroups);
                optList.AddRange(matchingCommands);
            }

            return optList.Count > 0 ? new AutoCompleteOptionSet(optList) : null;
        }

        /// <summary>
        /// Builds options for a positional argument by invoking its autocomplete function
        /// </summary>
        private async Task<AutoCompleteOptionSet> BuildOptions_PositionalArgument(ParsedCommandElement parsedElement, CancellationToken token = default)
        {
            // Get the command info for this element
            var cmdInfo = GetCommandInfo(parsedElement);
            if (cmdInfo == null) return null;

            // Get all positional arguments sorted by position
            var positionalArgs = cmdInfo.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .ToList();

            if (positionalArgs.Count == 0) return null;

            // Determine which positional argument this element corresponds to
            // Get all PositionalValue elements (not including Empty elements)
            var allPositionalElements = parsedElement.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.PositionalValue)
                .ToList();

            // Find elements that are part of command path vs actual positional values
            // The command path length = number of elements that form the command path
            var commandPathLength = GetCommandPathLength(parsedElement.ParentCommand);
            
            // Get positional value elements after the command path
            var positionalValueElements = allPositionalElements.Skip(commandPathLength - 1).ToList();
            
            // Determine current positional index
            int currentPositionalIndex;
            if (parsedElement.ElementType == CommandElementType.Empty)
            {
                // For Empty elements, the index is the count of existing positional values
                // (i.e., this would be the next positional argument slot)
                currentPositionalIndex = positionalValueElements.Count;
            }
            else
            {
                currentPositionalIndex = positionalValueElements.IndexOf(parsedElement);
                if (currentPositionalIndex < 0) return null;
            }

            // Find the corresponding positional argument
            ArgumentInfo argInfo = null;
            if (currentPositionalIndex < positionalArgs.Count)
            {
                argInfo = positionalArgs[currentPositionalIndex];
            }
            else
            {
                // Check if there's an IsRest argument
                var isRestArg = positionalArgs.FirstOrDefault(a => a.IsRest);
                if (isRestArg != null)
                {
                    argInfo = isRestArg;
                }
            }

            if (argInfo == null || string.IsNullOrEmpty(argInfo.AutoCompleteFunctionName))
                return null;

            // Build the autocomplete context with prior positional values
            var autoCompleteCtx = BuildAutoCompleteContextWithPositionalValues(parsedElement, cmdInfo, positionalValueElements, currentPositionalIndex);

            // Execute the autocomplete function
            List<AutoCompleteOption> results = new List<AutoCompleteOption>();

            if (cmdInfo.IsRemote)
            {
                results = await _serverProxy.AutoComplete(cmdInfo.Group?.FullPath, cmdInfo.Name, argInfo.AutoCompleteFunctionName, argInfo.IsAutoCompleteFunctionAsync, autoCompleteCtx, token) ?? [];
            }
            else
            {
                using var scope = _serviceProvider.CreateScope();
                var cmd = scope.ServiceProvider.GetRequiredService(cmdInfo.Type);
                var method = cmdInfo.Type.GetMethod(argInfo.AutoCompleteFunctionName);
                var args = new[] { autoCompleteCtx };

                results = await (argInfo.IsAutoCompleteFunctionAsync
                    ? (Task<List<AutoCompleteOption>>)method.Invoke(cmd, args)
                    : Task.Factory.StartNew(() => (List<AutoCompleteOption>)method.Invoke(cmd, args)));
            }

            return BuildAutoCompleteOptionSet(results, parsedElement.Value);
        }

        /// <summary>
        /// Gets the command path length (how many elements form the command path vs positional arguments)
        /// </summary>
        private int GetCommandPathLength(ParsedCommand parsedCmd)
        {
            var pathElements = parsedCmd.Elements
                .Where(e => e.ElementType == CommandElementType.Command || e.ElementType == CommandElementType.PositionalValue)
                .ToList();

            // Try progressively shorter paths from longest to shortest
            for (int length = pathElements.Count; length >= 1; length--)
            {
                var tryPath = string.Join(" ", pathElements.Take(length).Select(e => e.Value));
                var cmd = _registry.Find(tryPath);
                if (cmd != null)
                {
                    return length;
                }
            }

            return 1; // Default to 1 (just the command name)
        }

        /// <summary>
        /// Builds an AutoCompleteContext that includes prior positional argument values
        /// </summary>
        private AutoCompleteContext BuildAutoCompleteContextWithPositionalValues(
            ParsedCommandElement parsedElement,
            CommandInfo cmdInfo,
            List<ParsedCommandElement> positionalValueElements,
            int currentPositionalIndex)
        {
            var values = new Dictionary<ArgumentInfo, string>();

            // Get positional arguments
            var positionalArgs = cmdInfo.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .ToList();

            // Add prior positional values to context
            for (int i = 0; i < currentPositionalIndex && i < positionalArgs.Count; i++)
            {
                if (i < positionalValueElements.Count)
                {
                    values[positionalArgs[i]] = positionalValueElements[i].Value;
                }
            }

            // Also include named arguments from the standard context builder
            var standardCtx = BuildAutoCompleteContext(parsedElement);
            foreach (var kvp in standardCtx.Values)
            {
                if (!values.ContainsKey(kvp.Key))
                {
                    values[kvp.Key] = kvp.Value;
                }
            }

            return new AutoCompleteContext(parsedElement.Value, values);
        }

        /// <summary>
        /// Builds an AutoCompleteOptionSet from results, optionally filtering by current value
        /// </summary>
        private AutoCompleteOptionSet BuildAutoCompleteOptionSet(List<AutoCompleteOption> results, string currentValue)
        {
            if (results == null || results.Count == 0) return null;

            // Sort and filter based on current value
            var filtered = results.OrderBy(r => r.Value).ToList();
            
            // Find the starting index (first one starting with current value)
            int startingIndex = 0;
            if (!string.IsNullOrEmpty(currentValue))
            {
                var matchIndex = filtered.FindIndex(r => 
                    r.Value.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase));
                if (matchIndex >= 0)
                {
                    startingIndex = matchIndex;
                }
            }

            return new AutoCompleteOptionSet(filtered, startingIndex);
        }

        /// <summary>
        /// Build options for a command element (group or command name)
        /// </summary>
        /// <remarks>
        /// Supports space-separated group resolution:
        /// - "ma" -> suggests groups like "math" and commands like "makelogs"
        /// - "math " -> suggests commands under "math" group
        /// - "math a" -> suggests commands under "math" starting with "a"
        /// - "files io " -> suggests commands under "files io" group
        /// </remarks>
        private AutoCompleteOptionSet BuildOptions_Command(ParsedCommandElement parsedElement)
        {
            var currentValue = parsedElement.Value;
            var optList = new List<AutoCompleteOption>();
            
            // Get all command and positional elements that form the potential command path
            var allCommandElements = parsedElement.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.Command || e.ElementType == CommandElementType.PositionalValue)
                .ToList();
            var currentIndex = allCommandElements.IndexOf(parsedElement);
            
            // Build the group path from preceding command elements
            var precedingGroupTokens = currentIndex > 0 
                ? allCommandElements.Take(currentIndex).Select(e => e.Value).ToList() 
                : new List<string>();
            
            // Navigate to the current group based on preceding tokens
            Component.GroupInfo currentGroup = null;
            if (precedingGroupTokens.Count > 0)
            {
                // Build the full path and find the group
                var groupPath = string.Join(" ", precedingGroupTokens);
                currentGroup = _registry.FindGroup(groupPath);
                if (currentGroup == null)
                {
                    // Invalid group path - no suggestions
                    return null;
                }
            }
            
            // Now suggest groups and commands at this level
            if (currentGroup == null)
            {
                // At root level - suggest root groups and root commands
                var matchingGroups = _registry.RootGroups
                    .Where(g => g.Name.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(g => g.Name)
                    .Select(g => new AutoCompleteOption(g.Name))
                    .ToList();
                
                var matchingCommands = _registry.RootCommands
                    .Where(c => c.Name.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(c => c.Name)
                    .Select(c => new AutoCompleteOption(c.Name))
                    .ToList();
                
                optList.AddRange(matchingGroups);
                optList.AddRange(matchingCommands);
            }
            else
            {
                // Inside a group - suggest child groups and commands in this group
                var matchingGroups = currentGroup.ChildGroups
                    .Where(g => g.Name.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(g => g.Name)
                    .Select(g => new AutoCompleteOption(g.Name))
                    .ToList();
                
                var matchingCommands = currentGroup.Commands
                    .Where(c => c.Name.StartsWith(currentValue, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(c => c.Name)
                    .Select(c => new AutoCompleteOption(c.Name))
                    .ToList();
                
                optList.AddRange(matchingGroups);
                optList.AddRange(matchingCommands);
            }

            return optList.Count > 0 ? new AutoCompleteOptionSet(optList) : null;
        }

        /// <summary>
        /// Builds options for an argument name
        /// </summary>
        private AutoCompleteOptionSet BuildOptions_ArgumentName(ParsedCommandElement parsedElement)
        {
            // if the command info cannot be found for the parsed element, or there are no arguments on the command, return

            var cmdInfo = GetCommandInfo(parsedElement);
            if (cmdInfo == null || cmdInfo.Arguments.Count == 0) return null;

            // get the list of argument names defined on the parsed input so far, excluding the value of the current parsed element

            var usedArgNames = parsedElement.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.ArgumentName && e != parsedElement)
                .Select(e => e.Value.ToUpper())
                .ToList();

            // get the list of used aliases defined on the input so far

            var usedAliases = parsedElement.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.ArgumentAlias && e.Value.Length == 1)
                .OrderBy(e => e.Value)
                .Select(e => e.Value.ToUpper()[0])
                .ToList();

            // get the actual argument names from the command info that start with the value of the parsed element and haven't already
            // been used in the parssed input

            var argNames = cmdInfo.Arguments
                .Where(a =>
                    !usedArgNames.Contains(a.Name.ToUpper())
                    && !usedAliases.Contains(a.Alias.ToString().ToUpper()[0]))
                .Select(a => a.Name)
                .Order()
                .ToList();

            // build the options 

            return BuildOptionSet(argNames, $"--{{0}}", parsedElement.Value, true);
        }

        /// <summary>
        /// Builds options for an alias
        /// </summary>
        private AutoCompleteOptionSet BuildOptions_Alias(ParsedCommandElement parsedElement)
        {
            // aliases can only be auto completed from the position directly after the prefix, '-'

            if (parsedElement.Value.Length > 1) return null; 

            // if the command info cannot be found for the parsed element, or there are no arguments on the command, return

            var cmdInfo = GetCommandInfo(parsedElement);
            if (cmdInfo == null || cmdInfo.Arguments.Count == 0) return null;

            // get the list of argument names defined on the parsed input so far, excluding the value of the current parsed element

            var usedArgNames = parsedElement.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.ArgumentName)
                .Select(e => e.Value.ToUpper())
                .ToList();

            // get the list of valid aliases defined on the parsed input so far, excluding the value of the current parsed element

            var usedAliases = parsedElement.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.ArgumentAlias && e.Value.Length == 1 && e != parsedElement)
                .OrderBy(e => e.Value)
                .Select(e => e.Value.ToUpper()[0])
                .ToList();

            // get the actual argument aliases from the command info that haven't already been used in the parsed input

            var aliases = cmdInfo.Arguments
                .Where(a => 
                    !usedAliases.Contains(a.Alias.ToString().ToUpper()[0])
                    && !usedArgNames.Contains(a.Name.ToUpper()))
                .Select(a => a.Alias.ToString())
                .Order()
                .ToList();

            // build options

            FillOptions(aliases, $"-{{0}}");

            return BuildOptionSet(aliases, $"-{{0}}", parsedElement.Value, true);

        }

        /// <summary>
        /// Builds options for an argument value
        /// </summary>
        private async Task<AutoCompleteOptionSet> BuildOptions_ArgumentValue(ParsedCommandElement parsedElement, CancellationToken token)
        {
            // If the input parser labeled the parsed element as an argument value, then get the associated argument, but if the argument value is an empty string
            // the parser will have typed the element as Empty and any immediately preceeding argument element is needed

            var argumentElement = parsedElement.IsPairedWith ?? GetArgumentElementForEmptyParsedElement(parsedElement);
            if (argumentElement == null) return null; // if no argument element, then this isn't an argument value so treat as unexpected

            // get the command info - if null, unable to determine auto complete function - return

            var cmdInfo = GetCommandInfo(parsedElement);
            if (cmdInfo == null) return null;

            // get the arg info that matches the parsed input argument name / alias

            var argInfo = argumentElement.ElementType == CommandElementType.ArgumentName
                ? cmdInfo.Arguments.Where(a => a.Name.Equals(argumentElement.Value, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault()
                : cmdInfo.Arguments.Where(a => a.Alias.ToString().Equals(argumentElement.Value, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            // if the argument could not be found, or the auto complete function is not defined, return

            if (argInfo == null || string.IsNullOrEmpty(argInfo.AutoCompleteFunctionName)) return null;

            // otherwise execute

            var autoCompleteCtx = BuildAutoCompleteContext(parsedElement);

            List<AutoCompleteOption> results = new List<AutoCompleteOption>();

            if (cmdInfo.IsRemote) // remote server execution
            {
                // TODO: T075/T076 - Update IServerProxy to use groupPath instead of cmdNamespace
                results = 
                    await _serverProxy.AutoComplete(cmdInfo.Group?.FullPath, cmdInfo.Name, argInfo.AutoCompleteFunctionName, argInfo.IsAutoCompleteFunctionAsync, autoCompleteCtx, token) 
                    ?? [];
            }
            else // local command execution
            {
                // instantiate the command and execute the auto complete function

                using var scope = _serviceProvider.CreateScope();
                var cmd = scope.ServiceProvider.GetRequiredService(cmdInfo.Type);

                var method = cmdInfo.Type.GetMethod(argInfo.AutoCompleteFunctionName);
                var args = new[] { autoCompleteCtx };

                results = await (argInfo.IsAutoCompleteFunctionAsync
                        ? (Task<List<AutoCompleteOption>>)method.Invoke(cmd, args)
                        : Task.Factory.StartNew(() => (List<AutoCompleteOption>)method.Invoke(cmd, args)));

            }

            // build options from the results

            return BuildOptionSet(results, parsedElement.Value, true);
        }

        /// <summary>
        /// If the parsed element is an empty element this function returns the preceeding argument element
        /// </summary>
        /// <returns>The argument element preceeding the empty parsed element, or null if the parsed element is not empty and / or the preceeding
        /// element is not an argument element</returns>
        private ParsedCommandElement GetArgumentElementForEmptyParsedElement(ParsedCommandElement parsedElement)
        {
            // if the parsed element is an empty element, it may be an empty argument value. If so, examine the previous element - if an argument element
            // build options for that

            if (parsedElement.ElementType == CommandElementType.Empty)
            {
                // get the index of the parsed element

                var index = parsedElement.ParentCommand.Elements.ToList().IndexOf(parsedElement);

                // are there other elements in the command besides the parsed element and is the parsed element not the first element, then it may come
                // directly after an argument element

                if (parsedElement.ParentCommand.Elements.Count > 1 && index > 0)
                {
                    // get the previous element

                    var previousElement = parsedElement.ParentCommand.Elements[index - 1];

                    // if it's an argument element, then return it

                    if (previousElement.ElementType == CommandElementType.ArgumentName || previousElement.ElementType == CommandElementType.ArgumentAlias)
                        return previousElement;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds an auto complete context to be pased to a commands auto complete function
        /// </summary>
        /// <returns></returns>
        private AutoCompleteContext BuildAutoCompleteContext(ParsedCommandElement parsedElement)
        {
            // build dictionary of all argument values defined in the parsed input for valid arguments

            var cmdInfo = GetCommandInfo(parsedElement);
            var argValueDict = new Dictionary<ArgumentInfo, string>();

            foreach (var val in parsedElement.ParentCommand.Elements.Where(e => e.ElementType == CommandElementType.ArgumentValue))
            {
                var argInfo = val.IsPairedWith.ElementType == CommandElementType.ArgumentName
                    ? cmdInfo.Arguments.FirstOrDefault(a => a.Name.Equals(val.IsPairedWith.Value, StringComparison.InvariantCultureIgnoreCase))
                    : cmdInfo.Arguments.FirstOrDefault(a => a.Alias.ToString().Equals(val.IsPairedWith.Value, StringComparison.InvariantCultureIgnoreCase));

                if (argInfo != null)
                    argValueDict.Add(argInfo, val.Raw);
            }

            // return the context

            return new AutoCompleteContext(parsedElement.Value, argValueDict);
        }

        /// <summary>
        /// Gets the command info associated with the parsed element
        /// </summary>
        /// <returns>The command info, or null if there is none</returns>
        private CommandInfo GetCommandInfo(ParsedCommandElement parsedElement)
        {
            if (parsedElement.ParentCommand.GetCommandElement() == null) return null;
            
            // Use progressive path matching like CommandResolver.FindCommandWithLongestPath
            var fullPath = parsedElement.ParentCommand.GetFullCommandPath();
            var pathParts = fullPath.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Try progressively shorter paths from longest to shortest
            for (int length = pathParts.Length; length >= 1; length--)
            {
                var tryPath = string.Join(" ", pathParts.Take(length));
                var cmdInfo = _registry.Find(tryPath);
                if (cmdInfo != null)
                {
                    return cmdInfo;
                }
            }

            return null;
        }

        private AutoCompleteOptionSet BuildOptionSet(List<AutoCompleteOption> options, string query, bool returnNullOnNoQueryMatch)
        {
            if (string.IsNullOrEmpty(query))
                return new AutoCompleteOptionSet(options);

            var startingIndex = GetStartingIndex(options, query);

            if (startingIndex == -1)
                return returnNullOnNoQueryMatch ? null : new AutoCompleteOptionSet(options);

            return new AutoCompleteOptionSet(options, startingIndex);
        }

        private AutoCompleteOptionSet BuildOptionSet(List<string> values, string format, string query, bool returnNullOnNoQueryMatch)
            => BuildOptionSet(FillOptions(values, format), query, returnNullOnNoQueryMatch);

        /// <summary>
        /// Builds the options from the given values and format
        /// </summary>
        /// <param name="values">The values to build options for</param>
        /// <param name="format">The format string to use for each option</param>
        private List<AutoCompleteOption> FillOptions(List<string> values, string format = null)
        {
            var options = new List<AutoCompleteOption>();
            foreach (var str in values)
                options.Add(new AutoCompleteOption(str, format));

            return options;
        }

        /// <summary>
        /// Gets the starting option index given a query value
        /// </summary>
        /// <param name="options">The options</param>
        /// <param name="forValue">The query value</param>
        /// <returns>The index of the option that matches the query, or 0 if no option was found</returns>
        private int GetStartingIndex(List<AutoCompleteOption> options, string forValue)
            => options.IndexOf(options.Where(o => o.Value.StartsWith(forValue, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault());

        public void Dispose()
        {
            if (_serviceProvider != null)
                ((IDisposable)_serviceProvider).Dispose(); // assuming the Microsoft provided ServiceProvider continues to implement IDisposable going forward
        }
    }
}
