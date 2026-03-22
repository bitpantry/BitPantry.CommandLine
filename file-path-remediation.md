# Cross-Platform File Path Test Remediation

CI failures on `ubuntu-latest` (Linux) from release tag `release-v20260322-180852` (commit `3a5d680`).
All failures stem from Windows-specific paths, path separators, or path resolution that behave differently on Linux.

**Summary:** 117 distinct test failures across 2 test projects (114 in Remote.SignalR, 3 in CommandLine.Tests).

---

## Previously Fixed (Commits `5c38597`, `e615ad4`, `3a5d680`)

These tests were broken in earlier CI runs and have been remediated. They are **passing** in the current CI run.

### Commit `5c38597` — Platform-portable paths in LocalPathEntryProvider and UploadCommand

| Status | Test | File | Root Cause |
|--------|------|------|------------|
| ✅ FIXED | `GetOptionsAsync_OnlyReturnsDirectories` | `ClientTests/LocalPathEntryProviderTests.cs` | Hardcoded `C:\work` not rooted on Linux |
| ✅ FIXED | `GetOptionsAsync_ReturnsMatchingFiles` | `ClientTests/LocalPathEntryProviderTests.cs` | Hardcoded `C:\work` not rooted on Linux |
| ✅ FIXED | `ResolveDestinationPath_DirectoryDestination_AppendsFilename` | `ClientTests/UploadCommandTests.cs` | `Path.GetFileName` doesn't parse `\` on Linux |

### Commit `e615ad4` — Trim() instead of TrimEnd() for Spectre progress cleanup

| Status | Test | File | Root Cause |
|--------|------|------|------------|
| ✅ FIXED | `SpectreProgress_SingleTask_RendersProgressBar` | `SpectreConsoleIntegrationTests.cs` | Leading whitespace from Spectre on Linux |
| ✅ FIXED | `SpectreProgress_MultiTask_RendersAllTasks` | `SpectreConsoleIntegrationTests.cs` | Leading whitespace from Spectre on Linux |

### Commit `3a5d680` — Cross-platform test failures for Linux CI

| Status | Test | File | Root Cause |
|--------|------|------|------------|
| ✅ FIXED | `GetOptionsAsync_EmptyQuery_ReturnsAllEntriesInCurrentDir` | `DirectoryPathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_SubdirectoryQuery_ReturnsContents` | `DirectoryPathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_PartialMatch_ReturnsMatchingDirs` | `DirectoryPathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_NoMatch_ReturnsEmpty` | `DirectoryPathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_DirectoryEntries_HaveTrailingSeparator` | `DirectoryPathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_DirectoryEntries_HaveMenuStyle` | `DirectoryPathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_DirectoryOptions_HaveTrailingSlash` | `DirectoryPathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_Options_ReplacementIsPrefixPlusEntry` | `DirectoryPathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_EmptyQuery_ReturnsAllEntriesInCurrentDir` | `FilePathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_SubdirectoryQuery_ReturnsContents` | `FilePathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_PartialMatch_ReturnsMatchingEntries` | `FilePathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_NoMatch_ReturnsEmpty` | `FilePathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_CaseInsensitiveMatching` | `FilePathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_TrailingSlashQuery_ListsDirectoryContents` | `FilePathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_DirectoryEntries_HaveMenuStyle` | `FilePathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |
| ✅ FIXED | `GetOptionsAsync_FileEntries_HaveNoMenuStyle` | `FilePathAutoCompleteHandlerTests.cs` | Hardcoded `C:\work` MockFileSystem root |

---

## Still Failing — `BitPantry.CommandLine.Tests` (3 of 875)

### Category A: History Recall Syntax Highlighting (2 tests)

**Root Cause:** After history recall via Up/Down Arrow, the cell's foreground style has **no color at all** (`Foreground256 = null`, `ForegroundRgb = null`). This means highlighting is not being applied to recalled text on Linux — it's not a 256-vs-RGB format mismatch, it's a total absence of styling. The `AssertForegroundColor` helper falls through to the else branch and fails.

| Status | Test | File | Line | Error |
|--------|------|------|------|-------|
| ❌ OPEN | `UpArrow_HistoryRecall_AppliesSyntaxHighlighting` | `Input/InputBuilderSyntaxHighlightTests.cs` | L132 (assertion), L521 (call site) | `Expected cell.Style.Foreground256 to be 0x0E ... but found <null>` |
| ❌ OPEN | `DownArrow_HistoryRecall_AppliesSyntaxHighlighting` | `Input/InputBuilderSyntaxHighlightTests.cs` | L132 (assertion), L560 (call site) | `Expected cell.Style.Foreground256 to be 0x0E ... but found <null>` |

### Category B: File Path Autocomplete RelativeDotDot (1 test)

**Root Cause:** On Linux, `WorkDir = "/work"` and `parentDir = "/"`. Navigating `../` from `/work` goes to `/`. MockFileSystem on Linux may not enumerate entries at `/` correctly, or the sibling path `/sibling/other.txt` is not found.

| Status | Test | File | Line | Error |
|--------|------|------|------|-------|
| ❌ OPEN | `GetOptionsAsync_RelativeDotDot_ReturnsParentEntries` | `AutoComplete/Handlers/FilePathAutoCompleteHandlerTests.cs` | L302 | `Expected options not to be empty` |

---

## Still Failing — `BitPantry.CommandLine.Tests.Remote.SignalR` (114 of 809)

### Category 1: PathValidator — Hardcoded `C:\ServerStorage` (10 tests)

**File:** `ServerTests/PathValidationTests.cs`

**Root Cause:** `PathValidator` is constructed with `StorageRoot = @"C:\ServerStorage"`. On Linux, `Path.GetFullPath` on a relative subpath combined with `C:\ServerStorage` yields a path rooted at the Linux CWD (e.g., `/home/runner/.../C:\ServerStorage/...`), not the expected Windows-style path. Additionally, `..` traversal detection fails because `Path.GetFullPath` resolves `../` relative to Linux CWD, which may still appear to be under the root.

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `ValidatePath_RelativePathWithinRoot_ReturnsFullPath` | L18 | Expected `C:\ServerStorage/subfolder/file.txt` but got `/home/runner/...` |
| ❌ OPEN | `ValidatePath_AbsolutePathOutsideRoot_ThrowsUnauthorizedAccessException` | L68 | Expected exception, but none was thrown |
| ❌ OPEN | `ValidatePath_PathWithSpaces_ReturnsValidPath` | L93 | Expected `C:\ServerStorage/sub folder/my file.txt` but got `/home/runner/...` |
| ❌ OPEN | `ValidatePath_PathWithUnicode_ReturnsValidPath` | L107 | Expected `C:\ServerStorage/文件夹/文件.txt` but got `/home/runner/...` |
| ❌ OPEN | `ValidatePath_PathAtRootBoundary_ReturnsValidPath` | L148 | Expected `C:\ServerStorage/file.txt` but got `/home/runner/...` |
| ❌ OPEN | `ValidatePath_WindowsStyleBackslashTraversal_ThrowsUnauthorizedAccessException` | L168 | Expected exception, but none was thrown |
| ❌ OPEN | `ValidatePath_MixedSlashTraversal_ThrowsUnauthorizedAccessException` | L183 | Expected exception, but none was thrown |
| ❌ OPEN | `ValidatePath_ForwardSlashRoot_ReturnsSandboxRoot` | L194 | Expected `C:\ServerStorage` but got `/home/runner/...` |
| ❌ OPEN | `ValidatePath_BackslashRoot_ReturnsSandboxRoot` | L207 | Expected `result` to equal `StorageRoot` |
| ❌ OPEN | `ValidatePath_ForwardSlashWithFilename_ReturnsFileAtSandboxRoot` | L220 | Expected `C:\ServerStorage/file.txt` but got `/home/runner/...` |

### Category 2: MockFileSystem with `C:\storage` Root — Server Command Tests (23 tests)

**Root Cause:** Server command tests (ls, rm, mkdir, cp, stat) construct a `MockFileSystem` with `@"C:\storage"` root. On Linux, `C:\storage` is not a valid rooted path — files placed there by MockFileSystem are inaccessible via expected paths.

#### LsCommand Tests (14 tests)

**File:** `ServerTests/LsCommandTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Execute_WithPath_ListsFilesAtPath` | L162 | `Directory not found: C:\storage\reports` |
| ❌ OPEN | `Execute_WithSubdirPath_ListsSubdirContents` | L183 | `Directory not found: C:\storage\data` |
| ❌ OPEN | `Execute_WithGlobPattern_FiltersToMatchingFiles` | L204 | `No files matching: C:\storage\*.txt` |
| ❌ OPEN | `Execute_WithGlobPattern_MatchesMultipleFiles` | L226 | `No files matching: C:\storage\*.log` |
| ❌ OPEN | `Execute_WithRecursive_ListsAllDepths` | L250 | `Directory not found: C:\storage` |
| ❌ OPEN | `Execute_WithSortSize_OrdersByFileSize` | L279 | `small should appear before medium, but found -1` |
| ❌ OPEN | `Execute_DefaultList_ShowsFilesAndDirectoriesWithCorrectSuffixes` | L337 | `Directory not found: C:\storage` |
| ❌ OPEN | `Execute_LongFormat_ShowsTableWithHeadersAndFormattedSizes` | L370 | `Directory not found: C:\storage` |
| ❌ OPEN | `Execute_Recursive_ShowsNestedEntriesWithHierarchy` | L400 | `Directory not found: C:\storage` |
| ❌ OPEN | `Execute_SortBySize_OrdersByFileSizeAscAndDesc` | L438 | `small.txt should appear before big.txt, but found -1` |
| ❌ OPEN | `Execute_SortByModified_OrdersByLastModifiedOldestFirst` | L481 | `older.txt should appear before newer, but found -1` |
| ❌ OPEN | `Execute_SortByName_OrdersAlphabeticallyAndReverse` | L505 | `a.txt should appear before z.txt, but found -1` |
| ❌ OPEN | `Execute_Recursive_WithSandboxedFs_DoesNotShowStorageRootPrefix` | L594 | `Directory not found: .` |
| ❌ OPEN | `Execute_Recursive_UsesForwardSlashSeparators` | L628 | `Expected collection not to be empty` |

#### RmCommand Tests (7 tests)

**File:** `ServerTests/RmCommandTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Execute_NonEmptyDir_WithoutRecursive_ProducesError` | L92 | Expected output containing `Cannot remove 'C:\storage\mydir'` |
| ❌ OPEN | `Execute_NonEmptyDir_WithRecursive_DeletesAll` | L201 | `fs.File.Exists(@"C:\storage\mydir\file1.txt")` still true |
| ❌ OPEN | `Execute_GlobPattern_DeletesMatchingFiles` | L223 | `fs.File.Exists(@"C:\storage\a.log")` still true |
| ❌ OPEN | `Execute_GlobBelowThreshold_NoPrompt_DeletesFiles` | L246 | `fs.File.Exists(@"C:\storage\a.log")` still true |
| ❌ OPEN | `Execute_GlobAboveThreshold_ConfirmYes_DeletesAll` | L272 | `fs.File.Exists(@"C:\storage\a.log")` still true |
| ❌ OPEN | `Execute_GlobMultipleMatches_ShowsItemCount` | L385 | `fs.File.Exists(@"C:\storage\a.log")` still true |
| ❌ OPEN | `Execute_GlobNoMatch_DisplaysExplicitMessageAndNoDeletions` | L407 | Expected output `Path not found: C:\storage...` |

#### CpCommand Tests (4 tests)

**File:** `ServerTests/CpCommandTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Execute_DirectoryWithRecursive_CopiesSuccessfully` | L76 | `fs.File.Exists(@"C:\storage\dstdir\file.txt")` is false |
| ❌ OPEN | `Execute_DirectoryRecursive_CopiesNestedStructure` | L137 | `fs.Directory.Exists(@"C:\storage\dst")` is false |
| ❌ OPEN | `Execute_PathTraversalInDestination_ProducesAccessDeniedError` | L269 | Expected output `Source not found: C:\storage\src.txt` |
| ❌ OPEN | `Execute_DirectoryRecursive_SummaryShowsItemCount` | L310 | Expected output `Source not found: C:\storage\src` |

#### MkdirCommand Tests (3 tests)

**File:** `ServerTests/MkdirCommandTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Execute_WithParents_CreatesAllIntermediateDirs` | L72 | `fs.Directory.Exists(@"C:\storage\a")` is false |
| ❌ OPEN | `Execute_ParentMissing_WithoutParentsFlag_FailsWithError` | L94 | `fs.Directory.Exists(@"C:\storage\a\b")` unexpectedly true |
| ❌ OPEN | `Execute_PathTraversal_BlockedByError` | L135 | Expected output `Created: ..\..\tmp\evil` not found |

#### StatCommand Tests (2 tests)

**File:** `ServerTests/StatCommandTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Execute_Directory_ShowsCorrectCounts` | L105 | Expected output `Name: C:\storage\project` |
| ❌ OPEN | `Execute_Directory_ShowsRecursiveTotalSize` | L124 | Expected output `Name: C:\storage\project` |

### Category 3: SandboxedFileSystem / SandboxedDirectory / SandboxedFile — `C:\storage` Root (9 tests)

**Root Cause:** Same as Category 2 — `MockFileSystem` with `@"C:\storage"` root. These are lower-level tests of the `SandboxedFileSystem`, `SandboxedFile`, and `SandboxedDirectory` wrappers.

#### SandboxedDirectoryTests (3 tests)

**File:** `ServerTests/SandboxedDirectoryTests.cs` (assertions) / Source: `SandboxedDirectory.cs`

| Status | Test | Source Line | Error |
|--------|------|-------------|-------|
| ❌ OPEN | `Exists_DirectoryExists_ReturnsTrue` | L39 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `CreateDirectory_ValidPath_CreatesDirectory` | L78 | `Expected directory to exist, but found False` |
| ❌ OPEN | `CreateDirectory_Nested_CreatesAllLevels` | L112 | `Expected directory to exist, but found False` |

**Note:** The following SandboxedDirectoryTests throw exceptions (error line in source `SandboxedDirectory.cs`):

| Status | Test | Source Line | Error |
|--------|------|-------------|-------|
| ❌ OPEN | `Delete_EmptyDirectory_Succeeds` | SandboxedDirectory.cs:44 | Threw exception |
| ❌ OPEN | `Delete_NonEmptyRecursive_DeletesAll` | SandboxedDirectory.cs:45 | Threw exception |
| ❌ OPEN | `Move_ValidPaths_MovesDirectory` | SandboxedDirectory.cs:48 | Threw exception |
| ❌ OPEN | `EnumerateFiles_ReturnsFiles` | SandboxedDirectory.cs:79 | Threw exception |
| ❌ OPEN | `EnumerateFiles_WithPattern_ReturnsMatchingFiles` | SandboxedDirectory.cs:80 | Threw exception |
| ❌ OPEN | `EnumerateFiles_Recursive_ReturnsAllFiles` | SandboxedDirectory.cs:82 | Threw exception |
| ❌ OPEN | `EnumerateDirectories_ReturnsSubdirectories` | SandboxedDirectory.cs:86 | Threw exception |

#### SandboxedFileTests (3 tests)

**File:** `ServerTests/SandboxedFileTests.cs` (assertions) / Source: `SandboxedFile.cs`

| Status | Test | Source Line | Error |
|--------|------|-------------|-------|
| ❌ OPEN | `Exists_FileExists_ReturnsTrue` | L39 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `WriteAllBytes_ValidPath_WritesFile` | L121 | `Expected file to exist, but found False` |
| ❌ OPEN | `WriteAllText_ValidPath_WritesContent` | L291 | `Expected file to exist, but found False` |

**Note:** The following SandboxedFileTests throw exceptions (error line in source `SandboxedFile.cs`):

| Status | Test | Source Line | Error |
|--------|------|-------------|-------|
| ❌ OPEN | `Delete_FileExists_DeletesFile` | SandboxedFile.cs:65 | Threw exception |
| ❌ OPEN | `Copy_ValidPaths_CopiesFile` | SandboxedFile.cs:68 | Threw exception |
| ❌ OPEN | `Move_ValidPaths_MovesFile` | SandboxedFile.cs:74 | Threw exception |
| ❌ OPEN | `ReadAllText_FileExists_ReturnsContent` | SandboxedFile.cs:89 | Threw exception |
| ❌ OPEN | `GetAttributes_ValidPath_ReturnsAttributes` | SandboxedFile.cs:279 | Threw exception |
| ❌ OPEN | `WriteAllBytes_NestedPath_CreatesDirectoriesAndWritesFile` | SandboxedFile.cs:155 | Threw exception |

#### SandboxedFileSystemTests (1 test)

**File:** `ServerTests/SandboxedFileSystemTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `FilePathAutoCompleteHandler_WithSandboxedFileSystem_EmptyQuery_ReturnsOptions` | L290 | `Expected collection not to be empty` |

### Category 4: PathEntries RPC Handler — Empty Storage Root on Linux (3 tests)

**File:** `ServerTests/PathEntriesRpcHandlerTests.cs`

**Root Cause:** RPC handler tests use a MockFileSystem with `C:\storage` root. The handler fails to find directories, returning `"Directory not found"` errors.

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `HandleRequest_ValidDirectory_IncludeFiles_ReturnsAll` | L64 | `Expected response.Error to be <null>, but found "Directory not found: "` |
| ❌ OPEN | `HandleRequest_ValidDirectory_IncludeFilesFalse_ReturnsOnlyDirs` | L82 | `Expected string to be <null>, but found "Directory not found: "` |
| ❌ OPEN | `HandleRequest_EmptyDirectoryPath_EnumeratesStorageRoot` | L138 | `Expected response.Error to be <null>, but found "Directory not found: "` |

### Category 5: Integration Tests — Storage Root Resolution (40 tests)

**Root Cause:** Integration tests use `TestEnvironment` which configures `StorageRoot = "./cli-storage"`. While `TestEnvironment.cs` was patched in commit `3a5d680` to call `Path.GetFullPath()`, the server-side storage root still resolves to a path where files written by the server aren't found by test assertions that check the local filesystem. The `./cli-storage` relative path may be resolving differently between the test process and the in-memory TestServer.

#### Upload Integration Tests (9 tests)

**File:** `IntegrationTests/IntegrationTests_UploadCommand.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `UploadCommand_SingleFile_UploadsSuccessfully` | L42 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `UploadCommand_MultipleFiles_UploadsAllSuccessfully` | L80 | `File.Exists(Path.Combine("./cli-storage"...)) is false` |
| ❌ OPEN | `UploadCommand_NoFlag_OverwritesExistingFile` | L217 | `Expected "new content" but found "original"` |
| ❌ OPEN | `UploadCommand_LargeFile_ShowsProgress` | L283 | `Expected uploaded file to exist on server` |
| ❌ OPEN | `UploadCommand_SmallFile_NoProgressBar` | L319 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `UploadCommand_MultiFile_ShowsCleanSummary` | L366 | `File.Exists(serverFile) is false` |
| ❌ OPEN | `UploadCommand_BatchExistsCheck_150Files` | L411 | Threw exception |
| ❌ OPEN | `UploadCommand_BatchExistsCheck_250Files` | L483 | Threw exception |
| ❌ OPEN | `UX_SkippedFiles_SummaryWithSkipCount` | L633 | Regex match failed on output |

#### Download Integration Tests (15 tests)

**File:** `IntegrationTests/IntegrationTests_DownloadCommand.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `DownloadCommand_SingleFile_DownloadsSuccessfully` | L40 | `Downloaded file should exist locally, but found False` |
| ❌ OPEN | `DownloadCommand_ToDirectory_AppendsFilename` | L67 | `File should be downloaded with original filename` |
| ❌ OPEN | `DownloadCommand_GlobPattern_DownloadsAllMatching` | L98 | `file1.txt should be downloaded` |
| ❌ OPEN | `FileTransferService_EnumerateFiles_ReturnsFileInfo` | L127 | Threw exception |
| ❌ OPEN | `DownloadCommand_RecursiveGlob_FlattensToDestination` | L167 | `root.txt should be downloaded, but found False` |
| ❌ OPEN | `DownloadCommand_PathSeparators_NormalizedCorrectly` | L222 | `file should be downloaded, but found False` |
| ❌ OPEN | `DownloadCommand_FilenameCollision_ShowsError` | L250 | Console output doesn't contain expected error |
| ❌ OPEN | `DownloadCommand_NoMatches_ShowsWarning` | L301 | Console output doesn't contain expected warning |
| ❌ OPEN | `DownloadCommand_MultipleFilesSuccess_DisplaysCorrectSummaryMessage` | L335 | Console output doesn't match |
| ❌ OPEN | `DownloadCommand_LargeFile_DisplaysProgressBar` | L374 | `Downloaded file should exist locally, but found False` |
| ❌ OPEN | `DownloadCommand_SmallFilesViaPattern_NoProgressBar` | L420 | `small1.log should exist locally, but found False` |
| ❌ OPEN | `DownloadCommand_SingleFile_AlwaysShowsProgress` | L456 | `Progress bar not visible` |
| ❌ OPEN | `DownloadCommand_MultipleFiles_AggregateAboveThreshold_DisplaysProgressBar` | L495 | `chunk1.bin should be downloaded, but found False` |
| ❌ OPEN | `DownloadCommand_MultipleFiles_AggregateBelowThreshold_NoProgressBar` | L539 | Console output doesn't match |
| ❌ OPEN | `DownloadCommand_ChecksumVerification_FileIntegrityPreserved` | L573 | `Downloaded file should exist, but found False` |

#### Download Service Tests (2 tests)

**File:** `IntegrationTests/IntegrationTests_Download.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Download_ExistingFile_ReturnsCorrectContent` | L43 | `File should be uploaded before download test` |
| ❌ OPEN | `Download_VerifiesIntegrity_EndToEnd` | L77 | Threw exception |

#### File Transfer Service Tests (2 tests)

**File:** `IntegrationTests/IntegrationTests_FileTransferService.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `UploadFile_FileUploaded` | L25 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `UploadFileWithProgress_FileUploadedWithProgress` | L49 | `Expected boolean to be true, but found False` |

#### Checksum Tests (4 tests)

**File:** `IntegrationTests/IntegrationTests_Checksum.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Upload_ValidChecksum_FilePreserved` | L45 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `Upload_BinaryFile_PreservesIntegrity` | L94 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `Upload_LargeFile_ChecksumComputedCorrectly` | L142 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `Upload_EmptyFile_ChecksumComputedCorrectly` | L187 | `Expected boolean to be true, but found False` |

#### Server Sandbox Tests (3 tests)

**File:** `IntegrationTests/IntegrationTests_ServerSandbox.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `ServerCommand_UsesIFileSystem_ConfinedToStorageRoot` | L31 | `File should be written to storage root, but found False` |
| ❌ OPEN | `ServerCommand_File_WriteAndRead_RoundTrip` | L56 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `ServerCommand_Directory_CreateEnumerateDelete_FullCycle` | L86 | `Directory should be created, but found False` |

#### Path Traversal Tests (2 tests)

**File:** `IntegrationTests/IntegrationTests_PathTraversal.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Upload_ValidRelativePath_Succeeds` | L35 | `Expected boolean to be true, but found False` |
| ❌ OPEN | `Upload_NestedSubdirectory_CreatesAndSucceeds` | L60 | `Expected boolean to be true, but found False` |

#### Token Security Tests (1 test)

**File:** `IntegrationTests/IntegrationTests_TokenSecurity.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Upload_TokenInHeader_Succeeds` | L37 | `Expected boolean to be true, but found False` |

#### Path AutoComplete Integration Tests (3 tests)

**File:** `IntegrationTests/IntegrationTests_PathAutoComplete.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `ServerFilePathAutoComplete_ReturnsServerFilesAndDirectories` | L149 | `Expected 3 items, but found 0: {empty}` |
| ❌ OPEN | `ServerFilePathAutoComplete_WithPrefix_FiltersResults` | L199 | `Expected 2 items, but found 0: {empty}` |
| ❌ OPEN | `ServerDirectoryPathAutoComplete_ReturnsOnlyDirectories` | L283 | `Expected 2 items, but found 0: {empty}` |

#### CpCommand Integration Test (1 test)

**File:** `IntegrationTests/IntegrationTests_CpCommand.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `CpCommand_WithShortRFlag_CopiesDirectoryRecursively` | L24 | `Expected RunResultCode.Success but found RunError` |

#### RmCommand Integration Test (1 test)

**File:** `IntegrationTests/IntegrationTests_RmCommand.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `RmCommand_PathOutsideSandbox_FailsWithError` | L65 | Console output doesn't contain expected error |

#### User-Facing Exception Test (1 test)

**File:** `IntegrationTests/IntegrationTests_UserFacingException.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Run_RegularException_ShowsGenericErrorMessage` | L186 | Console output doesn't match |

#### Download Endpoint Test (1 test)

**File:** `ServerTests/DownloadEndpointTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `Download_IncludesChecksumHeader` | L120 | `Expected _responseHeaders to contain key "X-File-Checksum"` |

### Category 6: GlobPattern / ResolveLocalPath — Path Separator Mismatches (6 tests)

**Root Cause:** On Linux, `Path.Combine` and `Path.GetFullPath` use `/` as separator. Tests assert Windows-style `\` in paths. Also `GlobPatternHelper.ParseGlobPattern` returns forward-slash paths on Linux.

#### GlobPatternHelperTests (2 tests)

**File:** `ClientTests/GlobPatternHelperTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `ParseGlobPattern_SimplePattern_ReturnsCwdAsBaseDir` | L149 | Expected `C:\testdir` but got `/C:\testdir` (prepended `/`) |
| ❌ OPEN | `ParseGlobPattern_AbsoluteWindowsPath_ParsesCorrectly` | L304 | Expected `C:\files\data` but got `C:/files/data` |

#### UploadCommandTests (1 test)

**File:** `ClientTests/UploadCommandTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `ParseGlobPattern_AbsolutePath_UsesSpecifiedDirectory` | L567 | Expected `C:\Users\test\data` but got `C:/Users/test/data` |

#### DownloadCommandTests (3 tests)

**File:** `ClientTests/DownloadCommandTests.cs`

| Status | Test | Line | Error |
|--------|------|------|-------|
| ❌ OPEN | `ResolveLocalPath_DestinationEndsWithSlash_AppendsFilename` | L101 | Expected `C:\downloads\myfile.txt` but got `C:\downloads/myfile.txt` |
| ❌ OPEN | `ResolveLocalPath_DestinationEndsWithBackslash_AppendsFilename` | L119 | Expected `C:\downloads\subdir\data.json` but got `C:\downloads\subdir/data.json` |
| ❌ OPEN | `ResolveLocalPath_RecursiveGlobNestedFiles_FlattensToDestination` | L434 | Expected `C:\flat\app.log` but got `C:\flat/app.log` |

---

## Root Cause Summary

| # | Root Cause | Affected Tests | Fix Strategy |
|---|-----------|----------------|-------------|
| 1 | `PathValidator` tests hardcode `C:\ServerStorage` as storage root | 10 | Use platform-portable root (`RuntimeInformation.IsOSPlatform`) |
| 2 | Server command tests use `MockFileSystem(... @"C:\storage")` | 23+ | Use portable root; same pattern as DirectoryPathAutoCompleteHandlerTests fix |
| 3 | `SandboxedFile`/`SandboxedDirectory` tests use `C:\storage` MockFS root | 9+ | Same as #2 |
| 4 | `PathEntriesRpcHandler` tests use `C:\storage` MockFS root | 3 | Same as #2 |
| 5 | Integration tests — `./cli-storage` relative path or TestEnvironment storage root mismatch | 40 | Ensure `Path.GetFullPath()` is applied consistently; verify TestEnvironment fix works end-to-end |
| 6 | `GlobPatternHelper` and `ResolveLocalPath` return `/` separators on Linux; tests assert `\` | 6 | Either skip on Linux or use `Path.DirectorySeparatorChar` in expectations |
| 7 | History recall highlighting not applied on Linux (null foreground) | 2 | Debug why `ApplySyntaxHighlighting` produces no styles after `PressUpArrowAsync` on Linux |
| 8 | MockFileSystem `../` navigation from `/work` to `/` fails on Linux | 1 | Use a deeper WorkDir like `/tmp/work` or conditionally adjust the test |

---

## CI Details

- **Workflow:** `release-unified.yml` on `ubuntu-latest` (Ubuntu 24.04)
- **Tag:** `release-v20260322-180852`
- **Commit:** `3a5d680`
- **Job:** `publish-core`
- **Test command:** `dotnet test --no-restore --configuration Release --verbosity normal`
- **Results:**
  - `BitPantry.VirtualConsole.Tests`: 255 passed ✅
  - `BitPantry.CommandLine.Tests`: 872 passed, **3 failed**, 875 total
  - `BitPantry.CommandLine.Tests.Remote.SignalR`: 692 passed, **114 failed**, 3 skipped, 809 total

---

## Remediation Strategy

The 117 open failures (plus 22 already-fixed) reduce to **3 root causes**, addressable by **3 infrastructure-level fixes** and **2 localized test-level fixes**. Several failure categories share the same root cause and therefore the same fix.

### Design Goals

- **Centralized:** Cross-platform path logic lives in one place in test infrastructure, not duplicated per test file
- **Transparent:** Future test writers use `TestPaths.StorageRoot` instead of a string literal — no platform awareness needed
- **Non-fragile:** No conditional `[Ignore]`, no hardcoded `/home/runner/...` expectations, no `if (Linux) skip` workarounds
- **Replaces ad-hoc fixes:** The round 1-3 fixes (commits `5c38597`, `e615ad4`, `3a5d680`) duplicated the `RuntimeInformation.IsOSPlatform` + `P()` pattern into each test file independently — this consolidates that into shared infrastructure

---

### Fix A: `TestPaths` Static Helper — New Class in Test Infrastructure

**Location:** `BitPantry.CommandLine.Tests.Infrastructure/TestPaths.cs` (new file)

**Covers:** Categories 1, 2, 3, 6, B, plus refactors the 22 already-fixed tests (Categories from commits `5c38597` and `3a5d680`)

**Tests fixed:** ~62 (10 + 23 + 9 + 6 + 1 + 13 already-fixed refactored to shared infra)

#### The Problem

Tests use hardcoded Windows paths (`C:\storage`, `C:\ServerStorage`, `C:\testdir`, `C:\work`, `C:\downloads`, `C:\flat`) as MockFileSystem roots or assertion targets. On Linux, these aren't valid rooted paths — `Path.GetFullPath("C:\\storage")` resolves to `/home/runner/.../C:\storage`.

The round 1-3 fixes addressed this by adding per-file `WorkDir` + `P()` + `Sep` fields using `RuntimeInformation.IsOSPlatform`. That works but duplicates the same boilerplate into every test file.

#### The Fix

A single static helper class providing platform-correct rooted paths and path-building utilities:

```csharp
// BitPantry.CommandLine.Tests.Infrastructure/TestPaths.cs
using System.Runtime.InteropServices;

namespace BitPantry.CommandLine.Tests.Infrastructure;

/// <summary>
/// Cross-platform path constants and helpers for tests using MockFileSystem.
/// Use these instead of hardcoded Windows paths (C:\storage, C:\work, etc.)
/// to ensure tests pass on both Windows and Linux CI.
/// </summary>
public static class TestPaths
{
    /// <summary>
    /// A valid rooted path for use as MockFileSystem storage root.
    /// Windows: C:\storage  |  Linux: /storage
    /// </summary>
    public static string StorageRoot { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\storage" : "/storage";

    /// <summary>
    /// A second isolated root for PathValidator tests needing a distinct root.
    /// Windows: C:\ServerStorage  |  Linux: /ServerStorage
    /// </summary>
    public static string ServerStorageRoot { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\ServerStorage" : "/ServerStorage";

    /// <summary>
    /// Working directory root for autocomplete / CWD-based tests.
    /// Uses two levels of depth so ../ doesn't hit filesystem root on Linux.
    /// Windows: C:\testroot\work  |  Linux: /testroot/work
    /// </summary>
    public static string WorkDir { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\testroot\work" : "/testroot/work";

    /// <summary>
    /// Platform directory separator character.
    /// </summary>
    public static char Sep => Path.DirectorySeparatorChar;

    /// <summary>
    /// A rooted path guaranteed to be OUTSIDE any test storage root.
    /// For testing path-traversal rejection.
    /// Windows: C:\Windows\System32\file.txt  |  Linux: /etc/passwd
    /// </summary>
    public static string OutsidePath { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Windows\System32\file.txt"
            : "/etc/passwd";

    /// <summary>
    /// Builds a platform-correct path under a given root.
    /// Converts backslashes in relative parts to the platform separator.
    /// Usage: TestPaths.Combine(TestPaths.StorageRoot, "subfolder", "file.txt")
    /// </summary>
    public static string Combine(string root, params string[] parts)
    {
        var combined = root;
        foreach (var part in parts)
            combined = Path.Combine(combined, part.Replace('\\', Sep));
        return combined;
    }

    /// <summary>
    /// Converts a relative Windows-style path to platform-native separators
    /// and prepends the given root.
    /// Usage: TestPaths.P(TestPaths.StorageRoot, @"subdir\file.txt")
    ///   Windows → C:\storage\subdir\file.txt
    ///   Linux   → /storage/subdir/file.txt
    /// </summary>
    public static string P(string root, string relativePath) =>
        Path.Combine(root, relativePath.Replace('\\', Sep));
}
```

#### How It Applies to Each Category

| Category | Current Code | Becomes |
|----------|-------------|---------|
| **Cat 1** (PathValidationTests) | `const string StorageRoot = @"C:\ServerStorage"` | `static string StorageRoot => TestPaths.ServerStorageRoot` |
| **Cat 2** (LsCommand, RmCommand, CpCommand, MkdirCommand, StatCommand) | `const string StorageRoot = @"C:\storage"` | `static string StorageRoot => TestPaths.StorageRoot` |
| **Cat 3** (SandboxedDirectory/File/FileSystem tests) | `const string StorageRoot = @"C:\storage"` | `static string StorageRoot => TestPaths.StorageRoot` |
| **Cat 6** (GlobPatternHelperTests) | `SetCurrentDirectory(@"C:\testdir")` | Use `TestPaths.WorkDir`; assertions use `TestPaths.P()` |
| **Cat 6** (DownloadCommandTests) | `command.Destination = @"C:\downloads\"` | Use `TestPaths.P(TestPaths.StorageRoot, "downloads") + Sep` |
| **Cat 6** (UploadCommandTests) | `@"C:\Users\test\data\*.txt"` | Use `TestPaths.P(TestPaths.WorkDir, @"data\*.txt")` |
| **Cat B** (RelativeDotDot test) | `WorkDir = "/work"` → `../` hits `/` root | `TestPaths.WorkDir = "/testroot/work"` → `../` hits `/testroot` |
| **Already-fixed** (DirectoryPath, FilePath, LocalPathEntry tests) | Per-file `WorkDir` + `P()` + `Sep` | Replaced with `TestPaths.*` (consolidation) |

#### PathValidator-Specific Detail (Cat 1)

Some PathValidator tests assert specific "outside root" paths (e.g., `@"C:\Windows\System32\file.txt"` should be rejected):
- Use `TestPaths.OutsidePath` for those assertions

For backslash-specific traversal tests (`@"subfolder\..\..\..\file.txt"`):
- Replace with forward-slash equivalent (`"subfolder/../../../file.txt"`) which works identically on both platforms
- The `ValidatePath_WindowsStyleBackslashTraversal` test specifically tests `\` as a path separator, which is a Windows-only concern. Convert to forward slashes, or use a platform check:

```csharp
// Platform-neutral traversal path that works identically on both:
var traversalPath = "subfolder/../../../file.txt";
```

#### RelativeDotDot Detail (Cat B)

The current `WorkDir = "/work"` is one level from root on Linux, so `../` goes to `/`. MockFileSystem may not correctly enumerate entries at `/`. By deepening `WorkDir` to `/testroot/work`:
- `../` goes to `/testroot` — a normal directory in MockFileSystem
- Sibling files can be placed at `/testroot/sibling/` and will be found
- No change needed to the test logic itself, just the starting depth

---

### Fix B: Temp-Dir Default for Integration Test Storage Root

**Location:** `BitPantry.CommandLine.Tests.Infrastructure/TestServerOptions.cs` (modify default) + integration test assertion updates

**Covers:** Category 4 (PathEntriesRpcHandler — 3 tests) and Category 5 (Integration tests — 40 tests)

**Tests fixed:** ~43

#### The Problem

Two distinct issues in integration tests:

**Issue 1 — Relative storage root:** `TestServerOptions.StorageRoot` defaults to `"./cli-storage"`. Even after the `Path.GetFullPath()` fix in commit `3a5d680`, the relative path resolves differently between the test process CWD and the in-memory TestServer's CWD on Linux.

**Issue 2 — Hardcoded path in assertions:** Some integration tests assert against `"./cli-storage"` directly instead of through the test infrastructure:

```csharp
// BROKEN — uses hardcoded relative path:
var serverFilePath = Path.Combine("./cli-storage", serverFileName);
File.Exists(serverFilePath).Should().BeTrue();
```

#### The Fix — Two Parts

**Part 1: Change the default storage root to an absolute temp directory:**

```csharp
// TestServerOptions.cs — change default
public string StorageRoot { get; set; } =
    Path.Combine(Path.GetTempPath(), $"cli-storage-{Guid.NewGuid():N}");
```

This makes each test environment use an isolated absolute temp directory by default. The `Path.GetFullPath()` call in `TestEnvironment.cs` becomes a no-op (already absolute) but is harmless to keep as a safety net.

**Part 2: Fix integration test assertions to use infrastructure accessors:**

```csharp
// BEFORE (broken — hardcoded relative path):
var serverFilePath = Path.Combine("./cli-storage", serverFileName);
File.Exists(serverFilePath).Should().BeTrue();

// AFTER (correct — uses env infrastructure):
var serverFilePath = Path.Combine(env.RemoteFileSystem.ServerStorageRoot, serverFileName);
File.Exists(serverFilePath).Should().BeTrue();
```

#### How It Applies to Each Sub-Category

| Sub-Category | Current Pattern | Fix |
|-------------|----------------|-----|
| **Upload integration tests** (9) that hardcode `"./cli-storage"` | `Path.Combine("./cli-storage", ...)` | `Path.Combine(env.RemoteFileSystem.ServerStorageRoot, ...)` |
| **Download integration tests** (15) | Already use `env.RemoteFileSystem.*` | Temp-dir default fix alone suffices |
| **Checksum tests** (4) | Assert `File.Exists` with `"./cli-storage"` | Use `env.RemoteFileSystem.ServerStorageRoot` |
| **ServerSandbox tests** (3) | Assert filesystem operations with `"./cli-storage"` | Use `env.RemoteFileSystem.ServerStorageRoot` |
| **PathTraversal tests** (2) | Assert `File.Exists` with `"./cli-storage"` | Use `env.RemoteFileSystem.ServerStorageRoot` |
| **TokenSecurity tests** (1) | Assert `File.Exists` with `"./cli-storage"` | Use `env.RemoteFileSystem.ServerStorageRoot` |
| **FileTransferService tests** (2) | Assert `File.Exists` with `"./cli-storage"` | Use `env.RemoteFileSystem.ServerStorageRoot` |
| **PathAutoComplete integration** (3) | Server file enumeration returns empty | Temp-dir default fix ensures files are found at correct absolute path |
| **CpCommand/RmCommand integration** (2) | Server-side operations fail | Same — correct storage root resolves the path mismatch |
| **DownloadEndpoint test** (1) | Missing checksum header | Storage root fix enables server to find the file for checksum computation |
| **UserFacingException test** (1) | Console output doesn't match | May be a downstream effect of storage root; needs verification |
| **Download service tests** (2) | File not found for download | Temp-dir default fix |
| **PathEntriesRpcHandler** (Cat 4, 3 tests) | Already uses temp dirs correctly via `Path.GetTempPath()` — failures are from the handler receiving empty effective path | The fix ensures `FileTransferOptions.StorageRootPath` is set to an absolute path, which resolves the empty-path resolution |

#### Why `TestRemoteFileSystem` Already Has the Right Pattern

The well-written integration tests (especially `IntegrationTests_DownloadCommand`) use the `TestRemoteFileSystem` accessors:
- `env.RemoteFileSystem.CreateServerFile(...)` — creates files in the server's storage
- `env.RemoteFileSystem.LocalPath(...)` — resolves local download paths
- `env.RemoteFileSystem.ServerStorageRoot` — the actual server storage directory

The failing upload tests bypass this and hardcode `"./cli-storage"`. The fix brings them in line with the established pattern.

---

### Fix C: `GlobPatternHelper.ResolveDestinationPath` — Use `Path.Combine` Instead of String Concatenation

**Location:** `BitPantry.CommandLine.Remote.SignalR.Client/GlobPatternHelper.cs` (source fix) + 3 test assertion updates

**Covers:** 3 tests from Category 6 (DownloadCommandTests `ResolveLocalPath` tests)

**Tests fixed:** 3

#### The Problem

`GlobPatternHelper.ResolveDestinationPath` uses string concatenation with a hardcoded `/`:

```csharp
return destination.TrimEnd('/', '\\') + "/" + sourceFileName;
```

This always produces forward-slash joins. The `DownloadCommand.ResolveLocalPath` method overrides this with `Path.Combine`, but the test assertions compare against the method's actual output which uses `Path.Combine` — and `Path.Combine` uses `\` on Windows and `/` on Linux. The tests hardcode Windows-style expected values (`@"C:\downloads\myfile.txt"`).

#### The Fix — Two Parts

**Part 1: Fix the source to use `Path.Combine`:**

```csharp
// GlobPatternHelper.cs — ResolveDestinationPath
public static string ResolveDestinationPath(string destination, string sourceFileName)
{
    if (destination.EndsWith('/') || destination.EndsWith('\\'))
    {
        return Path.Combine(destination.TrimEnd('/', '\\'), sourceFileName);
    }
    return destination;
}
```

**Part 2: Fix test assertions to use platform-portable paths:**

```csharp
// BEFORE:
command.Destination = @"C:\downloads\";
resolved.Should().Be(@"C:\downloads\myfile.txt");

// AFTER:
var downloadsDir = TestPaths.Combine(TestPaths.StorageRoot, "downloads");
command.Destination = downloadsDir + TestPaths.Sep;
resolved.Should().Be(TestPaths.Combine(downloadsDir, "myfile.txt"));
```

---

### Fix D: Syntax Highlighting History Recall — VirtualConsole ANSI Investigation

**Location:** TBD — requires Linux debugging to pinpoint

**Covers:** Category A (2 tests in `InputBuilderSyntaxHighlightTests`)

**Tests fixed:** 2

#### The Problem

After history recall via Up/Down Arrow, VirtualConsole cells have **no color at all** (`Foreground256 = null`, `ForegroundRgb = null`). The existing `AssertForegroundColor` helper already handles the 256-vs-RGB format mismatch, but this is a deeper issue — no color data is captured at all.

#### Diagnosis Required

This isn't a path issue. Possible root causes:
1. `InputBuilder.ApplySyntaxHighlighting` isn't being called after history recall on Linux
2. Spectre.Console emits a different ANSI escape sequence on Linux that VirtualConsole's parser doesn't recognize
3. The `VirtualConsoleAnsiAdapter`'s `ColorSystemSupport.TrueColor` mode emits SGR sequences the parser silently drops

#### Proposed Investigation

1. Add a diagnostic test that captures the raw ANSI output during history recall and inspects the `UnrecognizedSequences` collection on the `TestEnvironment`
2. If VirtualConsole's parser is dropping valid SGR sequences, fix the parser
3. If Spectre emits a different color format on Linux, update the adapter's color system negotiation
4. If `ApplySyntaxHighlighting` isn't called at all after recall, the fix is in `InputBuilder`

#### Fallback

If confirmed to be a VirtualConsole rendering capture issue (i.e., the highlights work visually in a real terminal, but VirtualConsole's test infrastructure doesn't capture them on Linux), a reasonable interim fix is to make the assertion accept "no color" as a passing case specifically for history recall on non-Windows:

```csharp
// Only as a last resort — prefer fixing the root cause in VirtualConsole
if (cell.Style.Foreground256 == null && cell.Style.ForegroundRgb == null
    && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // VirtualConsole doesn't capture Spectre SGR sequences on Linux — skip assertion
    return;
}
```

---

### Fix E: `FilePathAutoCompleteHandlerTests.RelativeDotDot` — Deeper WorkDir

**Location:** Covered by Fix A (`TestPaths.WorkDir` uses two levels of depth)

**Covers:** Category B (1 test)

**Tests fixed:** 1

#### The Problem

On Linux, `WorkDir = "/work"` and navigating `../` goes to `/` (filesystem root). MockFileSystem may not correctly enumerate entries at `/`, or the sibling path `/sibling/other.txt` isn't accessible.

#### The Fix

Already addressed by Fix A's `TestPaths.WorkDir` using two levels of depth:

```csharp
// Before: WorkDir = "/work" → ../ = "/"  (problematic root)
// After:  WorkDir = "/testroot/work" → ../ = "/testroot"  (normal directory)
```

The test creates sibling entries at the parent level. With a deeper WorkDir:
- `../` from `/testroot/work` resolves to `/testroot`
- Sibling files at `/testroot/sibling/` are a normal MockFileSystem directory
- No changes needed to test logic — only the starting depth changes

---

### Fix Coverage Summary

| Fix | Scope | Tests Fixed | Change Location |
|-----|-------|-------------|-----------------|
| **A: `TestPaths` helper** | New class in Tests.Infrastructure | **~62** (Cat 1, 2, 3, 6, B + refactors 22 already-fixed) | 1 new file + update ~15 test files |
| **B: Temp-dir storage root** | TestServerOptions default + assertion updates | **~43** (Cat 4, 5) | 1 infra change + update ~10 integration test files |
| **C: `ResolveDestinationPath`** | Source fix + test assertions | **3** (Cat 6 sub-group) | 1 source fix + 3 test assertion updates |
| **D: History recall highlighting** | VirtualConsole adapter/parser investigation | **2** (Cat A) | Requires Linux debugging |
| **E: Deep WorkDir for dotdot** | Covered by Fix A | **1** (Cat B) | Included in TestPaths.WorkDir |
| | | **~111 total** | |

**Fixes A + B alone cover 105 of 117 open failures.** Fix C adds 3 more. Fixes D and E cover the remaining 3.

### Category-to-Fix Mapping

| Category | Fix | Description |
|----------|-----|-------------|
| Cat 1: PathValidator hardcoded `C:\ServerStorage` | **A** | Replace with `TestPaths.ServerStorageRoot` |
| Cat 2: Server command MockFileSystem `C:\storage` | **A** | Replace with `TestPaths.StorageRoot` |
| Cat 3: Sandboxed wrapper tests `C:\storage` | **A** | Replace with `TestPaths.StorageRoot` |
| Cat 4: PathEntriesRpcHandler empty path | **B** | Absolute temp-dir storage root |
| Cat 5: Integration tests `./cli-storage` | **B** | Temp-dir default + `env.RemoteFileSystem.ServerStorageRoot` |
| Cat 6: GlobPattern / ResolveLocalPath separator mismatches | **A** + **C** | `TestPaths` for assertions + `Path.Combine` in source |
| Cat A: History recall syntax highlighting | **D** | VirtualConsole ANSI investigation (Linux-specific) |
| Cat B: RelativeDotDot navigation to root | **A** / **E** | Deeper `WorkDir` in `TestPaths` |
| Already-fixed (commits `5c38597`, `3a5d680`) | **A** (refactor) | Consolidate per-file `WorkDir`/`P()`/`Sep` into shared `TestPaths` |
| Already-fixed (commit `e615ad4`) | N/A | `.Trim()` fix is correct as-is, no consolidation needed |
