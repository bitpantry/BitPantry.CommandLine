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
        public void DescribeCommandGroup_CommandGroupPopulated()
        {
            var info = CommandReflection.Describe<CommandWithGroup>();
            info.Name.Should().Be("NewName");
            // Note: Group is associated during registration, not during reflection
        }

        [TestMethod]
        public void DescribeCommandGroupNoName_CommandNameIsClassName()
        {
            var info = CommandReflection.Describe<CommandWithGroupNoName>();
            info.Name.Should().Be(nameof(CommandWithGroupNoName));
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

        // Note: Bad namespace validation tests removed - namespace no longer exists.
        // Group validation will be done during registry registration, not during reflection.

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
                ex.InnerException.Message.Should().Contain("The command must implement a public Execute function with a signature of");

                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CommandDescriptionException))]
        public void DescribeCommandInvalidExecuteTooManyParameters_Exception()
        {
            try
            {
                CommandReflection.Describe<InvalidExecuteTooManyArgs>();
            }
            catch (CommandDescriptionException ex)
            {
                ex.InnerException.Should().NotBeNull();
                ex.InnerException.Message.Should().Contain("The command must implement a public Execute function with a signature of");

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

        private CommandInfo ValidateCommandDescription<T, TReturn>(string name = null)
        {
            var info = ValidateCommandDescription_CORE<T>(name);
            info.ReturnType.Should().Be<TReturn>();

            return info;
        }

        private CommandInfo ValidateCommandDescription<T>(
            string name = null)
        {
            var info = ValidateCommandDescription_CORE<T>(name);
            info.ReturnType.Should().Be(typeof(void));

            return info;
        }

        private CommandInfo ValidateCommandDescription_CORE<T>(string name = null)
        {
            var info = CommandReflection.Describe<T>();

            info.Should().NotBeNull();
            info.Type.Should().Be<T>();
            info.Description.Should().BeNull();
            info.Name.Should().Be(name ?? typeof(T).Name);
            info.Arguments.Should().BeEmpty();
            // Note: Group is not set during Describe - it's set during registry registration

            return info;
        }

        private ArgumentInfo ValidateParameterDescription<TDataType>(
            ArgumentInfo info,
            string name,
            char alias = default(char))
        {
            info.Name.Should().Be(name);
            info.PropertyInfo.GetPropertyInfo().PropertyType.Should().Be<TDataType>();
            info.Alias.Should().Be(alias);      

            return info;
        }
    }
}
