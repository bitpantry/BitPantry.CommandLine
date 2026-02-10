using System;
using System.Linq;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using BitPantry.CommandLine.Tests.Commands.DescribeCommands;
using BitPantry.CommandLine.Tests.Commands.PositionalCommands;
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
                'y');
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
        public void DescribeCommandWithNoExecute_Exception()
        {
            // Act
            Action act = () => CommandReflection.Describe<NoExecute>();

            // Assert
            act.Should().Throw<CommandDescriptionException>()
                .WithMessage("*The command must implement a public Execute function*");
        }

        [TestMethod]
        public void DescribeCommandInvalidExecuteParametersAsync_Exception()
        {
            // Act
            Action act = () => CommandReflection.Describe<InvalidExecuteParametersAsync>();

            // Assert
            act.Should().Throw<CommandDescriptionException>()
                .WithMessage("*The command must implement a public Execute function with a signature of*");
        }

        [TestMethod]
        public void DescribeCommandInvalidExecuteTooManyParameters_Exception()
        {
            // Act
            Action act = () => CommandReflection.Describe<InvalidExecuteTooManyArgs>();

            // Assert
            act.Should().Throw<CommandDescriptionException>()
                .WithMessage("*The command must implement a public Execute function with a signature of*");
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

        #region Positional Argument Validation Tests (VAL-001 through VAL-012)

        /// <summary>
        /// VAL-001: Valid single positional argument at position 0
        /// </summary>
        [TestMethod]
        public void DescribeCommand_SinglePositionalArg_ArgumentHasCorrectPosition()
        {
            var info = CommandReflection.Describe<SinglePositionalCommand>();
            
            info.Arguments.Should().HaveCount(1);
            var arg = info.Arguments.First();
            arg.Name.Should().Be("Source");
            arg.Position.Should().Be(0);
            arg.IsPositional.Should().BeTrue();
            arg.IsRest.Should().BeFalse();
        }

        /// <summary>
        /// VAL-002: Valid multiple positional arguments at positions 0, 1, 2
        /// </summary>
        [TestMethod]
        public void DescribeCommand_MultiplePositionalArgs_ArgumentsHaveCorrectPositions()
        {
            var info = CommandReflection.Describe<MultiplePositionalCommand>();
            
            info.Arguments.Should().HaveCount(3);
            var args = info.Arguments.OrderBy(a => a.Position).ToList();
            
            args[0].Position.Should().Be(0);
            args[0].Name.Should().Be("First");
            args[1].Position.Should().Be(1);
            args[1].Name.Should().Be("Second");
            args[2].Position.Should().Be(2);
            args[2].Name.Should().Be("Third");
        }

        /// <summary>
        /// VAL-003: Valid IsRest on array type
        /// </summary>
        [TestMethod]
        public void DescribeCommand_IsRestOnArray_IsRestAndIsCollectionTrue()
        {
            var info = CommandReflection.Describe<IsRestCommand>();
            
            info.Arguments.Should().HaveCount(1);
            var arg = info.Arguments.First();
            arg.IsRest.Should().BeTrue();
            arg.IsCollection.Should().BeTrue();
            arg.Position.Should().Be(0);
        }

        /// <summary>
        /// VAL-004: IsRest on scalar type should fail validation
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(PositionalArgumentValidationException))]
        public void DescribeCommand_IsRestOnScalar_ThrowsValidationException()
        {
            CommandReflection.Describe<InvalidIsRestScalarCommand>();
        }

        /// <summary>
        /// VAL-005: IsRest without Position (not positional) should fail validation
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(PositionalArgumentValidationException))]
        public void DescribeCommand_IsRestWithoutPosition_ThrowsValidationException()
        {
            CommandReflection.Describe<InvalidIsRestNotPositionalCommand>();
        }

        /// <summary>
        /// VAL-006: Multiple IsRest arguments should fail validation
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(PositionalArgumentValidationException))]
        public void DescribeCommand_MultipleIsRest_ThrowsValidationException()
        {
            CommandReflection.Describe<InvalidMultipleIsRestCommand>();
        }

        /// <summary>
        /// VAL-007: IsRest not on last positional should fail validation
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(PositionalArgumentValidationException))]
        public void DescribeCommand_IsRestNotLast_ThrowsValidationException()
        {
            CommandReflection.Describe<InvalidIsRestNotLastCommand>();
        }

        /// <summary>
        /// VAL-008: Gap in position indices should fail validation
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(PositionalArgumentValidationException))]
        public void DescribeCommand_GapInPositions_ThrowsValidationException()
        {
            CommandReflection.Describe<InvalidGapPositionCommand>();
        }

        /// <summary>
        /// VAL-009: Duplicate position indices should fail validation
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(PositionalArgumentValidationException))]
        public void DescribeCommand_DuplicatePositions_ThrowsValidationException()
        {
            CommandReflection.Describe<InvalidDuplicatePositionCommand>();
        }

        /// <summary>
        /// VAL-010: Negative position should fail validation
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(PositionalArgumentValidationException))]
        public void DescribeCommand_NegativePosition_ThrowsValidationException()
        {
            CommandReflection.Describe<InvalidNegativePositionCommand>();
        }

        /// <summary>
        /// VAL-011: Mixed positional and named arguments should work correctly
        /// </summary>
        [TestMethod]
        public void DescribeCommand_MixedPositionalAndNamed_BothDescribedCorrectly()
        {
            var info = CommandReflection.Describe<PositionalWithNamedCommand>();
            
            info.Arguments.Should().HaveCount(4);
            
            var positional = info.Arguments.Where(a => a.IsPositional).OrderBy(a => a.Position).ToList();
            var named = info.Arguments.Where(a => !a.IsPositional).ToList();
            
            positional.Should().HaveCount(2);
            positional[0].Name.Should().Be("Source");
            positional[0].Position.Should().Be(0);
            positional[1].Name.Should().Be("Destination");
            positional[1].Position.Should().Be(1);
            
            named.Should().HaveCount(2);
            named.Should().Contain(a => a.Name == "Force");
            named.Should().Contain(a => a.Name == "Mode");
        }

        /// <summary>
        /// VAL-012: Error message should contain command type and property name
        /// </summary>
        [TestMethod]
        public void DescribeCommand_ValidationError_MessageContainsCommandAndPropertyName()
        {
            try
            {
                CommandReflection.Describe<InvalidIsRestScalarCommand>();
                Assert.Fail("Expected PositionalArgumentValidationException");
            }
            catch (PositionalArgumentValidationException ex)
            {
                ex.Message.Should().Contain("InvalidIsRestScalarCommand");
                ex.Message.Should().Contain("SingleFile");
            }
        }

        #endregion
    }
}
