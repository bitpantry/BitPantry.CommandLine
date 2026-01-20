#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Get the next eligible task from the active batch.

.DESCRIPTION
    Reads batch-state.json and the active batch file to determine
    the next task that can be executed (pending with dependencies satisfied).

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./get-next-task.ps1 -Json
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
        [PSCustomObject]@{ status = "NO_STATE"; message = "batch-state.json not found. Run create-batch.ps1 first." } | ConvertTo-Json -Compress
    } else {
        Write-Error "batch-state.json not found. Run create-batch.ps1 first."
    }
    exit 1
}

# Load state
$state = Get-Content $stateFile -Raw | ConvertFrom-Json

# Check if there's a task in progress
if ($state.currentTask) {
    $taskId = $state.currentTask
    $taskState = $state.taskStates.$taskId
    
    # If the task is verified, clear currentTask and continue to find next task
    if ($taskState.status -eq 'verified' -or $taskState.phase -eq 'verified') {
        $state.currentTask = $null
        $state | ConvertTo-Json -Depth 10 | Set-Content -Path $stateFile -Encoding UTF8
        # Continue to find next task (don't exit)
    } else {
        if ($Json) {
            [PSCustomObject]@{
                status = "TASK_IN_PROGRESS"
                taskId = $taskId
                phase = $taskState.phase
                message = "Task $taskId is in progress at phase: $($taskState.phase)"
            } | ConvertTo-Json -Compress
        } else {
            Write-Output "Task in progress: $taskId (phase: $($taskState.phase))"
        }
        exit 0
    }
}

# Load active batch file
$batchFile = Join-Path $paths.FEATURE_DIR 'batches' "$($state.activeBatch).md"
if (-not (Test-Path $batchFile -PathType Leaf)) {
    if ($Json) {
        [PSCustomObject]@{ status = "NO_BATCH"; message = "Active batch file not found: $batchFile" } | ConvertTo-Json -Compress
    } else {
        Write-Error "Active batch file not found: $batchFile"
    }
    exit 1
}

# Parse tasks from batch file
$batchContent = Get-Content $batchFile -Raw
# Updated pattern: @test-case: is now optional
$taskPattern = '^\s*-\s*\[\s*[xX ]?\s*\]\s+(T\d+)\s*(?:\[depends:([^\]]+)\])?\s*(@test-case:\S+)?\s*(.+)$'

$batchTasks = @()
foreach ($line in ($batchContent -split "`n")) {
    if ($line -match $taskPattern) {
        $taskId = $matches[1]
        $depends = if ($matches[2]) { $matches[2] -split ',' | ForEach-Object { $_.Trim() } } else { @() }
        $testCase = if ($matches[3]) { $matches[3] -replace '@test-case:', '' } else { $null }
        $description = $matches[4].Trim()
        
        $batchTasks += [PSCustomObject]@{
            id = $taskId
            depends = $depends
            testCase = $testCase
            description = $description
        }
    }
}

# Find next eligible task
$nextTask = $null
foreach ($task in $batchTasks) {
    $taskState = $state.taskStates.($task.id)
    
    # Skip if not pending
    if ($taskState.phase -ne 'pending') {
        continue
    }
    
    # Check dependencies are satisfied (verified or from previous batch)
    $depsOk = $true
    foreach ($depId in $task.depends) {
        $depState = $state.taskStates.$depId
        if ($depState -and $depState.phase -ne 'verified') {
            # Check if dependency is in a completed batch (then it's satisfied)
            $depInCompletedBatch = $false
            # For now, if state exists and not verified, deps not met
            $depsOk = $false
            break
        }
    }
    
    if ($depsOk) {
        $nextTask = $task
        break
    }
}

if ($null -eq $nextTask) {
    # Check if all tasks are verified
    $allVerified = $true
    $pendingTasks = @()
    foreach ($task in $batchTasks) {
        $taskState = $state.taskStates.($task.id)
        if ($taskState.phase -ne 'verified') {
            $allVerified = $false
            $pendingTasks += [PSCustomObject]@{
                id = $task.id
                phase = $taskState.phase
            }
        }
    }
    
    if ($allVerified) {
        if ($Json) {
            [PSCustomObject]@{
                status = "BATCH_COMPLETE"
                batch = $state.activeBatch
                message = "All tasks in batch are verified. Run batch completion."
            } | ConvertTo-Json -Compress
        } else {
            Write-Output "BATCH_COMPLETE: All tasks verified. Run /speckit.batch complete"
        }
    } else {
        if ($Json) {
            [PSCustomObject]@{
                status = "BLOCKED"
                batch = $state.activeBatch
                message = "No eligible tasks. Some tasks are blocked by dependencies or in non-pending state."
                pendingTasks = $pendingTasks
            } | ConvertTo-Json -Compress
        } else {
            Write-Output "BLOCKED: No eligible tasks available."
            Write-Output "Pending tasks:"
            foreach ($pt in $pendingTasks) {
                Write-Output "  $($pt.id): $($pt.phase)"
            }
        }
    }
    exit 0
}

# Return next task
if ($Json) {
    [PSCustomObject]@{
        status = "READY"
        taskId = $nextTask.id
        testCase = $nextTask.testCase
        description = $nextTask.description
        depends = $nextTask.depends
        batch = $state.activeBatch
    } | ConvertTo-Json -Compress
} else {
    Write-Output "Next task: $($nextTask.id)"
    Write-Output "Test case: $($nextTask.testCase)"
    Write-Output "Description: $($nextTask.description)"
    if ($nextTask.depends.Count -gt 0) {
        Write-Output "Dependencies: $($nextTask.depends -join ', ')"
    }
}
