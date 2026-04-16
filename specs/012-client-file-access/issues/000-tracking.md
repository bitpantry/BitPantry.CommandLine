# Spec 012: Client File Access

Tracking issue for the 012-client-file-access feature specification.

**Spec**: `specs/012-client-file-access/spec.md`
**Plan**: `specs/012-client-file-access/plan.md`

## Overview

Add a location-transparent `IClientFileAccess` service that lets commands read and write files on the user's machine regardless of whether the command runs locally or on a remote server. Includes consent prompts for server-initiated file access, glob pattern support, and full reuse of existing HTTP file transfer infrastructure.

## Issues

### Phase 1: Core Types & Foundation
- [ ] 001 — Core types: IClientFileAccess, ClientFile, and local implementation
- [ ] 002 — Protocol message envelopes for client file access
- [ ] 004 — File access consent policy and ConnectCommand --allow-path

### Phase 2: Server & Client Implementations
- [ ] 003 — RemoteClientFileAccess server implementation (blocked by 001, 002)
- [ ] 005 — Client-side push message handler and consent UX (blocked by 002, 004)

### Phase 3: Integration & Round-Trip
- [ ] 006 — End-to-end integration: single file GetFile and SaveFile (blocked by 003, 005)

### Phase 4: Glob & Hardening
- [ ] 007 — Glob pattern support: GetFilesAsync with lazy enumeration (blocked by 006)
- [ ] 008 — Edge cases and hardening (blocked by 006, 007)

## Labels

- `spec-012`
