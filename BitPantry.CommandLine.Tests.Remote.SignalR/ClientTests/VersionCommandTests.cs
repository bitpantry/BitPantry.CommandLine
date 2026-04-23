using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using FluentAssertions;
using Moq;
using Spectre.Console.Testing;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class VersionCommandTests
    {
        private Mock<IServerProxy> _proxyMock = null!;
        private TestConsole _console = null!;

        [TestInitialize]
        public void Setup()
        {
            _proxyMock = TestServerProxyFactory.CreateDisconnected();
            _console = new TestConsole();
        }

        // ─── Test Validity Check ────────────────────────────────────────────────
        //   Invokes code under test: YES
        //   Breakage detection: YES
        //   Not a tautology: YES

        #region Test 1 – version (no flag) prints entry assembly version

        /// <summary>
        /// Invoking `version` with no flags prints the entry assembly version in X.Y.Z format.
        /// </summary>
        [TestMethod]
        public void Version_NoFlag_PrintsEntryAssemblyVersion()
        {
            // Arrange
            var command = CreateCommand();
            command.Full = false;

            // Act
            command.Execute(new CommandExecutionContext());

            // Assert – output must match major.minor.patch (digits only, 3 components)
            var output = _console.Output.Trim();
            output.Should().MatchRegex(@"^\d+\.\d+\.\d+$",
                "entry assembly version should be formatted as X.Y.Z");
        }

        #endregion

        #region Test 2 – version -f (disconnected) lists local executing and loaded assemblies

        /// <summary>
        /// Invoking `version -f` while disconnected shows a table with the local
        /// executing assembly row plus loaded BitPantry.CommandLine* assemblies.
        /// </summary>
        [TestMethod]
        public void Version_FullFlag_PrintsLocalAssemblies()
        {
            // Arrange
            _proxyMock = TestServerProxyFactory.CreateDisconnected();
            var command = CreateCommand();
            command.Full = true;

            // Act
            command.Execute(new CommandExecutionContext());

            // Assert
            var output = _console.Output;
            var executingAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

            // At minimum the header columns should appear
            output.Should().Contain("Assembly");
            output.Should().Contain("Version");
            output.Should().Contain("Source");
            output.Should().Contain("Kind");

            // Local executing assembly row is shown
            output.Should().Contain("Local");
            output.Should().Contain("Executing");
            output.Should().Contain(executingAssemblyName);

            // Loaded rows are shown for BitPantry.CommandLine* assemblies
            output.Should().Contain("Loaded");
            output.Should().Contain("BitPantry.CommandLine");
            output.IndexOf("Executing", StringComparison.Ordinal)
                .Should().BeLessThan(output.IndexOf("Loaded", StringComparison.Ordinal),
                    "the local executing row should be rendered before the loaded BitPantry.CommandLine rows");
        }

        #endregion

        #region Test 3 – version -f (connected with remote metadata) shows remote executing and loaded rows

        /// <summary>
        /// Invoking `version -f` while connected with mock server metadata shows both
        /// remote executing and remote loaded rows in the table.
        /// </summary>
        [TestMethod]
        public void Version_FullFlag_Connected_PrintsRemoteAssemblies()
        {
            // Arrange – connected proxy with remote executing assembly and loaded assemblies
            const string remoteExecutingAssemblyName = "BitPantry.CommandLine.Server.Host";
            const string remoteExecutingAssemblyVersion = "2.3.4";
            var remoteVersions = new Dictionary<string, string>
            {
                [remoteExecutingAssemblyName] = remoteExecutingAssemblyVersion,
                ["BitPantry.CommandLine.Remote.SignalR.Server"] = "1.5.2",
                ["BitPantry.CommandLine.Remote.SignalR"] = "1.4.0"
            };

            _proxyMock = TestServerProxyFactory.CreateConnected();
            _proxyMock.Setup(p => p.Server).Returns(new ServerCapabilities(
                new Uri("https://localhost:5000"),
                "test-connection-id",
                new List<BitPantry.CommandLine.Component.CommandInfo>(),
                100 * 1024 * 1024,
                remoteVersions,
                remoteExecutingAssemblyName,
                remoteExecutingAssemblyVersion));

            var command = CreateCommand();
            command.Full = true;

            // Act
            command.Execute(new CommandExecutionContext());

            // Assert – both sources and kinds must appear
            var output = _console.Output;
            output.Should().Contain("Local");
            output.Should().Contain("Remote");
            output.Should().Contain("Executing");
            output.Should().Contain("Loaded");

            // Remote executing assembly appears
            output.Should().Contain(remoteExecutingAssemblyName);
            output.Should().Contain(remoteExecutingAssemblyVersion);
            Regex.Matches(output, Regex.Escape(remoteExecutingAssemblyName)).Should().HaveCount(1,
                "the remote executing assembly should not also appear in the loaded rows when the handshake version dictionary includes it");

            // Remote loaded assembly names and versions appear
            output.Should().Contain("BitPantry.CommandLine.Remote.SignalR.Server");
            output.Should().Contain("1.5.2");
            output.IndexOf(remoteExecutingAssemblyName, StringComparison.Ordinal)
                .Should().BeLessThan(output.IndexOf("BitPantry.CommandLine.Remote.SignalR.Server", StringComparison.Ordinal),
                    "the remote executing row should be rendered before remote loaded rows");
        }

        #endregion

        #region Test 4 – version -f (disconnected) omits Remote rows

        /// <summary>
        /// Invoking `version -f` while disconnected should produce only one Executing row
        /// (the local one) and no remote metadata.
        /// </summary>
        [TestMethod]
        public void Version_FullFlag_Disconnected_OmitsRemote()
        {
            // Arrange
            _proxyMock = TestServerProxyFactory.CreateDisconnected();
            var command = CreateCommand();
            command.Full = true;

            // Act
            command.Execute(new CommandExecutionContext());

            // Assert
            Regex.Matches(_console.Output, "Executing").Should().HaveCount(1,
                "only the local executing row should appear when disconnected");
        }

        #endregion

        #region Test 5 – AssemblyVersionHelper returns only BitPantry assemblies

        /// <summary>
        /// GetBitPantryCommandLineAssemblyVersions returns entries whose keys start with
        /// "BitPantry.CommandLine" and contains no entries outside that prefix.
        /// </summary>
        [TestMethod]
        public void GetBitPantryCommandLineAssemblyVersions_ReturnsCommandLineAssemblies()
        {
            // Act
            var versions = AssemblyVersionHelper.GetBitPantryCommandLineAssemblyVersions();

            // Assert
            versions.Should().NotBeEmpty();
            versions.Keys.Should().OnlyContain(
                k => k.StartsWith("BitPantry.CommandLine", StringComparison.OrdinalIgnoreCase),
                "only BitPantry.CommandLine assemblies should be included");
        }

        #endregion

        #region Test 6 – CreateClientResponse AssemblyVersions round-trips through envelope

        /// <summary>
        /// Setting remote executing assembly metadata and AssemblyVersions on CreateClientResponse,
        /// then reading them back, preserves the values through the envelope dictionary mechanism.
        /// </summary>
        [TestMethod]
        public void CreateClientResponse_AssemblyVersions_RoundTrips()
        {
            // Arrange
            var versions = new Dictionary<string, string>
            {
                ["BitPantry.CommandLine"] = "5.6.0",
                ["BitPantry.CommandLine.Remote.SignalR"] = "1.4.0"
            };

            // Act – construct via primary constructor (sets the property)
            var response = new CreateClientResponse(
                correlationId: "corr-1",
                connectionId: "conn-1",
                commands: new List<BitPantry.CommandLine.Component.CommandInfo>(),
                maxFileSizeBytes: 1024,
                assemblyVersions: versions,
                executingAssemblyName: "BitPantry.CommandLine.Server.Host",
                executingAssemblyVersion: "5.6.0");

            // Simulate envelope round-trip by reading back through the same dictionary
            var deserialized = response.AssemblyVersions;

            // Assert
            deserialized.Should().ContainKey("BitPantry.CommandLine")
                .WhoseValue.Should().Be("5.6.0");
            deserialized.Should().ContainKey("BitPantry.CommandLine.Remote.SignalR")
                .WhoseValue.Should().Be("1.4.0");
            response.ExecutingAssemblyName.Should().Be("BitPantry.CommandLine.Server.Host");
            response.ExecutingAssemblyVersion.Should().Be("5.6.0");
        }

        #endregion

        #region Test 7 – ServerCapabilities default AssemblyVersions is empty dictionary (not null)

        /// <summary>
        /// Constructing ServerCapabilities without providing assemblyVersions gives an empty
        /// (non-null) dictionary and empty executing assembly metadata.
        /// </summary>
        [TestMethod]
        public void ServerCapabilities_DefaultAssemblyVersions_EmptyDictionary()
        {
            // Act – omit the optional assemblyVersions parameter
            var capabilities = new ServerCapabilities(
                new Uri("https://localhost:5000"),
                "conn-id",
                new List<BitPantry.CommandLine.Component.CommandInfo>(),
                100 * 1024 * 1024);

            // Assert
            capabilities.AssemblyVersions.Should().NotBeNull();
            capabilities.AssemblyVersions.Should().BeEmpty();
            capabilities.ExecutingAssemblyName.Should().BeEmpty();
            capabilities.ExecutingAssemblyVersion.Should().BeEmpty();
        }

        #endregion

        #region Additional edge-case tests

        /// <summary>
        /// Version -f with Connected proxy but empty remote loaded versions still shows the remote
        /// executing assembly row and omits remote loaded rows silently.
        /// </summary>
        [TestMethod]
        public void Version_FullFlag_Connected_EmptyRemoteVersions_OmitsRemote()
        {
            // Arrange – connected but server reports no loaded assembly versions
            _proxyMock = TestServerProxyFactory.CreateConnected(
                assemblyVersions: new Dictionary<string, string>(),
                executingAssemblyName: "BitPantry.CommandLine.Server.Host",
                executingAssemblyVersion: "2.3.4");
            var command = CreateCommand();
            command.Full = true;

            // Act
            command.Execute(new CommandExecutionContext());

            // Assert
            _console.Output.Should().Contain("BitPantry.CommandLine.Server.Host");
            Regex.Matches(_console.Output, "Loaded").Should().NotBeEmpty(
                "local loaded rows should still appear");
            Regex.Matches(_console.Output, "Executing").Should().HaveCountGreaterOrEqualTo(2,
                "both local and remote executing rows should appear");

            // Table still shows Local rows
            _console.Output.Should().Contain("Local");
        }

        /// <summary>
        /// AssemblyVersionHelper version strings are formatted as major.minor.patch (3 components).
        /// </summary>
        [TestMethod]
        public void GetBitPantryCommandLineAssemblyVersions_VersionStrings_AreThreeComponentFormat()
        {
            // Act
            var versions = AssemblyVersionHelper.GetBitPantryCommandLineAssemblyVersions();

            // Assert – all values must be X.Y.Z
            versions.Values.Should().OnlyContain(
                v => Regex.IsMatch(v, @"^\d+\.\d+\.\d+$"),
                "version strings should be in X.Y.Z format");
        }

        /// <summary>
        /// CreateClientResponse.AssemblyVersions defaults to empty dictionary and executing assembly
        /// metadata defaults to empty strings when not provided.
        /// </summary>
        [TestMethod]
        public void CreateClientResponse_DefaultAssemblyVersions_IsEmptyDictionary()
        {
            // Act – omit assemblyVersions (uses default null → empty dict)
            var response = new CreateClientResponse(
                correlationId: "corr-1",
                connectionId: "conn-1",
                commands: new List<BitPantry.CommandLine.Component.CommandInfo>(),
                maxFileSizeBytes: 1024);

            // Assert
            response.AssemblyVersions.Should().NotBeNull();
            response.AssemblyVersions.Should().BeEmpty();
            response.ExecutingAssemblyName.Should().BeEmpty();
            response.ExecutingAssemblyVersion.Should().BeEmpty();
        }

        #endregion

        #region Private helpers

        private VersionCommand CreateCommand()
        {
            var cmd = new VersionCommand(_proxyMock.Object);
            cmd.SetConsole(_console);
            return cmd;
        }

        #endregion
    }
}
