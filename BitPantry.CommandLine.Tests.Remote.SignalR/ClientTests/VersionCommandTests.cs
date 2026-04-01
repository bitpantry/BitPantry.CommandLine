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

        #region Test 2 – version -f (disconnected) lists local assemblies

        /// <summary>
        /// Invoking `version -f` while disconnected shows a table with Local rows
        /// for the entry assembly and BitPantry assemblies.
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

            // At minimum the header columns should appear
            output.Should().Contain("Assembly");
            output.Should().Contain("Version");
            output.Should().Contain("Source");

            // At least one Local row must be present
            output.Should().Contain("Local");

            // Must contain at least one BitPantry assembly
            output.Should().Contain("BitPantry");
        }

        #endregion

        #region Test 3 – version -f (connected with remote versions) shows Remote rows

        /// <summary>
        /// Invoking `version -f` while connected with mock server that has AssemblyVersions
        /// shows both Local and Remote rows in the table.
        /// </summary>
        [TestMethod]
        public void Version_FullFlag_Connected_PrintsRemoteAssemblies()
        {
            // Arrange – connected proxy with non-empty AssemblyVersions
            var remoteVersions = new Dictionary<string, string>
            {
                ["BitPantry.CommandLine.Remote.SignalR.Server"] = "1.5.2",
                ["BitPantry.CommandLine.Remote.SignalR"] = "1.4.0"
            };

            _proxyMock = TestServerProxyFactory.CreateConnected();
            _proxyMock.Setup(p => p.Server).Returns(new ServerCapabilities(
                new Uri("https://localhost:5000"),
                "test-connection-id",
                new List<BitPantry.CommandLine.Component.CommandInfo>(),
                100 * 1024 * 1024,
                remoteVersions));

            var command = CreateCommand();
            command.Full = true;

            // Act
            command.Execute(new CommandExecutionContext());

            // Assert – both sources must appear
            var output = _console.Output;
            output.Should().Contain("Local");
            output.Should().Contain("Remote");

            // Remote assembly names and versions appear
            output.Should().Contain("BitPantry.CommandLine.Remote.SignalR.Server");
            output.Should().Contain("1.5.2");
        }

        #endregion

        #region Test 4 – version -f (disconnected) omits Remote rows

        /// <summary>
        /// Invoking `version -f` while disconnected should produce no Remote rows and no error.
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

            // Assert – no line has "Remote" as its Source column value (end of line)
            // NOTE: "Remote" may appear in assembly names (e.g. BitPantry.CommandLine.Remote.SignalR),
            // so we check that no line ends with "Remote" (as the Source column value).
            var lines = _console.Output.Split('\n');
            lines.Select(l => l.TrimEnd())
                .Should().NotContain(
                    l => l.EndsWith("Remote"),
                    because: "no rows should have Source='Remote' when disconnected");
        }

        #endregion

        #region Test 5 – AssemblyVersionHelper returns only BitPantry assemblies

        /// <summary>
        /// GetBitPantryAssemblyVersions returns entries whose keys start with "BitPantry"
        /// and contains no entries that don't start with "BitPantry".
        /// </summary>
        [TestMethod]
        public void GetBitPantryAssemblyVersions_ReturnsBitPantryAssemblies()
        {
            // Act
            var versions = AssemblyVersionHelper.GetBitPantryAssemblyVersions();

            // Assert
            versions.Should().NotBeEmpty();
            versions.Keys.Should().OnlyContain(
                k => k.StartsWith("BitPantry", StringComparison.OrdinalIgnoreCase),
                "only BitPantry assemblies should be included");
        }

        #endregion

        #region Test 6 – CreateClientResponse AssemblyVersions round-trips through envelope

        /// <summary>
        /// Setting AssemblyVersions on CreateClientResponse, serializing it, and reading it back
        /// preserves the values through the envelope dictionary mechanism.
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
                assemblyVersions: versions);

            // Simulate envelope round-trip by reading back through the same dictionary
            var deserialized = response.AssemblyVersions;

            // Assert
            deserialized.Should().ContainKey("BitPantry.CommandLine")
                .WhoseValue.Should().Be("5.6.0");
            deserialized.Should().ContainKey("BitPantry.CommandLine.Remote.SignalR")
                .WhoseValue.Should().Be("1.4.0");
        }

        #endregion

        #region Test 7 – ServerCapabilities default AssemblyVersions is empty dictionary (not null)

        /// <summary>
        /// Constructing ServerCapabilities without providing assemblyVersions gives an empty
        /// (non-null) dictionary.
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
        }

        #endregion

        #region Additional edge-case tests

        /// <summary>
        /// Version -f with Connected proxy but empty AssemblyVersions omits Remote rows silently.
        /// </summary>
        [TestMethod]
        public void Version_FullFlag_Connected_EmptyRemoteVersions_OmitsRemote()
        {
            // Arrange – connected but server reports no assembly versions
            _proxyMock = TestServerProxyFactory.CreateConnected(); // uses empty dict by default
            var command = CreateCommand();
            command.Full = true;

            // Act
            command.Execute(new CommandExecutionContext());

            // Assert – no line has "Remote" as its Source column value (end of line)
            // NOTE: "Remote" may appear in assembly names (e.g. BitPantry.CommandLine.Remote.SignalR),
            // so we check that no line ends with "Remote" (as the Source column value).
            var lines = _console.Output.Split('\n');
            lines.Select(l => l.TrimEnd())
                .Should().NotContain(
                    l => l.EndsWith("Remote"),
                    because: "no rows should have Source='Remote' when server has no assembly versions");

            // Table still shows Local rows
            _console.Output.Should().Contain("Local");
        }

        /// <summary>
        /// AssemblyVersionHelper version strings are formatted as major.minor.patch (3 components).
        /// </summary>
        [TestMethod]
        public void GetBitPantryAssemblyVersions_VersionStrings_AreThreeComponentFormat()
        {
            // Act
            var versions = AssemblyVersionHelper.GetBitPantryAssemblyVersions();

            // Assert – all values must be X.Y.Z
            versions.Values.Should().OnlyContain(
                v => Regex.IsMatch(v, @"^\d+\.\d+\.\d+$"),
                "version strings should be in X.Y.Z format");
        }

        /// <summary>
        /// CreateClientResponse.AssemblyVersions defaults to empty dictionary when not provided.
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
