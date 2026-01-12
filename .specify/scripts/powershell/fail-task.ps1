#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Mark a task as failed with a reason.

.DESCRIPTION
    Updates batch-state.json to mark the task as failed,
    recording the failure reason for recovery.

.PARAMETER TaskId
    The task ID that failed (e.g., T001)

.PARAMETER Reason
    The failure reason/code

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./fail-task.ps1 -TaskId T001 -Reason "RED_PASSED" -Json
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$TaskId,
    
    [Parameter(Mandatory)]
    [string]$Reason,
    
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

# Source common functions
. "$PSScriptRoot/common.ps1"

# Get feature paths
$paths = Get-FeaturePathsEnv
$stateFile = Join-Path $paths.FEATURE_DIR 'batch-state.json'

# Load state
if (-not (Test-Path $stateFile -PathType Leaf)) {
    if ($Json) {
        [PSCustomObject]@{ error = "batch-state.json not found" } | ConvertTo-Json -Compress
    } else {
        Write-Error "batch-state.json not found"
    }
    exit 1
}

$state = Get-Content $stateFile -Raw | ConvertFrom-Json

# Update task state
$state.taskStates.$TaskId.phase = 'failed'
$state.taskStates.$TaskId | Add-Member -NotePropertyName failureReason -NotePropertyValue $Reason -Force
$state.taskStates.$TaskId | Add-Member -NotePropertyName failedAt -NotePropertyValue (Get-Date -Format 'o') -Force

# Keep currentTask set so recovery knows which task to fix
# $state.currentTask stays as TaskId

# Save state
$state | ConvertTo-Json -Depth 10 | Set-Content -Path $stateFile -Encoding UTF8

# Output result
if ($Json) {
    [PSCustomObject]@{
        success = $true
        taskId = $TaskId
        phase = 'failed'
        reason = $Reason
        batch = $state.activeBatch
    } | ConvertTo-Json -Compress
} else {
    Write-Output "✗ Task $TaskId marked as failed"
    Write-Output "Reason: $Reason"
    Write-Output "Run /speckit.recover to diagnose and fix"
}
