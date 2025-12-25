using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions by invoking a method on the command class.
/// </summary>
/// <remarks>
/// This provider handles [Completion("methodName")] style completions
/// where a method on the command class generates the suggestions.
/// The method can have dependencies injected via parameters.
/// </remarks>
public class MethodProvider : ICompletionProvider
{
    /// <inheritdoc />
    /// <remarks>
    /// Priority 75 - highest priority for value completion.
    /// </remarks>
    public int Priority => 75;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Only handle argument values with method name in completion attribute
        if (context.ElementType != CompletionElementType.ArgumentValue)
            return false;

        return !string.IsNullOrEmpty(context.CompletionAttribute?.MethodName);
    }

    /// <inheritdoc />
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return CompletionResult.Empty;

        var methodName = context.CompletionAttribute?.MethodName;
        if (string.IsNullOrEmpty(methodName))
            return CompletionResult.Empty;

        if (context.CommandInstance == null)
            return CompletionResult.Empty;

        var commandType = context.CommandInstance.GetType();
        var method = commandType.GetMethod(methodName, 
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (method == null)
            return CompletionResult.Empty;

        try
        {
            // Build method parameters with DI injection
            var parameters = BuildParameters(method, context);

            // Invoke the method
            var result = method.Invoke(context.CommandInstance, parameters);

            // Handle async methods
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                
                // Get result from Task<T>
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    var resultProperty = taskType.GetProperty("Result");
                    result = resultProperty?.GetValue(task);
                }
                else
                {
                    result = null;
                }
            }

            // Convert result to completion items
            var items = ConvertToCompletionItems(result, context.CurrentWord);

            return new CompletionResult(items);
        }
        catch (Exception)
        {
            // Method invocation failed - return empty
            return CompletionResult.Empty;
        }
    }

    private object[] BuildParameters(MethodInfo method, CompletionContext context)
    {
        var parameters = method.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            // Check for known context types
            if (paramType == typeof(CompletionContext))
            {
                args[i] = context;
            }
            else if (paramType == typeof(string))
            {
                // Assume it's the current partial value
                args[i] = context.CurrentWord ?? string.Empty;
            }
            else if (paramType == typeof(CancellationToken))
            {
                args[i] = CancellationToken.None;
            }
            else if (context.Services != null)
            {
                // Try to resolve from DI
                args[i] = context.Services.GetService(paramType);
            }
        }

        return args;
    }

    private List<CompletionItem> ConvertToCompletionItems(object result, string prefix)
    {
        var items = new List<CompletionItem>();

        if (result == null)
            return items;

        // Handle IEnumerable<CompletionItem>
        if (result is IEnumerable<CompletionItem> completionItems)
        {
            items.AddRange(completionItems);
        }
        // Handle IEnumerable<string>
        else if (result is IEnumerable<string> stringItems)
        {
            foreach (var item in stringItems)
            {
                if (item == null) continue;

                items.Add(new CompletionItem
                {
                    DisplayText = item,
                    InsertText = item,
                    Kind = CompletionItemKind.ArgumentValue
                });
            }
        }

        // Filter by prefix if provided
        if (!string.IsNullOrEmpty(prefix))
        {
            items = items
                .Where(i => i.InsertText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                           i.DisplayText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Sort alphabetically
        return items.OrderBy(i => i.DisplayText).ToList();
    }
}
