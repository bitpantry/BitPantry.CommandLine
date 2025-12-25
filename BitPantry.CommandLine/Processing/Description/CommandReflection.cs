using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Processing.Description
{
    /// <summary>
    /// Provides reflection services for CommandBase objects
    /// </summary>
    public class CommandReflection
    {
        /// <summary>
        /// Describes the given command type
        /// </summary>
        /// <typeparam name="T">The type of the command to describe</typeparam>
        /// <returns>A CommandInfo object that describes the command</returns>
        public static CommandInfo Describe<T>() { return Describe(typeof(T)); }

        /// <summary>
        /// Describes the given command type
        /// </summary>
        /// <param name="commandType">The type of the command to describe</param>
        /// <returns>A CommandInfo object that describes the command</returns>
        public static CommandInfo Describe(Type commandType)
        {
            try
            {
                var info = new CommandInfo();
                var properties = commandType.GetProperties();

                // check base type

                if (!commandType.IsSubclassOf(typeof(CommandBase)))
                    throw new CommandDescriptionException(commandType, $"The command type must extend type {typeof(CommandBase).FullName}");

                // check for abstract classes

                if (commandType.IsAbstract)
                    throw new CommandDescriptionException(commandType, "The command type cannot be abstract");

                info.Type = commandType;

                // get the command name (Group will be associated later during registration)

                var cmdAttr = GetAttributes<CommandAttribute>(commandType).SingleOrDefault();

                // Note: info.Group will be set during CommandRegistry.RegisterCommand() based on cmdAttr.Group type
                info.Name = cmdAttr?.Name ?? commandType.Name;

                // get the description attribute

                info.Description = GetAttributes<DescriptionAttribute>(commandType).SingleOrDefault()?.Description;

                // get parameters

                info.Arguments = GetArgumentInfos(commandType, properties);

                // validate positional arguments
                ValidatePositionalArguments(commandType, info.Arguments);

                // describe execution function

                DescribeExecutionFunction(commandType, info);

                return info;
            }
            catch (PositionalArgumentValidationException)
            {
                // Let positional validation exceptions propagate without wrapping
                throw;
            }
            catch(Exception ex)
            {
                throw new CommandDescriptionException(commandType, "An error occured while describing the command type", ex);
            }
        }

        // Derived from - https://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.asyncstatemachineattribute.aspx
        private static void DescribeExecutionFunction(Type commandType, CommandInfo info)
        {
            var method = commandType.GetMethod("Execute");

            var badMethodException = new CommandDescriptionException(commandType, "The command must implement a public Execute function with a signature of \"public void Execute(<optional> CommandExecutionContext)\" or \"public async Task Execute(<optional> CommandExecutionContext)\\\"");

            if (method == null)
                throw badMethodException;

            // get if is synch or async

            info.IsExecuteAsync = method.IsAsync();

            // does command execution context exist - is it a generic

            var parameters = method.GetParameters();
            if (parameters.Count() == 1)
            {
                if (!typeof(CommandExecutionContext).IsAssignableFrom(parameters[0].ParameterType))
                    throw badMethodException;

                if (parameters[0].ParameterType.IsGenericType)
                    info.InputType = parameters[0].ParameterType.GetGenericArguments().Single();
            }
            else
            {
                throw badMethodException;
            }

            // get return type
           
            if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                info.ReturnType = method.ReturnType.GetGenericArguments().First();
            else if (method.ReturnType != typeof(Task) && method.ReturnType != typeof(void))
                info.ReturnType = method.ReturnType;
        }

        private static IReadOnlyCollection<ArgumentInfo> GetArgumentInfos(Type commandType, PropertyInfo[] properties)
        {
            var arguments = new List<ArgumentInfo>();

            foreach (var property in properties)
            {
                var paramAttr = GetAttributes<ArgumentAttribute>(property).SingleOrDefault();

                if (paramAttr != null)
                {
                    var aliasAttr = GetAttributes<AliasAttribute>(property).SingleOrDefault();
                    var descAttr = GetAttributes<DescriptionAttribute>(property).SingleOrDefault();

                    // completion attribute
                    var completionAttr = GetAttributes<CompletionAttribute>(property).SingleOrDefault();
                    var isCompletionMethodAsync = false;

                    // Validate method-based completion if specified
                    if (completionAttr?.MethodName != null)
                    {
                        var method = commandType.GetMethod(completionAttr.MethodName);
                        if (method != null)
                        {
                            isCompletionMethodAsync = method.IsAsync();
                        }
                        // Note: Validation is deferred to runtime per spec (A3)
                    }

                    // add info

                    arguments.Add(new ArgumentInfo
                    {
                        Name = paramAttr.Name ?? property.Name,
                        CompletionAttribute = completionAttr,
                        IsCompletionMethodAsync = isCompletionMethodAsync,
                        Alias = aliasAttr == null ? default(char) : aliasAttr.Alias,
                        Description = descAttr?.Description,
                        PropertyInfo = new SerializablePropertyInfo(property),
                        IsRequired = paramAttr.IsRequired,
                        Position = paramAttr.Position,
                        IsRest = paramAttr.IsRest
                    });
                }
            }

            return arguments.AsReadOnly();
        }

        /// <summary>
        /// Validates positional argument configuration for a command
        /// </summary>
        /// <param name="commandType">The command type being validated</param>
        /// <param name="arguments">The arguments to validate</param>
        private static void ValidatePositionalArguments(Type commandType, IReadOnlyCollection<ArgumentInfo> arguments)
        {
            // VAL-010: Position must be >= 0 (any explicit negative other than -1 is invalid)
            // Note: Position = -1 means named argument (default), so we don't error on that
            // But Position = -2 or lower is explicitly invalid
            // This check must run FIRST before we filter for positional args
            foreach (var arg in arguments.Where(a => a.Position < -1))
            {
                throw new PositionalArgumentValidationException(
                    commandType,
                    $"Position must be >= 0 for positional arguments, or -1 for named arguments. Found Position={arg.Position}",
                    arg.Name);
            }

            var positionalArgs = arguments.Where(a => a.IsPositional).OrderBy(a => a.Position).ToList();
            var isRestArgs = arguments.Where(a => a.IsRest).ToList();

            // No positional arguments? Nothing to validate
            if (!positionalArgs.Any() && !isRestArgs.Any())
                return;

            // VAL-005: IsRest must have Position >= 0 (must be positional)
            foreach (var arg in isRestArgs)
            {
                if (!arg.IsPositional)
                {
                    throw new PositionalArgumentValidationException(
                        commandType,
                        "IsRest can only be used on positional arguments (Position >= 0)",
                        arg.Name);
                }
            }

            // VAL-004: IsRest must be on a collection type
            foreach (var arg in isRestArgs)
            {
                if (!arg.IsCollection)
                {
                    throw new PositionalArgumentValidationException(
                        commandType,
                        "IsRest can only be used on collection types (array, List<T>, etc.)",
                        arg.Name);
                }
            }

            // VAL-006: Only one IsRest argument allowed
            if (isRestArgs.Count > 1)
            {
                throw new PositionalArgumentValidationException(
                    commandType,
                    $"Only one IsRest argument is allowed per command. Found {isRestArgs.Count}: {string.Join(", ", isRestArgs.Select(a => a.Name))}",
                    isRestArgs[1].Name);
            }

            // VAL-007: IsRest must be the last positional argument
            if (isRestArgs.Count == 1)
            {
                var isRestArg = isRestArgs[0];
                var maxPosition = positionalArgs.Any() ? positionalArgs.Max(a => a.Position) : -1;
                if (isRestArg.Position != maxPosition)
                {
                    throw new PositionalArgumentValidationException(
                        commandType,
                        $"IsRest argument must be the last positional argument (highest Position value). '{isRestArg.Name}' has Position={isRestArg.Position} but max is {maxPosition}",
                        isRestArg.Name);
                }
            }

            // VAL-009: Duplicate positions not allowed
            var duplicatePositions = positionalArgs
                .GroupBy(a => a.Position)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicatePositions.Any())
            {
                var dup = duplicatePositions.First();
                throw new PositionalArgumentValidationException(
                    commandType,
                    $"Duplicate Position value {dup.Key} found on arguments: {string.Join(", ", dup.Select(a => a.Name))}",
                    dup.Skip(1).First().Name);
            }

            // VAL-008: Positions must be contiguous starting from 0
            if (positionalArgs.Any())
            {
                for (int i = 0; i < positionalArgs.Count; i++)
                {
                    if (positionalArgs[i].Position != i)
                    {
                        var expectedPositions = string.Join(", ", Enumerable.Range(0, positionalArgs.Count));
                        var actualPositions = string.Join(", ", positionalArgs.Select(a => a.Position));
                        throw new PositionalArgumentValidationException(
                            commandType,
                            $"Positional arguments must have contiguous Position values starting from 0. Expected positions [{expectedPositions}] but found [{actualPositions}]",
                            positionalArgs[i].Name);
                    }
                }
            }
        }

        // internal helper function to return attributes from a type
        private static List<T> GetAttributes<T>(Type type)
        {
            object[] attributeList = type.GetCustomAttributes(typeof(T), true);
            return attributeList.Select(p => (T)p).ToList();
        }

        // internal helper function to return attributes from a property
        private static List<T> GetAttributes<T>(PropertyInfo property)
        {
            object[] attributeList = property.GetCustomAttributes(typeof(T), true);
            return attributeList.Select(p => (T)p).ToList();
        }
    }
}
