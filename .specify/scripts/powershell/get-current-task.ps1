#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Get the current task in progress.

.DESCRIPTION
    Reads batch-state.json to find the task currently being executed.

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./get-current-task.ps1 -Json
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

# Check if there's a current task
if (-not $state.currentTask) {
    if ($Json) {
        [PSCustomObject]@{ status = "NO_TASK"; message = "No task currently in progress" } | ConvertTo-Json -Compress
    } else {
        Write-Output "No task currently in progress"
    }
    exit 0
}

$taskId = $state.currentTask
$taskState = $state.taskStates.$taskId

# Load batch file to get task details
$batchFile = Join-Path $paths.FEATURE_DIR 'batches' "$($state.activeBatch).md"
$taskDetails = $null

if (Test-Path $batchFile -PathType Leaf) {
    $batchContent = Get-Content $batchFile -Raw
    $taskPattern = "^\s*-\s*\[\s*[xX ]?\s*\]\s+($taskId)\s+(?:\[depends:([^\]]+)\])?\s*(@test-case:\S+)\s+(.+)$"
    
    foreach ($line in ($batchContent -split "`n")) {
        if ($line -match $taskPattern) {
            $testCase = $matches[3] -replace '@test-case:', ''
            $description = $matches[4].Trim()
            $taskDetails = [PSCustomObject]@{
                testCase = $testCase
                description = $description
            }
            break
        }
    }
}

if ($Json) {
    $result = [PSCustomObject]@{
        status = "IN_PROGRESS"
        taskId = $taskId
        phase = $taskState.phase
        batch = $state.activeBatch
    }
    
    if ($taskDetails) {
        $result | Add-Member -NotePropertyName testCase -NotePropertyValue $taskDetails.testCase
        $result | Add-Member -NotePropertyName description -NotePropertyValue $taskDetails.description
    }
    
    if ($taskState.failureReason) {
        $result | Add-Member -NotePropertyName failureReason -NotePropertyValue $taskState.failureReason
    }
    
    $result | ConvertTo-Json -Compress
} else {
    Write-Output "Current task: $taskId"
    Write-Output "Phase: $($taskState.phase)"
    Write-Output "Batch: $($state.activeBatch)"
    if ($taskDetails) {
        Write-Output "Test case: $($taskDetails.testCase)"
        Write-Output "Description: $($taskDetails.description)"
    }
    if ($taskState.failureReason) {
        Write-Output "Failure reason: $($taskState.failureReason)"
    }
}
