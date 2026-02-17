# RPC Communication Pattern

The remote execution system uses a bidirectional RPC pattern over SignalR for correlated request/response messaging.

---

## How It Works

SignalR provides a persistent bidirectional connection. The RPC layer adds request correlation on top:

1. The sender creates a request with a unique `CorrelationId`
2. The request is registered in the `RpcMessageRegistry`
3. The request is sent over the SignalR connection
4. The receiver processes the request and sends a response with the same `CorrelationId`
5. The registry matches the response to the pending request and completes the task

```
Client                              Server
  │                                    │
  ├── Request (CorrelationId: abc) ───►│
  │                                    │  Process request
  │◄── Response (CorrelationId: abc) ──┤
  │                                    │
```

---

## Key Types

| Type | Description |
|------|-------------|
| `RpcMessageRegistry` | Maintains pending requests keyed by `CorrelationId` |
| `RpcMessageContext` | Wraps a pending request with a `TaskCompletionSource` for async await |
| `IRpcScope` | Scoped lifetime container for RPC contexts |

---

## Bidirectional RPC

The server can also send requests to the client. This is used for remote console I/O — when a server command needs to read a keystroke, the server sends an RPC request to the client:

```
Server                              Client
  │                                    │
  ├── ReadKey Request ────────────────►│
  │                                    │  Read from local console
  │◄── ReadKey Response ──────────────-┤
  │                                    │
```

This enables server-side commands to use `Console.ReadKey()` transparently, with the actual keystroke captured on the client.

---

## Error Propagation

If the receiver encounters an error while processing a request, the error is serialized in the response. The sender's awaiting task throws the deserialized exception, propagating errors across the wire.

---

## See Also

- [Remote Execution](index.md)
- [Shared Protocol](shared-protocol.md)
- [The IServerProxy Interface](server-proxy.md)
