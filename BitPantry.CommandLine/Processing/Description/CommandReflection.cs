using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

                info.Type = commandType;

                // get the command namespace and name

                var cmdAttr = GetAttributes<CommandAttribute>(commandType).SingleOrDefault();

                info.Namespace = cmdAttr?.Namespace;
                info.Name = cmdAttr?.Name ?? commandType.Name;

                // get the description attribute

                info.Description = GetAttributes<DescriptionAttribute>(commandType).SingleOrDefault()?.Description;

                // get parameters

                info.Arguments = GetArgumentInfos(properties);

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

            if (method == null)
                throw new CommandDescriptionException(commandType, "The command must implement a public Execute function - e.g., \"public void Execute(CommandExecutionContext)\"");

            // get if is synch or async

            info.IsExecuteAsync = method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;

            // does command execution context exist - is it a generic

            var parameters = method.GetParameters();
            if (parameters.Count() > 0)
            {
                if (!typeof(CommandExecutionContext).IsAssignableFrom(parameters[0].ParameterType))
                    throw new CommandDescriptionException(commandType, "The Execute function must accept a CommandExecutionContext parameter");

                if (parameters[0].ParameterType.IsGenericType)
                    info.InputType = parameters[0].ParameterType.GetGenericArguments().Single();
            }

            // get return type
           
            if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                info.ReturnType = method.ReturnType.GetGenericArguments().First();
            else if (method.ReturnType != typeof(Task) && method.ReturnType != typeof(void))
                info.ReturnType = method.ReturnType;
        }

        private static IReadOnlyCollection<ArgumentInfo> GetArgumentInfos(PropertyInfo[] properties)
        {
            var arguments = new List<ArgumentInfo>();

            foreach (var property in properties)
            {
                var paramAttr = GetAttributes<ArgumentAttribute>(property).SingleOrDefault();

                if (paramAttr != null)
                {
                    var aliasAttr = GetAttributes<AliasAttribute>(property).SingleOrDefault();
                    var descAttr = GetAttributes<DescriptionAttribute>(property).SingleOrDefault();

                    arguments.Add(new ArgumentInfo
                    {
                        Name = paramAttr.Name ?? property.Name,
                        Alias = aliasAttr == null ? default(char) : aliasAttr.Alias,
                        Description = descAttr?.Description,
                        PropertyInfo = property
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
