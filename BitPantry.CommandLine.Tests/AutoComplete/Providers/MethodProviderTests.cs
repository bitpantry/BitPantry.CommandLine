using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.AutoComplete.Providers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for MethodProvider - MB-001 to MB-010.
/// </summary>
[TestClass]
public class MethodProviderTests
{
    private MethodProvider _sut;
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IExampleService>(new ExampleService("test-value"));
        _serviceProvider = services.BuildServiceProvider();
        
        _sut = new MethodProvider();
    }

    #region MB-001: Method invocation returns completions

    [TestMethod]
    [TestCategory("MB-001")]
    public async Task GetCompletionsAsync_WithMethodReturningStringArray_ReturnsCompletions()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetFormats"),
            CommandInstance = new TestCommandWithFormats(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.Select(i => i.DisplayText).Should().Contain(new[] { "json", "xml", "csv" });
    }

    [TestMethod]
    [TestCategory("MB-001")]
    public async Task GetCompletionsAsync_WithMethodReturningEnumerable_ReturnsCompletions()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetFormatsEnumerable"),
            CommandInstance = new TestCommandWithFormats(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.DisplayText).Should().Contain(new[] { "format1", "format2" });
    }

    #endregion

    #region MB-002: Async method support

    [TestMethod]
    [TestCategory("MB-002")]
    public async Task GetCompletionsAsync_WithAsyncMethod_ReturnsCompletions()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetFormatsAsync"),
            CommandInstance = new TestCommandWithFormats(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.DisplayText).Should().Contain(new[] { "async1", "async2" });
    }

    [TestMethod]
    [TestCategory("MB-002")]
    public async Task GetCompletionsAsync_WithAsyncMethodReturningTask_ReturnsCompletions()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetFormatsAsyncArray"),
            CommandInstance = new TestCommandWithFormats(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.DisplayText).Should().Contain(new[] { "taskAsync1", "taskAsync2" });
    }

    #endregion

    #region MB-003: Wrong return type handling

    [TestMethod]
    [TestCategory("MB-003")]
    public async Task GetCompletionsAsync_WithMethodReturningInt_ReturnsEmptyResult()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetInvalidReturnType"),
            CommandInstance = new TestCommandWithInvalidMethods(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region MB-004: Missing method handling

    [TestMethod]
    [TestCategory("MB-004")]
    public async Task GetCompletionsAsync_WithNonExistentMethod_ReturnsEmptyResult()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("NonExistentMethod"),
            CommandInstance = new TestCommandWithFormats(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region MB-005: DI injection

    [TestMethod]
    [TestCategory("MB-005")]
    public async Task GetCompletionsAsync_WithServiceParameter_InjectsService()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetFormatsWithService"),
            CommandInstance = new TestCommandWithDI(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].DisplayText.Should().Be("test-value");
    }

    #endregion

    #region MB-006: Partial value filtering

    [TestMethod]
    [TestCategory("MB-006")]
    public async Task GetCompletionsAsync_WithPartialValue_FiltersResults()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "js",
            CompletionAttribute = new CompletionAttribute("GetFormats"),
            CommandInstance = new TestCommandWithFormats(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].DisplayText.Should().Be("json");
    }

    #endregion

    #region MB-007: Context parameter injection

    [TestMethod]
    [TestCategory("MB-007")]
    public async Task GetCompletionsAsync_WithContextParameter_InjectsContext()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "format", // Use a prefix that matches the returned value
            CompletionAttribute = new CompletionAttribute("GetFormatsWithContext"),
            CommandInstance = new TestCommandWithContext(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].DisplayText.Should().Be("format-format"); // The method returns "format-{PartialValue}"
    }

    #endregion

    #region MB-008: CompletionItem return type

    [TestMethod]
    [TestCategory("MB-008")]
    public async Task GetCompletionsAsync_WithMethodReturningCompletionItems_PreservesMetadata()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetFormatsAsCompletionItems"),
            CommandInstance = new TestCommandWithCompletionItems(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].DisplayText.Should().Be("json");
        result.Items[0].Description.Should().Be("JSON format");
        result.Items[1].DisplayText.Should().Be("xml");
        result.Items[1].Description.Should().Be("XML format");
    }

    #endregion

    #region MB-009: Static method support

    [TestMethod]
    [TestCategory("MB-009")]
    public async Task GetCompletionsAsync_WithStaticMethod_ReturnsCompletions()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetStaticFormats"),
            CommandInstance = new TestCommandWithStaticMethod(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.DisplayText).Should().Contain(new[] { "static1", "static2" });
    }

    #endregion

    #region MB-010: Method throws exception

    [TestMethod]
    [TestCategory("MB-010")]
    public async Task GetCompletionsAsync_WhenMethodThrows_ReturnsEmptyResult()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetFormatsThrows"),
            CommandInstance = new TestCommandWithInvalidMethods(),
            Services = _serviceProvider
        };

        // Act
        var result = await _sut.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region CanHandle Tests

    [TestMethod]
    public void CanHandle_WithCompletionAttributeAndMethod_ReturnsTrue()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = new CompletionAttribute("GetFormats"),
            CommandInstance = new TestCommandWithFormats()
        };

        // Act
        var result = _sut.CanHandle(context);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public void CanHandle_WithoutCompletionAttribute_ReturnsFalse()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = null,
            CommandInstance = new TestCommandWithFormats()
        };

        // Act
        var result = _sut.CanHandle(context);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void CanHandle_WithStaticValues_ReturnsFalse()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = new CompletionAttribute(new[] { "val1", "val2" }),
            CommandInstance = new TestCommandWithFormats()
        };

        // Act
        var result = _sut.CanHandle(context);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void CanHandle_WithNonArgumentValue_ReturnsFalse()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            CompletionAttribute = new CompletionAttribute("GetFormats"),
            CommandInstance = new TestCommandWithFormats()
        };

        // Act
        var result = _sut.CanHandle(context);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Test Helpers

    public interface IExampleService
    {
        string GetValue();
    }

    public class ExampleService : IExampleService
    {
        private readonly string _value;
        public ExampleService(string value) => _value = value;
        public string GetValue() => _value;
    }

    public class TestCommandWithFormats
    {
        public string[] GetFormats() => new[] { "json", "xml", "csv" };

        public IEnumerable<string> GetFormatsEnumerable()
        {
            yield return "format1";
            yield return "format2";
        }

        public async Task<IEnumerable<string>> GetFormatsAsync()
        {
            await Task.Delay(1);
            return new[] { "async1", "async2" };
        }

        public async Task<string[]> GetFormatsAsyncArray()
        {
            await Task.Delay(1);
            return new[] { "taskAsync1", "taskAsync2" };
        }
    }

    public class TestCommandWithInvalidMethods
    {
        public int GetInvalidReturnType() => 42;

        public string[] GetFormatsThrows() => throw new InvalidOperationException("Test exception");
    }

    public class TestCommandWithDI
    {
        public string[] GetFormatsWithService(IExampleService service) => new[] { service.GetValue() };
    }

    public class TestCommandWithContext
    {
        public string[] GetFormatsWithContext(CompletionContext context)
            => new[] { $"format-{context.PartialValue}" };
    }

    public class TestCommandWithCompletionItems
    {
        public IEnumerable<CompletionItem> GetFormatsAsCompletionItems()
        {
            yield return new CompletionItem
            {
                DisplayText = "json",
                Description = "JSON format",
                InsertText = "json"
            };
            yield return new CompletionItem
            {
                DisplayText = "xml",
                Description = "XML format",
                InsertText = "xml"
            };
        }
    }

    public class TestCommandWithStaticMethod
    {
        public static string[] GetStaticFormats() => new[] { "static1", "static2" };
    }

    #endregion
}
