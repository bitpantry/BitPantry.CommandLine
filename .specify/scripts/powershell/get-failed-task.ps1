#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Get details of the failed task for recovery.

.DESCRIPTION
    Reads batch-state.json to find the task in failed state
    and returns its details including failure reason.

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./get-failed-task.ps1 -Json
#>

[CmdletBinding()]
param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

# Source common functions
. "$PSScriptRoot/common.ps1"

# Get feature paths
$paths = Get-FeaturePathsEnv
$stateFile = Join-Path $paths.FEATURE_DIR 'batch-state.json'
$evidenceDir = Join-Path $paths.FEATURE_DIR 'evidence'

# Check state file exists
if (-not (Test-Path $stateFile -PathType Leaf)) {
    if ($Json) {
        [PSCustomObject]@{ status = "NO_STATE"; message = "batch-state.json not found" } | ConvertTo-Json -Compress
    } else {
        Write-Error "batch-state.json not found"
    }
    exit 1
}

# Load state
$state = Get-Content $stateFile -Raw | ConvertFrom-Json

# Find failed task (prioritize currentTask if it's failed)
$failedTaskId = $null
$failedTaskState = $null

if ($state.currentTask) {
    $taskState = $state.taskStates.($state.currentTask)
    if ($taskState.phase -eq 'failed') {
        $failedTaskId = $state.currentTask
        $failedTaskState = $taskState
    }
}

# If no current failed task, search all tasks in batch
if (-not $failedTaskId) {
    $batchFile = Join-Path $paths.FEATURE_DIR 'batches' "$($state.activeBatch).md"
    if (Test-Path $batchFile -PathType Leaf) {
        $batchContent = Get-Content $batchFile -Raw
        $taskPattern = '^\s*-\s*\[[xX ]\]\s+(T\d+)'
        
        foreach ($line in ($batchContent -split "`n")) {
            if ($line -match $taskPattern) {
                $tid = $matches[1]
                $ts = $state.taskStates.$tid
                if ($ts.phase -eq 'failed') {
                    $failedTaskId = $tid
                    $failedTaskState = $ts
                    break
                }
            }
        }
    }
}

if (-not $failedTaskId) {
    if ($Json) {
        [PSCustomObject]@{ status = "NO_FAILED_TASK"; message = "No failed task found in current batch" } | ConvertTo-Json -Compress
    } else {
        Write-Output "No failed task found in current batch"
    }
    exit 0
}

# Get task details from batch file
$taskDetails = $null
$batchFile = Join-Path $paths.FEATURE_DIR 'batches' "$($state.activeBatch).md"
if (Test-Path $batchFile -PathType Leaf) {
    $batchContent = Get-Content $batchFile -Raw
    $taskPattern = "^\s*-\s*\[[xX ]\]\s+($failedTaskId)\s+(?:\[depends:([^\]]+)\])?\s*@test-case:(\S+)\s+(.+)$"
    
    foreach ($line in ($batchContent -split "`n")) {
        if ($line -match $taskPattern) {
            $taskDetails = [PSCustomObject]@{
                depends = if ($matches[2]) { $matches[2] -split ',' | ForEach-Object { $_.Trim() } } else { @() }
                testCase = $matches[3]
                description = $matches[4].Trim()
            }
            break
        }
    }
}

# Load evidence if exists
$evidence = $null
$evidenceFile = Join-Path $evidenceDir "$failedTaskId.json"
if (Test-Path $evidenceFile -PathType Leaf) {
    $evidence = Get-Content $evidenceFile -Raw | ConvertFrom-Json
}

# Output result
if ($Json) {
    $result = [PSCustomObject]@{
        status = "FAILED_TASK_FOUND"
        taskId = $failedTaskId
        failureReason = $failedTaskState.failureReason
        failedAt = $failedTaskState.failedAt
        batch = $state.activeBatch
    }
    
    if ($taskDetails) {
        $result | Add-Member -NotePropertyName testCase -NotePropertyValue $taskDetails.testCase
        $result | Add-Member -NotePropertyName description -NotePropertyValue $taskDetails.description
    }
    
    if ($evidence) {
        $evidenceSummary = [PSCustomObject]@{
            hasRed = $null -ne $evidence.red
            hasGreen = $null -ne $evidence.green
            hasDiff = $null -ne $evidence.diff
        }
        if ($evidence.red) {
            $evidenceSummary | Add-Member -NotePropertyName redExitCode -NotePropertyValue $evidence.red.exitCode
        }
        if ($evidence.green) {
            $evidenceSummary | Add-Member -NotePropertyName greenExitCode -NotePropertyValue $evidence.green.exitCode
        }
        $result | Add-Member -NotePropertyName evidence -NotePropertyValue $evidenceSummary
    }
    
    $result | ConvertTo-Json -Depth 5 -Compress
} else {
    Write-Output "Failed task: $failedTaskId"
    Write-Output "Failure reason: $($failedTaskState.failureReason)"
    Write-Output "Failed at: $($failedTaskState.failedAt)"
    if ($taskDetails) {
        Write-Output "Test case: $($taskDetails.testCase)"
        Write-Output "Description: $($taskDetails.description)"
    }
    if ($evidence) {
        Write-Output ""
        Write-Output "Evidence status:"
        Write-Output "  RED: $(if ($evidence.red) { 'present (exit code ' + $evidence.red.exitCode + ')' } else { 'missing' })"
        Write-Output "  GREEN: $(if ($evidence.green) { 'present (exit code ' + $evidence.green.exitCode + ')' } else { 'missing' })"
        Write-Output "  DIFF: $(if ($evidence.diff) { 'present' } else { 'missing' })"
    }
}
