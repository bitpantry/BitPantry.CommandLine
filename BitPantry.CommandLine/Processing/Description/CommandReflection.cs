﻿using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
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
                    throw new CommandReflectionException(commandType, $"The command type must extend type {typeof(CommandBase).FullName}");

                info.Type = commandType;

                // get the command name

                info.Name = GetAttributes<CommandAttribute>(commandType).SingleOrDefault()?.Name ?? commandType.Name;

                // get the description attribute

                info.Description = GetAttributes<DescriptionAttribute>(commandType).SingleOrDefault()?.Description;

                // get parameters

                info.Arguments = GetArgumentInfos(properties);

                // get execution function

                info.IsExecuteAsync = ValidateExecutionFunction(commandType);

                return info;
            }
            catch(Exception ex)
            {
                throw new CommandReflectionException(commandType, "An error occured while describing the command type", ex);
            }
        }

        // Derived from - https://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.asyncstatemachineattribute.aspx
        private static bool ValidateExecutionFunction(Type commandType)
        {
            var method = commandType.GetMethod("Execute");

            if (method == null)
                throw new Exception("The command must implement a public Execute function - \"public int Execute(CommandExecutionContext)\" or \"public async Task<int> Execute(CommandExecutionContext)\"");

            bool isAsync = method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;

            var parameters = method.GetParameters();
            if (parameters.Count() != 1 || parameters[0].ParameterType != typeof(CommandExecutionContext))
                throw new Exception("The Execute function must accept a CommandExecutionContext parameter");

            if (isAsync && method.ReturnType != typeof(Task<int>))
                throw new Exception("The async Execute function must return a Task<int>. Use \"public int Execute()\" for asynchronous exception");
            else if (!isAsync && method.ReturnType != typeof(int))
                throw new Exception("the Execute function must return an int.");

            return isAsync;
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
