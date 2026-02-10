using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Client
{
    /// <summary>
    /// Handles automatic server connection for single-command execution mode.
    /// When registered and enabled, the server proxy will call EnsureConnectedAsync
    /// before operations that require a connection.
    /// </summary>
    public interface IAutoConnectHandler
    {
        /// <summary>
        /// The profile name to use for auto-connect.
        /// Set from the --profile global argument or left null for default resolution.
        /// </summary>
        string RequestedProfileName { get; set; }

        /// <summary>
        /// Whether auto-connect is enabled. True in single-command execution mode,
        /// false in REPL mode where the user explicitly manages connections.
        /// </summary>
        bool AutoConnectEnabled { get; set; }

        /// <summary>
        /// Ensures a server connection is established, auto-connecting if necessary.
        /// Resolution order: RequestedProfileName → BITPANTRY_PROFILE env var → default profile.
        /// </summary>
        /// <param name="proxy">The server proxy to connect through (passed to avoid circular dependency)</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>True if connected (already or newly); false if no profile available.</returns>
        Task<bool> EnsureConnectedAsync(IServerProxy proxy, CancellationToken token = default);
    }
}
