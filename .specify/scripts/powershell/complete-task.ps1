#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Mark a task as verified and complete.

.DESCRIPTION
    Updates batch-state.json to mark the task as verified,
    marks the task checkbox in the batch file, and clears currentTask.

.PARAMETER TaskId
    The task ID to complete (e.g., T001)

.PARAMETER Force
    Force completion even if evidence validation would fail (requires user approval)

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./complete-task.ps1 -TaskId T001 -Json
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$TaskId,
    
    [switch]$Force,
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

# Validate this is the current task (unless force)
if (-not $Force -and $state.currentTask -ne $TaskId) {
    if ($Json) {
        [PSCustomObject]@{ 
            error = "Task $TaskId is not the current task"
            currentTask = $state.currentTask
        } | ConvertTo-Json -Compress
    } else {
        Write-Error "Task $TaskId is not the current task. Current: $($state.currentTask)"
    }
    exit 1
}

# Update state
$state.taskStates.$TaskId.phase = 'verified'
$state.taskStates.$TaskId | Add-Member -NotePropertyName status -NotePropertyValue 'verified' -Force
$state.taskStates.$TaskId | Add-Member -NotePropertyName verifiedAt -NotePropertyValue (Get-Date -Format 'o') -Force
$state.currentTask = $null

# Update batch file - mark task as complete
$batchFile = Join-Path $paths.FEATURE_DIR 'batches' "$($state.activeBatch).md"
if (Test-Path $batchFile -PathType Leaf) {
    $content = Get-Content $batchFile -Raw
    
    # Replace [ ] with [X] for this task
    $pattern = "(-\s*\[)\s*(\]\s+$TaskId\s)"
    $replacement = '$1X$2'
    $newContent = $content -replace $pattern, $replacement
    
    # Update task count in header
    $completedCount = ([regex]::Matches($newContent, '-\s*\[X\]')).Count
    $totalCount = ([regex]::Matches($newContent, '-\s*\[[xX ]\]')).Count
    $newContent = $newContent -replace '(\*\*Tasks\*\*:\s*)\d+(\s*of\s*)\d+', "`${1}$completedCount`${2}$totalCount"
    
    # Update status if all complete
    if ($completedCount -eq $totalCount) {
        $newContent = $newContent -replace '(\*\*Status\*\*:\s*)in-progress', '${1}complete'
    } else {
        $newContent = $newContent -replace '(\*\*Status\*\*:\s*)pending', '${1}in-progress'
    }
    
    Set-Content -Path $batchFile -Value $newContent -Encoding UTF8
}

# Save state
$state | ConvertTo-Json -Depth 10 | Set-Content -Path $stateFile -Encoding UTF8

# Count progress
$batchTasks = @()
$batchContent = Get-Content $batchFile -Raw
$taskPattern = '^\s*-\s*\[[xX ]\]\s+(T\d+)'
foreach ($line in ($batchContent -split "`n")) {
    if ($line -match $taskPattern) {
        $batchTasks += $matches[1]
    }
}

$verifiedCount = 0
foreach ($tid in $batchTasks) {
    if ($state.taskStates.$tid.phase -eq 'verified') {
        $verifiedCount++
    }
}

# Output result
if ($Json) {
    [PSCustomObject]@{
        success = $true
        taskId = $TaskId
        phase = 'verified'
        batch = $state.activeBatch
        progress = "$verifiedCount/$($batchTasks.Count)"
        batchComplete = ($verifiedCount -eq $batchTasks.Count)
    } | ConvertTo-Json -Compress
} else {
    Write-Output "✓ Task $TaskId marked as verified"
    Write-Output "Batch progress: $verifiedCount/$($batchTasks.Count)"
    if ($verifiedCount -eq $batchTasks.Count) {
        Write-Output "Batch complete! Run /speckit.batch complete"
    }
}
