# Shared Protocol

`BitPantry.CommandLine.Remote.SignalR` is a shared library referenced by both client and server. It defines the message types, RPC infrastructure, and serialization used for remote communication.

---

## Envelope Types

All communication between client and server uses typed message envelopes:

| Type | Direction | Description |
|------|-----------|-------------|
| `ServerRequest` | Client → Server | Wraps a typed request (run, autocomplete, etc.) |
| `ResponseMessage` | Server → Client | Wraps a response with correlation ID |
| `RunRequest` | Client → Server | Execute a command |
| `RunResponse` | Server → Client | Execution result |
| `AutoCompleteRequest` | Client → Server | Autocomplete query |
| `AutoCompleteResponse` | Server → Client | Autocomplete results |

---

## RPC Infrastructure

The RPC system provides correlated request/response messaging over the bidirectional SignalR connection:

| Type | Description |
|------|-------------|
| `RpcMessageRegistry` | Registers pending requests and correlates responses by `CorrelationId` |
| `RpcMessageContext` | Wraps a pending request with its completion source |
| `IRpcScope` | Scoped lifetime for RPC message contexts |

This enables the server to make requests back to the client (e.g., requesting keystrokes for remote console input).

---

## Serialization

| Type | Description |
|------|-------------|
| `RemoteJsonOptions` | Pre-configured `JsonSerializerOptions` for the protocol |
| Custom converters | `CommandInfoJsonConverter`, `ArgumentInfoJsonConverter`, `GroupInfoJsonConverter` |

The custom converters handle serialization of the component model types (`CommandInfo`, `ArgumentInfo`, `GroupInfo`) which contain circular references and complex object graphs.

---

## Constants

| Class | Contents |
|-------|----------|
| `SignalRMethodNames` | Hub method names (`ReceiveRequest`, `ReceiveResponse`) |
| `ServiceEndpointNames` | HTTP endpoint paths for token and file transfer services |

---

## See Also

- [Remote Execution](index.md)
- [RPC Communication Pattern](rpc.md)
- [The IServerProxy Interface](server-proxy.md)
- [Component Model](../api-reference/component-model.md)
