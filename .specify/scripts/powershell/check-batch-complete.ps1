#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Check if the current batch is ready for completion.

.DESCRIPTION
    Validates that all tasks in the active batch are verified
    and returns completion status.

.PARAMETER Json
    Output results as JSON

.PARAMETER StatusOnly
    Only return status, don't validate completion readiness

.EXAMPLE
    ./check-batch-complete.ps1 -Json
#>

[CmdletBinding()]
param(
    [switch]$Json,
    [switch]$StatusOnly
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
        [PSCustomObject]@{ error = "batch-state.json not found" } | ConvertTo-Json -Compress
    } else {
        Write-Error "batch-state.json not found"
    }
    exit 1
}

# Load state
$state = Get-Content $stateFile -Raw | ConvertFrom-Json

# Load active batch file
$batchFile = Join-Path $paths.FEATURE_DIR 'batches' "$($state.activeBatch).md"
if (-not (Test-Path $batchFile -PathType Leaf)) {
    if ($Json) {
        [PSCustomObject]@{ error = "Batch file not found: $batchFile" } | ConvertTo-Json -Compress
    } else {
        Write-Error "Batch file not found: $batchFile"
    }
    exit 1
}

# Parse tasks from batch file
$batchContent = Get-Content $batchFile -Raw
$taskPattern = '^\s*-\s*\[[xX ]\]\s+(T\d+)'
$batchTasks = @()

foreach ($line in ($batchContent -split "`n")) {
    if ($line -match $taskPattern) {
        $batchTasks += $matches[1]
    }
}

# Count tasks by phase
$phaseCounts = @{
    pending = 0
    started = 0
    red = 0
    green = 0
    verified = 0
    failed = 0
}

$incompleteTasks = @()

foreach ($taskId in $batchTasks) {
    $taskState = $state.taskStates.$taskId
    $phase = if ($taskState) { $taskState.phase } else { 'pending' }
    
    if ($phaseCounts.ContainsKey($phase)) {
        $phaseCounts[$phase]++
    } else {
        $phaseCounts[$phase] = 1
    }
    
    if ($phase -ne 'verified') {
        $incompleteTasks += [PSCustomObject]@{
            id = $taskId
            phase = $phase
            failureReason = $taskState.failureReason
        }
    }
}

$totalTasks = $batchTasks.Count
$verifiedTasks = $phaseCounts['verified']
$isComplete = ($verifiedTasks -eq $totalTasks) -and ($null -eq $state.currentTask)

# Check for blocking issues
$blockingIssues = @()

if ($state.currentTask) {
    $blockingIssues += "Task $($state.currentTask) is still in progress"
}

if ($phaseCounts['failed'] -gt 0) {
    $blockingIssues += "$($phaseCounts['failed']) task(s) in failed state - need recovery"
}

if ($phaseCounts['pending'] -gt 0 -or $phaseCounts['started'] -gt 0 -or $phaseCounts['red'] -gt 0 -or $phaseCounts['green'] -gt 0) {
    $blockingIssues += "Not all tasks are verified"
}

# Output results
if ($Json) {
    [PSCustomObject]@{
        batch = $state.activeBatch
        isComplete = $isComplete
        totalTasks = $totalTasks
        verifiedTasks = $verifiedTasks
        progress = "$verifiedTasks/$totalTasks"
        currentTask = $state.currentTask
        phaseCounts = [PSCustomObject]$phaseCounts
        incompleteTasks = $incompleteTasks
        blockingIssues = $blockingIssues
        totalBatches = $state.totalBatches
        completedBatches = $state.completedBatches.Count
    } | ConvertTo-Json -Depth 5 -Compress
} else {
    Write-Output "Batch: $($state.activeBatch)"
    Write-Output "Progress: $verifiedTasks/$totalTasks verified"
    Write-Output ""
    Write-Output "Task phases:"
    Write-Output "  Pending:  $($phaseCounts['pending'])"
    Write-Output "  Started:  $($phaseCounts['started'])"
    Write-Output "  Red:      $($phaseCounts['red'])"
    Write-Output "  Green:    $($phaseCounts['green'])"
    Write-Output "  Verified: $($phaseCounts['verified'])"
    Write-Output "  Failed:   $($phaseCounts['failed'])"
    
    if ($isComplete) {
        Write-Output ""
        Write-Output "✓ Batch is ready for completion"
    } else {
        Write-Output ""
        Write-Output "✗ Batch is NOT ready for completion"
        foreach ($issue in $blockingIssues) {
            Write-Output "  - $issue"
        }
        
        if ($incompleteTasks.Count -gt 0 -and $incompleteTasks.Count -le 5) {
            Write-Output ""
            Write-Output "Incomplete tasks:"
            foreach ($task in $incompleteTasks) {
                $extra = if ($task.failureReason) { " ($($task.failureReason))" } else { "" }
                Write-Output "  - $($task.id): $($task.phase)$extra"
            }
        }
    }
}

if (-not $StatusOnly -and -not $isComplete) {
    exit 1
}
