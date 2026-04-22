using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

/// <summary>
/// Bare-bones SignalR tests that incrementally add complexity to isolate
/// the interactive prompt hang. Each step builds on the previous one.
/// No production CommandLine code is used — only raw SignalR.
///
/// Findings:
///   - Steps 1-4: Basic SignalR RPC round-trip works fine with async hub methods.
///   - Step 5a: Context.Items-scoped state (like production RpcMessageRegistry) works fine.
///   - Step 5b: Sync-over-async (.GetAwaiter().GetResult()) on the hub dispatch thread
///     deadlocks because SignalR cannot dispatch the ReceiveResponse call while the
///     hub method is blocking. MaximumParallelInvocationsPerClient does NOT help.
///
/// Fix: Wrap the entire hub method body in Task.Run, capturing scoped dependencies
/// (Clients.Caller, Context.Items, Context.ConnectionId) before the Task.Run.
/// This offloads execution to a thread pool thread, freeing the SignalR dispatch
/// thread to process incoming ReceiveResponse calls. The hub method awaits the
/// Task.Run, keeping the hub instance alive for the duration.
///
/// Production application: Apply the same pattern in CommandLineHub.ReceiveRequest —
/// capture scoped deps, then await Task.Run(() => body). This is more defensive than
/// fixing only SignalRAnsiInput.ReadKey, as it protects against any future sync-over-async
/// in any command.
/// </summary>
[TestClass]
public class BareBonesSignalRTests
{
    // ──────────────────────────────────────────────
    // Shared diagnostic log for all tests
    // ──────────────────────────────────────────────

    private static readonly ConcurrentQueue<string> _log = new();
    private static void Log(string msg) => _log.Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
    private string GetDiagnostics() => string.Join("\n", _log);

    [TestInitialize]
    public void ClearLog() => _log.Clear();

    // ──────────────────────────────────────────────
    // Minimal hub — just enough for each step
    // ──────────────────────────────────────────────

    /// <summary>
    /// Shared state visible to hub instances (hubs are transient).
    /// </summary>
    public class HubState
    {
        public ConcurrentQueue<string> ReceivedMessages { get; } = new();
        public TaskCompletionSource<string> ResponseReceived { get; set; } = new();
    }

    /// <summary>
    /// Minimal hub: 
    ///   - Ping: client→server (step 1)
    ///   - ReceiveResponse: client→server response delivery (step 3+)
    ///   - StartRpc: server calls client, waits for response (step 4+)
    /// </summary>
    public class TestHub : Hub
    {
        private readonly HubState _state;

        public TestHub(HubState state) => _state = state;

        /// <summary>Step 1: Simple client→server call</summary>
        public string Ping(string message)
        {
            Log($"[HUB] Ping received: {message}");
            _state.ReceivedMessages.Enqueue(message);
            return $"pong:{message}";
        }

        /// <summary>Step 2: Server calls client back during a hub method</summary>
        public async Task<string> PingWithCallback(string message)
        {
            Log($"[HUB] PingWithCallback received: {message}");
            _state.ReceivedMessages.Enqueue(message);

            // Call client back
            Log("[HUB] Calling client 'onCallback'...");
            await Clients.Caller.SendAsync("onCallback", $"hello from server: {message}");
            Log("[HUB] Callback sent");

            return $"pong:{message}";
        }

        /// <summary>Step 3: Client sends a response to the server via a separate hub method</summary>
        public void ReceiveResponse(string correlationId, string data)
        {
            Log($"[HUB] ReceiveResponse entered: cid={correlationId}, data={data}");
            _state.ResponseReceived.TrySetResult($"{correlationId}:{data}");
        }

        /// <summary>Step 4: Server sends RPC to client during long-running method, waits for response</summary>
        public async Task<string> RunWithRpc(string command)
        {
            Log($"[HUB] RunWithRpc started: {command}");

            // Reset the TCS for this invocation
            _state.ResponseReceived = new TaskCompletionSource<string>();

            // Send a "request" to the client (like ReadKey RPC)
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            Log($"[HUB] Sending RPC request to client: cid={correlationId}");
            await Clients.Caller.SendAsync("onRpcRequest", correlationId);
            Log("[HUB] RPC request sent, waiting for ReceiveResponse...");

            // Wait for client to call ReceiveResponse
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cts.Token.Register(() => _state.ResponseReceived.TrySetCanceled());

            try
            {
                var result = await _state.ResponseReceived.Task;
                Log($"[HUB] Got response: {result}");
                return $"completed:{result}";
            }
            catch (TaskCanceledException)
            {
                Log("[HUB] TIMEOUT waiting for ReceiveResponse");
                throw new TimeoutException("Server never received ReceiveResponse from client");
            }
        }

        // ── Step 5a: Same RPC but using Context.Items (like production RpcMessageRegistry) ──

        private const string TcsKey = "RpcTcs";

        private TaskCompletionSource<string> GetOrCreateTcs()
        {
            if (Context.Items.TryGetValue(TcsKey, out var existing))
                return (TaskCompletionSource<string>)existing;
            var tcs = new TaskCompletionSource<string>();
            Context.Items[TcsKey] = tcs;
            return tcs;
        }

        /// <summary>Step 5a: ReceiveResponse using Context.Items-scoped TCS</summary>
        public void ReceiveResponseScoped(string correlationId, string data)
        {
            Log($"[HUB] ReceiveResponseScoped entered: cid={correlationId}, data={data}");
            var tcs = GetOrCreateTcs();
            tcs.TrySetResult($"{correlationId}:{data}");
        }

        /// <summary>Step 5a: RPC using Context.Items-scoped TCS</summary>
        public async Task<string> RunWithRpcScoped(string command)
        {
            Log($"[HUB] RunWithRpcScoped started: {command}");

            var tcs = GetOrCreateTcs();

            var correlationId = Guid.NewGuid().ToString("N")[..8];
            Log($"[HUB] Sending RPC request to client: cid={correlationId}");
            await Clients.Caller.SendAsync("onRpcRequest", correlationId);
            Log("[HUB] RPC request sent, waiting for ReceiveResponseScoped...");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cts.Token.Register(() => tcs.TrySetCanceled());

            try
            {
                var result = await tcs.Task;
                Log($"[HUB] Got response: {result}");
                return $"completed:{result}";
            }
            catch (TaskCanceledException)
            {
                Log("[HUB] TIMEOUT waiting for ReceiveResponseScoped");
                throw new TimeoutException("Server never received ReceiveResponseScoped from client");
            }
        }

        // ── Step 5b: Sync-over-async fix ──
        //
        // Problem: If a hub method does .GetAwaiter().GetResult() (sync-over-async),
        // it blocks the SignalR dispatch thread, preventing ReceiveResponse from being
        // dispatched — deadlock.
        //
        // Fix: Capture scoped dependencies (Clients.Caller, etc.) upfront, then wrap
        // the entire body in Task.Run so the hub dispatch thread is freed immediately.
        // The hub method awaits the Task.Run, keeping the hub instance alive.

        /// <summary>
        /// Step 5b: Hub method that internally does sync-over-async, but offloads
        /// the entire body to Task.Run so the dispatch thread stays free.
        /// This mirrors the proposed production fix for CommandLineHub.ReceiveRequest.
        /// </summary>
        public async Task<string> RunWithRpcSyncOverAsync(string command)
        {
            Log($"[HUB] RunWithRpcSyncOverAsync started: {command}");

            // Capture scoped dependencies before Task.Run
            // (In production: Clients.Caller, Context.ConnectionId, Context.Items values)
            var caller = Clients.Caller;
            _state.ResponseReceived = new TaskCompletionSource<string>();
            var tcs = _state.ResponseReceived;

            // Offload to thread pool — frees SignalR dispatch thread
            return await Task.Run(() =>
            {
                var correlationId = Guid.NewGuid().ToString("N")[..8];
                Log($"[HUB] Sending RPC request to client: cid={correlationId}");
                caller.SendAsync("onRpcRequest", correlationId).GetAwaiter().GetResult();
                Log("[HUB] RPC request sent, waiting for ReceiveResponseSingleton...");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                cts.Token.Register(() => tcs.TrySetCanceled());

                try
                {
                    var result = tcs.Task.GetAwaiter().GetResult();
                    Log($"[HUB] Got response: {result}");
                    return $"completed:{result}";
                }
                catch (TaskCanceledException)
                {
                    Log("[HUB] TIMEOUT waiting for ReceiveResponseSingleton");
                    throw new TimeoutException("Server never received ReceiveResponseSingleton from client");
                }
            });
        }

        /// <summary>Step 5b: ReceiveResponse using singleton state (for sync-over-async tests)</summary>
        public void ReceiveResponseSingleton(string correlationId, string data)
        {
            Log($"[HUB] ReceiveResponseSingleton entered: cid={correlationId}, data={data}");
            _state.ResponseReceived.TrySetResult($"{correlationId}:{data}");
        }
    }

    // ──────────────────────────────────────────────
    // Test infrastructure
    // ──────────────────────────────────────────────

    private (TestServer server, HubConnection connection, HubState state) CreateTestServerAndClient(
        Action<IHubConnectionBuilder>? configureConnection = null)
    {
        var state = new HubState();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(state);
                services.AddSignalR()
                    .AddHubOptions<TestHub>(opts =>
                    {
                        opts.MaximumParallelInvocationsPerClient = 10;
                    });
                services.AddRouting();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<TestHub>("/testHub");
                });
            });

        var server = new TestServer(webHostBuilder);
        server.PreserveExecutionContext = true;

        var httpClient = server.CreateClient();
        var connectionBuilder = new HubConnectionBuilder()
            .WithUrl($"{server.BaseAddress}testHub", opts =>
            {
                opts.Transports = HttpTransportType.LongPolling;
                opts.HttpMessageHandlerFactory = _ => server.CreateHandler();
            });

        configureConnection?.Invoke(connectionBuilder);

        var connection = connectionBuilder.Build();

        return (server, connection, state);
    }

    // ──────────────────────────────────────────────
    // Step 1: Basic client→server call
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task Step1_ClientCallsServer_ServerReceives()
    {
        var (server, connection, state) = CreateTestServerAndClient();
        using var _ = server;

        await connection.StartAsync();
        Log($"[CLIENT] Connected, state={connection.State}");

        var result = await connection.InvokeAsync<string>("Ping", "hello");
        Log($"[CLIENT] Ping returned: {result}");

        result.Should().Be("pong:hello", GetDiagnostics());
        state.ReceivedMessages.Should().Contain("hello", GetDiagnostics());

        await connection.StopAsync();
    }

    // ──────────────────────────────────────────────
    // Step 2: Server calls client back during hub method
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task Step2_ServerCallsClientBack_DuringHubMethod()
    {
        var callbackReceived = new TaskCompletionSource<string>();

        var (server, connection, state) = CreateTestServerAndClient();
        using var _ = server;

        connection.On<string>("onCallback", msg =>
        {
            Log($"[CLIENT] Callback received: {msg}");
            callbackReceived.TrySetResult(msg);
        });

        await connection.StartAsync();
        Log($"[CLIENT] Connected, state={connection.State}");

        var result = await connection.InvokeAsync<string>("PingWithCallback", "test");
        Log($"[CLIENT] PingWithCallback returned: {result}");

        // Also verify callback was received
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => callbackReceived.TrySetCanceled());
        var cbResult = await callbackReceived.Task;
        Log($"[CLIENT] Callback value: {cbResult}");

        result.Should().Be("pong:test", GetDiagnostics());
        cbResult.Should().Contain("hello from server", GetDiagnostics());

        await connection.StopAsync();
    }

    // ──────────────────────────────────────────────
    // Step 3: Client calls ReceiveResponse hub method
    //         (independent of any running hub method)
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task Step3_ClientCallsReceiveResponse_ServerReceives()
    {
        var (server, connection, state) = CreateTestServerAndClient();
        using var _ = server;

        await connection.StartAsync();
        Log($"[CLIENT] Connected, state={connection.State}");

        // Client directly calls ReceiveResponse
        Log("[CLIENT] Sending ReceiveResponse...");
        await connection.SendAsync("ReceiveResponse", "cid-123", "my-data");
        Log("[CLIENT] ReceiveResponse sent");

        // Wait for server to process it
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => state.ResponseReceived.TrySetCanceled());
        var result = await state.ResponseReceived.Task;
        Log($"[CLIENT] Server got: {result}");

        result.Should().Be("cid-123:my-data", GetDiagnostics());

        await connection.StopAsync();
    }

    // ──────────────────────────────────────────────
    // Step 4: Full RPC round-trip
    //   Server method sends request → client receives → client calls
    //   ReceiveResponse → server's waiting method completes
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task Step4_FullRpcRoundTrip_ServerSendsRequest_ClientResponds()
    {
        var (server, connection, state) = CreateTestServerAndClient();
        using var _ = server;

        // Wire up: when server sends an RPC request, client responds via ReceiveResponse
        connection.On<string>("onRpcRequest", async correlationId =>
        {
            Log($"[CLIENT] RPC request received: cid={correlationId}");
            Log("[CLIENT] Sending ReceiveResponse...");
            await connection.SendAsync("ReceiveResponse", correlationId, "user-pressed-Y");
            Log("[CLIENT] ReceiveResponse sent");
        });

        await connection.StartAsync();
        Log($"[CLIENT] Connected, state={connection.State}");

        // This hub method will: send RPC to client → wait for ReceiveResponse → return
        Log("[CLIENT] Invoking RunWithRpc...");
        var result = await connection.InvokeAsync<string>("RunWithRpc", "delete files");
        Log($"[CLIENT] RunWithRpc returned: {result}");

        result.Should().Contain("user-pressed-Y", GetDiagnostics());

        await connection.StopAsync();
    }

    // ──────────────────────────────────────────────
    // Step 5a: RPC using Context.Items-scoped state
    //   Same as Step 4 but TCS is stored per-connection in
    //   Context.Items (like production's RpcMessageRegistry)
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task Step5a_RpcWithContextItemsScope_Works()
    {
        var (server, connection, state) = CreateTestServerAndClient();
        using var _ = server;

        connection.On<string>("onRpcRequest", async correlationId =>
        {
            Log($"[CLIENT] RPC request received: cid={correlationId}");
            await connection.SendAsync("ReceiveResponseScoped", correlationId, "scoped-Y");
            Log("[CLIENT] ReceiveResponseScoped sent");
        });

        await connection.StartAsync();
        Log($"[CLIENT] Connected, state={connection.State}");

        var result = await connection.InvokeAsync<string>("RunWithRpcScoped", "delete files");
        Log($"[CLIENT] RunWithRpcScoped returned: {result}");

        result.Should().Contain("scoped-Y", GetDiagnostics());

        await connection.StopAsync();
    }

    // ──────────────────────────────────────────────
    // Step 5b: Sync-over-async RPC
    //   Hub method blocks with GetAwaiter().GetResult()
    //   like production's SignalRAnsiInput.ReadKey()
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task Step5b_SyncOverAsync_RpcRoundTrip()
    {
        var (server, connection, state) = CreateTestServerAndClient();
        using var _ = server;

        connection.On<string>("onRpcRequest", async correlationId =>
        {
            Log($"[CLIENT] RPC request received: cid={correlationId}");
            await connection.SendAsync("ReceiveResponseSingleton", correlationId, "sync-Y");
            Log("[CLIENT] ReceiveResponseSingleton sent");
        });

        await connection.StartAsync();
        Log($"[CLIENT] Connected, state={connection.State}");

        try
        {
            var result = await connection.InvokeAsync<string>("RunWithRpcSyncOverAsync", "delete files");
            Log($"[CLIENT] RunWithRpcSyncOverAsync returned: {result}");
            result.Should().Contain("sync-Y", GetDiagnostics());
        }
        catch (Exception ex)
        {
            Log($"[CLIENT] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Assert.Fail($"InvokeAsync failed.\n--- DIAGNOSTICS ---\n{GetDiagnostics()}\n--- END ---");
        }

        await connection.StopAsync();
    }
}
