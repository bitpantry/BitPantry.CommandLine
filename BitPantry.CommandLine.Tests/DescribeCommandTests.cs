using System;
using System.Linq;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using BitPantry.CommandLine.Tests.Commands.DescribeCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class DescribeCommandTests
    {
        [TestMethod]
        public void DescribeMinimalCommand_CommandDescribed()
        {
            ValidateCommandDescription<Minimal>();
        }

        [TestMethod]
        public void DescribeCommandAttribute_CommandNamePopulated()
        {
            ValidateCommandDescription<CommandAttributeCmd>("NewName");
        }

        [TestMethod]
        public void DescribeCommandNamespace_CommandNamespacePopulated()
        {
            ValidateCommandDescription<CommandWithNamespace>("BitPantry", "NewName");
        }

        [TestMethod]
        public void DescribeCommandNamespaceNoName_CommandNamespacePopulated()
        {
            ValidateCommandDescription<CommandWithNamespaceNoName>("BitPantry", nameof(CommandWithNamespaceNoName));
        }

        [TestMethod]
        public void DescribeEmptyCommandAttribute_CommandNameIsClassName()
        {
            ValidateCommandDescription<EmptyCommandAttribute>();
        }

        [TestMethod]
        public void DescribeWithNonDescribableProperties_PropertiesNotDescribed()
        {
            ValidateCommandDescription<NonDescribableProperties>();
        }

        [TestMethod]
        public void DescribeWithOneEmptyParameter_ParameterDescribed()
        {
            var parameters = CommandReflection.Describe<EmptyArgumentAttribute>().Arguments;
            ValidateParameterDescription<string>(parameters.First(), "TestArg");
        }

        [TestMethod]
        public void DescribeWithOneParameter_ParameterDescribed()
        {
            var parameters = CommandReflection.Describe<ArgumentAttributeCmd>().Arguments;
            ValidateParameterDescription<int>(parameters.First(), "MyName");
        }

        [TestMethod]
        public void DescribeParameterWithAliases_AliasesDescribed()
        {
            var parameters = CommandReflection.Describe<ArgumentWithAlias>().Arguments;
            ValidateParameterDescription<int>(
                parameters.First(),
                "MyProperty",
                'p');
        }

        [TestMethod]
        public void DescribeCommandAcceptsNone_Described()
        {
            var type = CommandReflection.Describe<Minimal>().InputType;

            type.Should().BeNull(); 
        }

        [TestMethod]
        public void DescribeCommandAcceptsString_Described()
        {
            var type = CommandReflection.Describe<AcceptsTypeString>().InputType;

            type.Should().Be<string>();
        }

        [TestMethod]
        [ExpectedException(typeof(CommandDescriptionException))]
        public void DescribeCommandOfBadCommandName_Exception()
        {
            CommandReflection.Describe<BadCommandName>();
        }

        [TestMethod]
        [ExpectedException(typeof(CommandDescriptionException))]
        public void DescribeCommandOfBadNamespace_InvalidChars_Exception()
        {
            CommandReflection.Describe<BadNamespace_InvalidChars>();
        }

        [TestMethod]
        [ExpectedException(typeof(CommandDescriptionException))]
        public void DescribeCommandOfBadNamespace_Spaces_Exception()
        {
            CommandReflection.Describe<BadNamespace_Spaces>();
        }

        [TestMethod]
        [ExpectedException(typeof(CommandDescriptionException))]
        public void DescribeCommandOfBadNamespace_EmptySegment_Exception()
        {
            CommandReflection.Describe<BadNamespace_EmptySegment>();
        }

        [TestMethod]
        [ExpectedException(typeof(CommandDescriptionException))]
        public void DescribeCommandOfWrongBaseType_Exception()
        {
            CommandReflection.Describe<CommandWithWrongBase>();
        }

        [TestMethod]
        [ExpectedException(typeof(CommandDescriptionException))]
        public void DescribeCommandWithNoExecute_Exception()
        {
            try
            {
                CommandReflection.Describe<NoExecute>();
            }
            catch (CommandDescriptionException ex)
            {
                ex.InnerException.Should().NotBeNull();
                ex.InnerException.Message.Should().Contain("The command must implement a public Execute function");

                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CommandDescriptionException))]
        public void DescribeCommandInvalidExecuteParametersAsync_Exception()
        {
            try
            {
                CommandReflection.Describe<InvalidExecuteParametersAsync>();
            }
            catch (CommandDescriptionException ex)
            {
                ex.InnerException.Should().NotBeNull();
                ex.InnerException.Message.Should().Contain("The Execute function must accept a CommandExecutionContext parameter");

                throw;
            }
        }

        [TestMethod]
        public void DescribeCommandWithReturnType_Described()
        {
            ValidateCommandDescription<ReturnType, string>();
        }

        [TestMethod]
        public void DescribeCommandWithReturnTypeAsync_Described()
        {
            ValidateCommandDescription<ReturnTypeAsync>();
        }

        [TestMethod]
        public void DescribeCommandWithReturnTypeAsyncGeneric_Described()
        {
            ValidateCommandDescription<ReturnTypeAsyncGeneric, string>();
        }

        private CommandInfo ValidateCommandDescription<T, TReturn>(
            string name = null)
            => ValidateCommandDescription<T, TReturn>(null, name);

        private CommandInfo ValidateCommandDescription<T, TReturn>(
            string @namespace, string name = null)
        {
            var info = ValidateCommandDescription_CORE<T>(@namespace, name);
            info.ReturnType.Should().Be<TReturn>();

            return info;
        }

        private CommandInfo ValidateCommandDescription<T>(
            string name = null)
            => ValidateCommandDescription<T>(null, name);
        
        private CommandInfo ValidateCommandDescription<T>(
            string @namespace, string name = null)
        {
            var info = ValidateCommandDescription_CORE<T>(@namespace, name);
            info.ReturnType.Should().Be(typeof(void));

            return info;
        }

        private CommandInfo ValidateCommandDescription_CORE<T>(
            string @namespace, string name = null)
        {
            var info = CommandReflection.Describe<T>();

            info.Should().NotBeNull();
            info.Type.Should().Be<T>();
            info.Description.Should().BeNull();
            info.Name.Should().Be(name ?? typeof(T).Name);
            info.Arguments.Should().BeEmpty();
            info.Namespace.Should().Be(@namespace);

            return info;
        }

        private ArgumentInfo ValidateParameterDescription<TDataType>(
            ArgumentInfo info,
            string name,
            char alias = default(char))
        {
            info.Name.Should().Be(name);
            info.DataType.Should().Be<TDataType>();
            info.Alias.Should().Be(alias);      

            return info;
        }
    }
}
