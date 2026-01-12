#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Advance to the next batch after completing the current one.

.DESCRIPTION
    Marks the current batch as complete and activates the next batch.
    Resets task states for the new batch.

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./advance-batch.ps1 -Json
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
$batchesDir = Join-Path $paths.FEATURE_DIR 'batches'

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

$currentBatch = $state.activeBatch

# Find all batch files
$batchFiles = Get-ChildItem -Path $batchesDir -Filter "batch-*.md" | 
    Sort-Object Name | 
    ForEach-Object { $_.BaseName }

# Find current batch index
$currentIndex = $batchFiles.IndexOf($currentBatch)

if ($currentIndex -lt 0) {
    if ($Json) {
        [PSCustomObject]@{ error = "Current batch $currentBatch not found in batches directory" } | ConvertTo-Json -Compress
    } else {
        Write-Error "Current batch $currentBatch not found"
    }
    exit 1
}

# Add to completed batches - ensure property exists
if (-not (Get-Member -InputObject $state -Name 'completedBatches' -MemberType NoteProperty)) {
    $state | Add-Member -NotePropertyName 'completedBatches' -NotePropertyValue @()
}
$state.completedBatches = @($state.completedBatches) + $currentBatch

# Update batch file status
$currentBatchFile = Join-Path $batchesDir "$currentBatch.md"
if (Test-Path $currentBatchFile -PathType Leaf) {
    $content = Get-Content $currentBatchFile -Raw
    $content = $content -replace '(\*\*Status\*\*:\s*)in-progress', '${1}complete'
    $content = $content -replace '(\*\*Status\*\*:\s*)pending', '${1}complete'
    Set-Content -Path $currentBatchFile -Value $content -Encoding UTF8
}

# Check if there's a next batch
$nextBatch = $null
if ($currentIndex -lt ($batchFiles.Count - 1)) {
    $nextBatch = $batchFiles[$currentIndex + 1]
    
    # Load next batch and initialize task states
    $nextBatchFile = Join-Path $batchesDir "$nextBatch.md"
    $nextBatchContent = Get-Content $nextBatchFile -Raw
    $taskPattern = '^\s*-\s*\[\s*\]\s+(T\d+)'
    
    foreach ($line in ($nextBatchContent -split "`n")) {
        if ($line -match $taskPattern) {
            $taskId = $matches[1]
            if (-not $state.taskStates.$taskId) {
                $state.taskStates | Add-Member -NotePropertyName $taskId -NotePropertyValue @{ phase = 'pending' } -Force
            } else {
                $state.taskStates.$taskId.phase = 'pending'
            }
        }
    }
    
    $state.activeBatch = $nextBatch
    $state.currentTask = $null
    $state.batchStatus = 'pending'
} else {
    # No more batches
    $state.activeBatch = $null
    $state.currentTask = $null
    $state.batchStatus = 'all-complete'
}

# Save state
$state | ConvertTo-Json -Depth 10 | Set-Content -Path $stateFile -Encoding UTF8

# Output result
if ($Json) {
    [PSCustomObject]@{
        success = $true
        completedBatch = $currentBatch
        nextBatch = $nextBatch
        allComplete = ($null -eq $nextBatch)
        completedBatches = $state.completedBatches.Count
        totalBatches = $batchFiles.Count
    } | ConvertTo-Json -Compress
} else {
    Write-Output "✓ Completed batch: $currentBatch"
    if ($nextBatch) {
        Write-Output "→ Activated batch: $nextBatch"
        Write-Output "  Progress: $($state.completedBatches.Count)/$($batchFiles.Count) batches complete"
    } else {
        Write-Output ""
        Write-Output "🎉 All batches complete!"
        Write-Output "   Total batches: $($batchFiles.Count)"
    }
}
