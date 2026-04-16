<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: 002
  GitHub Issue Number: #52
-->

# Protocol message envelopes for client file access

**Labels**: enhancement, spec-012
**Blocked by**: None
**Implements**: FR-006, FR-016, FR-017
**Covers**: —

## Summary

Add the SignalR message envelopes and type extensions needed for the server to request file operations from the client and for the client to respond. This is the shared protocol layer — no business logic, just message definitions.

## Current Behavior

`PushMessageType` has only `FileUploadProgress`. `ServerRequestType` has no file access response type. There are no message classes for server-initiated file access requests.

## Expected Behavior

Three new `PushMessageType` values (`ClientFileUploadRequest`, `ClientFileDownloadRequest`, `ClientFileEnumerateRequest`), one new `ServerRequestType` value (`ClientFileAccessResponse`), and corresponding message envelope classes exist in the shared `BitPantry.CommandLine.Remote.SignalR` project.

## Affected Area

- **Project(s):** `BitPantry.CommandLine.Remote.SignalR`
- **Key files:**
  - `BitPantry.CommandLine.Remote.SignalR/Envelopes/PushMessage.cs` — MODIFY: add enum values
  - `BitPantry.CommandLine.Remote.SignalR/Envelopes/ServerRequest.cs` — MODIFY: add enum value
  - `BitPantry.CommandLine.Remote.SignalR/Envelopes/ClientFileUploadRequestMessage.cs` — NEW
  - `BitPantry.CommandLine.Remote.SignalR/Envelopes/ClientFileDownloadRequestMessage.cs` — NEW
  - `BitPantry.CommandLine.Remote.SignalR/Envelopes/ClientFileEnumerateRequestMessage.cs` — NEW
  - `BitPantry.CommandLine.Remote.SignalR/Envelopes/ClientFileAccessResponseMessage.cs` — NEW
- **Spec reference:** See `specs/012-client-file-access/spec.md`
- **Plan reference:** See `specs/012-client-file-access/plan.md`
- **Data model reference:** See `specs/012-client-file-access/data-model.md`

## Requirements

- [ ] `PushMessageType` enum has `ClientFileUploadRequest` value
- [ ] `PushMessageType` enum has `ClientFileDownloadRequest` value
- [ ] `PushMessageType` enum has `ClientFileEnumerateRequest` value
- [ ] `ServerRequestType` enum has `ClientFileAccessResponse` value (value = 6)
- [ ] `ClientFileUploadRequestMessage` extends `PushMessage` with `ClientPath` and `ServerTempPath` properties
- [ ] `ClientFileDownloadRequestMessage` extends `PushMessage` with `ServerPath`, `ClientPath`, and `FileSize` properties
- [ ] `ClientFileEnumerateRequestMessage` extends `PushMessage` with `GlobPattern` property
- [ ] `ClientFileAccessResponseMessage` extends `ServerRequest` with `Success`, `Error`, and `FileInfoEntries` properties
- [ ] All message classes follow existing `MessageBase` pattern (Dictionary<string, string> data, JsonConstructor, typed property accessors)
- [ ] All message classes have `CorrelationId` (inherited from `MessageBase`)

## Prerequisites

No prerequisites — this issue can be started independently.

## Implementation Guidance

Follow the exact pattern of existing message classes. For example, `FileUploadProgressMessage` shows how to extend `PushMessage`:

```csharp
public class ClientFileUploadRequestMessage : PushMessage
{
    [JsonIgnore]
    public string ClientPath
    {
        get { return TryGetValue(MessageArgNames.ClientFileAccess.ClientPath); }
        set { Data[MessageArgNames.ClientFileAccess.ClientPath] = value; }
    }

    [JsonIgnore]
    public string ServerTempPath
    {
        get { return TryGetValue(MessageArgNames.ClientFileAccess.ServerTempPath); }
        set { Data[MessageArgNames.ClientFileAccess.ServerTempPath] = value; }
    }

    [JsonConstructor]
    public ClientFileUploadRequestMessage(Dictionary<string, string> data) : base(data) { }

    public ClientFileUploadRequestMessage(string clientPath, string serverTempPath)
        : base(PushMessageType.ClientFileUploadRequest)
    {
        ClientPath = clientPath;
        ServerTempPath = serverTempPath;
    }
}
```

Add compact key names to `MessageArgNames` (e.g., `"cfacp"` for ClientPath, `"cfastp"` for ServerTempPath) following the existing pattern of abbreviated keys.

For `ClientFileAccessResponseMessage`, extend `ServerRequest` (not `PushMessage`) since it flows client → server via `ReceiveRequest`. `FileInfoEntries` should be serialized/deserialized as JSON string (using `SerializeObject`/`DeserializeObject` helpers from `MessageBase`).

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

### Test Approach

- **Test level:** Unit
- **Test project:** `BitPantry.CommandLine.Tests.Remote.SignalR`
- **Existing fixtures to reuse:** None — these are pure serialization tests

### Prescribed Test Cases

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `ClientFileUploadRequestMessage_Roundtrip_PropertiesPreserved` | Create message, serialize to dict, deserialize from dict | All properties match original values |
| 2 | `ClientFileDownloadRequestMessage_Roundtrip_PropertiesPreserved` | Same roundtrip test | All properties preserved |
| 3 | `ClientFileEnumerateRequestMessage_Roundtrip_PropertiesPreserved` | Same roundtrip test | All properties preserved |
| 4 | `ClientFileAccessResponseMessage_Roundtrip_PropertiesPreserved` | Same roundtrip test including FileInfoEntries JSON | All properties preserved |
| 5 | `ClientFileAccessResponseMessage_WithError_ErrorPreserved` | Create with error string | Error property roundtrips correctly |
| 6 | `ClientFileUploadRequestMessage_CorrelationId_Inherited` | Create message | Has non-null CorrelationId from base |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above.

### TDD Workflow

Follow the `tdd-workflow` skill: write failing tests first (RED), implement (GREEN), refactor.
