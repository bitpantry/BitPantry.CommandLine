using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
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
/// Tests for provider resolution and routing - PR-001 to PR-006.
/// Tests that the correct provider is selected based on context.
/// </summary>
[TestClass]
public class ProviderResolutionTests
{
    private List<ICompletionProvider> _providers;
    private IServiceProvider _serviceProvider;
    private MockFileSystem _mockFileSystem;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
        _mockFileSystem = new MockFileSystem();

        // Create providers in priority order (high to low)
        _providers = new List<ICompletionProvider>
        {
            new MethodProvider(), // 75
            new StaticValuesProvider(), // 70
            new EnumProvider(), // 65
            new FilePathProvider(_mockFileSystem), // 60
            new DirectoryPathProvider(_mockFileSystem) // 61 (intentionally higher than File for dir-only)
        };
    }

    #region PR-001: Method provider takes priority over static values

    [TestMethod]
    [TestCategory("PR-001")]
    public void CanHandle_MethodAttribute_MethodProviderHandles()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = new CompletionAttribute("GetFormats"),
            CommandInstance = new TestCommand(),
            Services = _serviceProvider
        };

        var methodProvider = _providers.OfType<MethodProvider>().First();
        var staticProvider = _providers.OfType<StaticValuesProvider>().First();

        // Act & Assert
        methodProvider.CanHandle(context).Should().BeTrue();
        staticProvider.CanHandle(context).Should().BeFalse();
    }

    #endregion

    #region PR-002: Static values provider handles array attribute

    [TestMethod]
    [TestCategory("PR-002")]
    public void CanHandle_StaticValuesAttribute_StaticValuesProviderHandles()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = new CompletionAttribute(new[] { "val1", "val2" }),
            CommandInstance = new TestCommand(),
            Services = _serviceProvider
        };

        var staticProvider = _providers.OfType<StaticValuesProvider>().First();
        var methodProvider = _providers.OfType<MethodProvider>().First();

        // Act & Assert
        staticProvider.CanHandle(context).Should().BeTrue();
        methodProvider.CanHandle(context).Should().BeFalse();
    }

    #endregion

    #region PR-003: Enum provider handles enum types

    [TestMethod]
    [TestCategory("PR-003")]
    public void CanHandle_EnumPropertyType_EnumProviderHandles()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(DayOfWeek),
            CompletionAttribute = null,
            CommandInstance = new TestCommand(),
            Services = _serviceProvider
        };

        var enumProvider = _providers.OfType<EnumProvider>().First();

        // Act & Assert
        enumProvider.CanHandle(context).Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("PR-003")]
    public void CanHandle_NonEnumPropertyType_EnumProviderDoesNotHandle()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(string),
            CompletionAttribute = null,
            CommandInstance = new TestCommand(),
            Services = _serviceProvider
        };

        var enumProvider = _providers.OfType<EnumProvider>().First();

        // Act & Assert
        enumProvider.CanHandle(context).Should().BeFalse();
    }

    #endregion

    #region PR-004: File path provider handles file path attribute

    [TestMethod]
    [TestCategory("PR-004")]
    public void CanHandle_FilePathAttribute_FilePathProviderHandles()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = new FilePathCompletionAttribute(),
            CommandInstance = new TestCommand(),
            Services = _serviceProvider
        };

        var fileProvider = _providers.OfType<FilePathProvider>().First();

        // Act & Assert
        fileProvider.CanHandle(context).Should().BeTrue();
    }

    #endregion

    #region PR-005: Directory path provider handles directory path attribute

    [TestMethod]
    [TestCategory("PR-005")]
    public void CanHandle_DirectoryPathAttribute_DirectoryPathProviderHandles()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = new DirectoryPathCompletionAttribute(),
            CommandInstance = new TestCommand(),
            Services = _serviceProvider
        };

        var dirProvider = _providers.OfType<DirectoryPathProvider>().First();

        // Act & Assert
        dirProvider.CanHandle(context).Should().BeTrue();
    }

    #endregion

    #region PR-006: Provider priority order

    [TestMethod]
    [TestCategory("PR-006")]
    public void Priority_ProvidersOrderedCorrectly()
    {
        // Verify the expected priority order
        var orderedProviders = _providers.OrderByDescending(p => p.Priority).ToList();

        // Method should be highest (75), then Static (70), then Enum (65),
        // then Directory (61), then File (60)
        orderedProviders[0].Should().BeOfType<MethodProvider>();
        orderedProviders[1].Should().BeOfType<StaticValuesProvider>();
        orderedProviders[2].Should().BeOfType<EnumProvider>();
        orderedProviders[3].Should().BeOfType<DirectoryPathProvider>();
        orderedProviders[4].Should().BeOfType<FilePathProvider>();
    }

    [TestMethod]
    [TestCategory("PR-006")]
    public void Priority_MethodProviderHasHighestPriority()
    {
        var methodProvider = _providers.OfType<MethodProvider>().First();
        methodProvider.Priority.Should().Be(75);
    }

    [TestMethod]
    [TestCategory("PR-006")]
    public void Priority_StaticValuesProviderHasCorrectPriority()
    {
        var staticProvider = _providers.OfType<StaticValuesProvider>().First();
        staticProvider.Priority.Should().Be(70);
    }

    [TestMethod]
    [TestCategory("PR-006")]
    public void Priority_EnumProviderHasCorrectPriority()
    {
        var enumProvider = _providers.OfType<EnumProvider>().First();
        enumProvider.Priority.Should().Be(65);
    }

    #endregion

    #region Integration: First matching provider wins

    [TestMethod]
    public async Task GetCompletions_MultipleCanHandle_HighestPriorityWins()
    {
        // Arrange - context that both Method and Enum could handle
        // (if method existed for enum type)
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(TestEnum),
            CompletionAttribute = null, // No method, so enum should handle
            CommandInstance = new TestCommand(),
            Services = _serviceProvider
        };

        // Order providers by priority
        var orderedProviders = _providers.OrderByDescending(p => p.Priority).ToList();

        // Find first provider that can handle
        ICompletionProvider selectedProvider = null;
        foreach (var provider in orderedProviders)
        {
            if (provider.CanHandle(context))
            {
                selectedProvider = provider;
                break;
            }
        }

        // Assert
        selectedProvider.Should().NotBeNull();
        selectedProvider.Should().BeOfType<EnumProvider>();

        // Verify it returns correct values
        var result = await selectedProvider.GetCompletionsAsync(context, CancellationToken.None);
        result.Items.Should().HaveCountGreaterThan(0);
        result.Items.Select(i => i.DisplayText).Should().Contain(new[] { "Value1", "Value2", "Value3" });
    }

    #endregion

    #region FB-001: FilePathProvider acts as fallback for unattributed arguments

    /// <summary>
    /// Documents current behavior: FilePathProvider.CanHandle returns true for ANY ArgumentValue,
    /// acting as a fallback when no specific completion attribute is present.
    /// 
    /// This test reproduces the issue where typing "server connect --Profile " and pressing Tab
    /// shows file system completions because:
    /// 1. ProfileNameProvider returns empty (no profiles saved)
    /// 2. FilePathProvider.CanHandle returns true for any ArgumentValue
    /// 3. FilePathProvider returns file system completions as "fallback"
    /// 
    /// This may be intentional (like Windows Terminal behavior) or a bug to fix.
    /// </summary>
    [TestMethod]
    [TestCategory("FB-001")]
    public void CanHandle_NoCompletionAttribute_FilePathProviderStillHandles()
    {
        // Arrange - context with NO completion attribute (like --Profile with no ProfileNameProvider match)
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = null,  // No attribute
            PropertyType = typeof(string),  // Not an enum
            CommandInstance = new TestCommand(),
            Services = _serviceProvider
        };

        var fileProvider = _providers.OfType<FilePathProvider>().First();
        var dirProvider = _providers.OfType<DirectoryPathProvider>().First();

        // Act & Assert - BOTH path providers say they can handle (current behavior)
        fileProvider.CanHandle(context).Should().BeTrue("FilePathProvider acts as fallback for any ArgumentValue");
        dirProvider.CanHandle(context).Should().BeTrue("DirectoryPathProvider acts as fallback for any ArgumentValue");
    }

    [TestMethod]
    [TestCategory("FB-001")]
    public async Task GetCompletions_NoAttribute_FilePathProviderReturnsFileSystemResults()
    {
        // Arrange - set up mock file system with some files
        _mockFileSystem.AddFile(@"C:\work\config.json", new MockFileData("{}"));
        _mockFileSystem.AddFile(@"C:\work\data.txt", new MockFileData("data"));
        _mockFileSystem.AddDirectory(@"C:\work\logs");
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = null,  // No completion attribute
            PropertyType = typeof(string),
            PartialValue = "",
            CurrentWord = "",
            Services = _serviceProvider
        };

        var fileProvider = _providers.OfType<FilePathProvider>().First();

        // Act
        var result = await fileProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert - file system results are returned as fallback
        result.Items.Should().NotBeEmpty("FilePathProvider provides file system completions as fallback");
        result.Items.Should().Contain(i => i.DisplayText.Contains("config.json") || i.InsertText.Contains("config.json"));
    }

    #endregion

    #region FB-002: Custom provider returns empty, fallback kicks in

    /// <summary>
    /// Tests the scenario where a custom provider (like ProfileNameProvider) returns empty
    /// and the FilePathProvider kicks in as fallback.
    /// </summary>
    [TestMethod]
    [TestCategory("FB-002")]
    public async Task Fallback_CustomProviderReturnsEmpty_FilePathProviderKicksIn()
    {
        // Arrange - set up mock file system
        _mockFileSystem.AddDirectory(@"C:\work\bin");
        _mockFileSystem.AddDirectory(@"C:\work\obj");
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        // Add a mock custom provider that returns empty (simulating ProfileNameProvider with no profiles)
        var emptyProvider = new EmptyCustomProvider();
        var testProviders = new List<ICompletionProvider>
        {
            emptyProvider,  // Priority 80 - higher than FilePathProvider
            new FilePathProvider(_mockFileSystem)  // Priority 60
        };

        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CompletionAttribute = new CompletionAttribute(typeof(EmptyCustomProvider)),
            PropertyType = typeof(string),
            PartialValue = "",
            CurrentWord = "",
            Services = _serviceProvider
        };

        // Act - iterate through providers in priority order (simulating orchestrator behavior)
        CompletionResult finalResult = CompletionResult.Empty;
        foreach (var provider in testProviders.OrderByDescending(p => p.Priority))
        {
            if (provider.CanHandle(context))
            {
                var result = await provider.GetCompletionsAsync(context, CancellationToken.None);
                if (result.Items.Count > 0)
                {
                    finalResult = result;
                    break;
                }
                // If empty, continue to next provider (fallback behavior)
            }
        }

        // Assert - FilePathProvider's results are used as fallback
        finalResult.Items.Should().NotBeEmpty("FilePathProvider provides fallback when custom provider returns empty");
        finalResult.Items.Should().Contain(i => 
            i.DisplayText.Contains("bin") || i.DisplayText.Contains("obj"));
    }

    /// <summary>
    /// Mock custom provider that always returns empty (simulates ProfileNameProvider with no saved profiles).
    /// </summary>
    private class EmptyCustomProvider : ICompletionProvider
    {
        public int Priority => 80;  // Higher priority than FilePathProvider (60)

        public bool CanHandle(CompletionContext context)
        {
            return context.ElementType == CompletionElementType.ArgumentValue &&
                   context.CompletionAttribute?.ProviderType == typeof(EmptyCustomProvider);
        }

        public Task<CompletionResult> GetCompletionsAsync(CompletionContext context, CancellationToken cancellationToken)
        {
            // Returns empty - simulating ProfileNameProvider with no profiles
            return Task.FromResult(CompletionResult.Empty);
        }
    }

    #endregion

    #region Test Helpers

    public class TestCommand
    {
        public string[] GetFormats() => new[] { "json", "xml" };
    }

    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    #endregion
}
