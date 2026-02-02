using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.Parsing.Strings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace BitPantry.CommandLine.Processing.Activation
{
    public class CommandActivator : IDisposable
    {
        private IServiceProvider _svcProvider;

        /// <summary>
        /// Instantiates a new CommandActivator with a provided command dependency container
        /// </summary>
        /// <param name="container">The container to use for injecting commands</param>
        public CommandActivator(IServiceProvider svcProvider)
        {
            _svcProvider = svcProvider;
        }

        /// <summary>
        /// Activates the resolved command
        /// </summary>
        /// <param name="resCmd">The resolved command</param>
        /// <returns>An activation result</returns>
        public ActivationResult Activate(ResolvedCommand resCmd)
        {
            // check for errors

            if (!resCmd.IsValid)
                throw new CommandActivationException(resCmd, "Cannot activate command with resolution errors");

            // create command instance

            var scope = _svcProvider.CreateScope();
            var cmd = scope.ServiceProvider.GetRequiredService(resCmd.CommandInfo.Type) as CommandBase;

            // inject property values

            foreach (var info in resCmd.CommandInfo.Arguments)
            {
                if (info.IsFlag)
                    // Flag arguments: presence = true, absence = false
                    info.PropertyInfo.SetValue(cmd, resCmd.InputMap.ContainsKey(info));
                else if (resCmd.IsRestValues != null && resCmd.IsRestValues.ContainsKey(info))
                {
                    // Multi-value argument (IsRest positional or repeated named option) - populate array from all collected values
                    var restElements = resCmd.IsRestValues[info];
                    
                    // For named arguments, extract value from IsPairedWith; for positional, use element Value directly
                    var stringValues = restElements.Select(e => info.IsPositional ? e.Value : e.IsPairedWith?.Value).ToArray();
                    
                    var propertyType = info.PropertyInfo.GetPropertyInfo().PropertyType;
                    var elementType = propertyType.IsArray 
                        ? propertyType.GetElementType() 
                        : propertyType.GetGenericArguments().FirstOrDefault() ?? typeof(string);
                    
                    // Create and populate the array
                    var typedArray = Array.CreateInstance(elementType, stringValues.Length);
                    for (int i = 0; i < stringValues.Length; i++)
                    {
                        try
                        {
                            var parsedValue = StringParsing.Parse(elementType, stringValues[i]);
                            typedArray.SetValue(parsedValue, i);
                        }
                        catch (Exception ex)
                        {
                            throw new CommandFailedException($"Invalid value '{stringValues[i]}' for argument '{info.Name}': {ex.Message}", ex);
                        }
                    }
                    
                    info.PropertyInfo.SetValue(cmd, typedArray);
                }
                else if(resCmd.InputMap.ContainsKey(info))
                {
                    var element = resCmd.InputMap[info];
                    
                    // Determine value based on how the argument was PROVIDED in input, not how it's defined.
                    // If element has IsPairedWith, it was provided as named (--name value), so value is in paired element.
                    // If element has no IsPairedWith, it was provided positionally, so value is in the element itself.
                    string valueToUse;
                    if (element.IsPairedWith != null)
                    {
                        // Provided as named argument - value comes from the paired element
                        valueToUse = element.IsPairedWith.Value;
                    }
                    else
                    {
                        // Provided positionally - the element itself contains the value
                        valueToUse = element.Value;
                    }
                    
                    try
                    {
                        info.PropertyInfo.SetValue(cmd, StringParsing.Parse(info.PropertyInfo.GetPropertyInfo().PropertyType, valueToUse));
                    }
                    catch (Exception ex)
                    {
                        throw new CommandFailedException($"Invalid value '{valueToUse}' for argument '{info.Name}': {ex.Message}", ex);
                    }
                }
            }

            // return result

            return new ActivationResult(cmd, resCmd, scope);
        }

        public void Dispose()
        {
            if (_svcProvider != null)
                ((IDisposable)_svcProvider).Dispose(); // assuming the Microsoft provided ServiceProvider continues to implement IDisposable going forward
        }
    }
}
