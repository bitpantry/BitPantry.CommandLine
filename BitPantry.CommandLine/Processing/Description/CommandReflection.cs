using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
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

                // describe execution function

                DescribeExecutionFunction(commandType, info);

                return info;
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

                    // auto complete function

                    var isAutoCompleteFunctionAsync = false;

                    if (!string.IsNullOrEmpty(paramAttr.AutoCompleteFunctionName))
                    {
                        var badAutoCompleteFunctionException = new CommandDescriptionException(commandType, $"An auto complete function for argument, {property.Name}, could not be found with a signature of \"public List<AutoCompleteOption> {paramAttr.AutoCompleteFunctionName}(AutoCompleteContext)\" or \"public async Task<List<AutoCompleteOption>> {paramAttr.AutoCompleteFunctionName}(AutoCompleteContext)\"");

                        var method = commandType.GetMethod(paramAttr.AutoCompleteFunctionName);

                        if (method == null)
                            throw badAutoCompleteFunctionException;

                        isAutoCompleteFunctionAsync = method.IsAsync();

                        // make sure it only has the one AutoCompleteContext argument

                        var parameters = method.GetParameters();
                        if (parameters.Count() == 1)
                        {
                            if (!typeof(AutoCompleteContext).IsAssignableFrom(parameters[0].ParameterType))
                                throw badAutoCompleteFunctionException;
                        }
                        else
                        {
                            throw badAutoCompleteFunctionException;
                        }

                        // make sure it returns a List<AutoCompleteOption>

                        if (method.ReturnType == typeof(void))
                            throw badAutoCompleteFunctionException;

                        var returnType = method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                            ? method.ReturnType.GetGenericArguments().First()
                            : method.ReturnType;

                        if (returnType != typeof(List<AutoCompleteOption>))
                            throw badAutoCompleteFunctionException;
                    }

                    // add info

                    arguments.Add(new ArgumentInfo
                    {
                        Name = paramAttr.Name ?? property.Name,
                        AutoCompleteFunctionName = paramAttr.AutoCompleteFunctionName,
                        IsAutoCompleteFunctionAsync = isAutoCompleteFunctionAsync,
                        Alias = aliasAttr == null ? default(char) : aliasAttr.Alias,
                        Description = descAttr?.Description,
                        PropertyInfo = new SerializablePropertyInfo(property)
                    });
                }
            }

            return arguments.AsReadOnly();
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
