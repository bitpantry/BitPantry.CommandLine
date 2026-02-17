# File Transfers

Upload and download files between client and server with glob pattern support, progress display, and checksum verification.

---

## Uploading Files

```
app> server upload --source ./data/*.csv
Uploading 3 files...
  data/report-jan.csv    ✓  (1.2 MB)
  data/report-feb.csv    ✓  (980 KB)
  data/report-mar.csv    ✓  (1.1 MB)
Done. 3 files uploaded.
```

---

## Downloading Files

```
app> server download --source reports/*.pdf --destination ./local-reports
Downloading 2 files...
  reports/q1-summary.pdf    ✓  (4.5 MB)
  reports/q2-summary.pdf    ✓  (3.8 MB)
Done. 2 files downloaded.
```

---

## Glob Pattern Support

File paths support glob patterns via `GlobPatternHelper`:

| Pattern | Matches |
|---------|---------|
| `*.csv` | All CSV files in the current directory |
| `**/*.log` | All log files recursively |
| `data/2024-*` | Files matching the prefix |

---

## Progress Display

Files larger than 25 MB show a progress bar during transfer:

```
  large-dataset.csv   [████████████░░░░░░░░]  62%  (155 MB / 250 MB)
```

Smaller files complete without progress display.

---

## Concurrent Transfers

Transfers run concurrently with a maximum of 4 simultaneous streams. This is managed internally by the `FileTransferService`.

---

## Checksum Verification

Each transfer includes a checksum to verify data integrity. If the checksum doesn't match, the transfer is reported as failed and the file is discarded.

---

## Server-Side Constraints

Uploads are subject to the server's [sandboxing](../server/sandboxing.md) rules:

| Constraint | Configuration |
|------------|--------------|
| Storage root | `FileTransferOptions.StorageRootPath` |
| Max file size | `FileTransferOptions.MaxFileSizeBytes` (default: 100 MB) |
| Allowed extensions | `FileTransferOptions.AllowedExtensions` (default: all) |

The server reports its `MaxFileSizeBytes` in `ServerCapabilities`, which can be checked via:

```csharp
var maxSize = ServerCapabilities.FormatFileSize(proxy.Server.MaxFileSizeBytes);
```

---

## See Also

- [Setting Up the Client](index.md)
- [File System & Sandboxing](../server/sandboxing.md)
- [The IServerProxy Interface](../server-proxy.md)
- [RPC Communication Pattern](../rpc.md)
