#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Create task batches from tasks.md and initialize batch state.

.DESCRIPTION
    Parses tasks.md, partitions tasks into batches of 10-15 tasks respecting
    dependencies, creates batch files, and initializes batch-state.json.

.PARAMETER BatchSize
    Target batch size (default: 12, min: 10, max: 15)

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./create-batch.ps1 -Json
#>

[CmdletBinding()]
param(
    [int]$BatchSize = 12,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

# Source common functions
. "$PSScriptRoot/common.ps1"

# Get feature paths
$paths = Get-FeaturePathsEnv

# Validate tasks.md exists
if (-not (Test-Path $paths.TASKS -PathType Leaf)) {
    if ($Json) {
        [PSCustomObject]@{ error = "tasks.md not found"; path = $paths.TASKS } | ConvertTo-Json -Compress
    } else {
        Write-Error "tasks.md not found at $($paths.TASKS)"
    }
    exit 1
}

# Parse tasks from tasks.md
$tasksContent = Get-Content $paths.TASKS -Raw
$taskPattern = '^\s*-\s*\[\s*[xX ]?\s*\]\s+(T\d+)\s+(?:\[depends:([^\]]+)\])?\s*(@test-case:\S+)\s+(.+)$'

$tasks = @()
foreach ($line in ($tasksContent -split "`n")) {
    if ($line -match $taskPattern) {
        $taskId = $matches[1]
        $depends = if ($matches[2]) { $matches[2] -split ',' | ForEach-Object { $_.Trim() } } else { @() }
        $testCase = $matches[3]
        $description = $matches[4].Trim()
        
        $tasks += [PSCustomObject]@{
            id = $taskId
            depends = $depends
            testCase = $testCase
            description = $description
            line = $line.Trim()
        }
    }
}

if ($tasks.Count -eq 0) {
    if ($Json) {
        [PSCustomObject]@{ error = "No valid tasks found in tasks.md"; pattern = "Expected format: - [ ] T### @test-case:XX-### Description" } | ConvertTo-Json -Compress
    } else {
        Write-Error "No valid tasks found in tasks.md. Expected format: - [ ] T### [depends:T###] @test-case:XX-### Description"
    }
    exit 1
}

# Topological sort respecting dependencies
function Get-TopologicalOrder {
    param([array]$Tasks)
    
    $result = @()
    $visited = @{}
    $temp = @{}
    
    function Visit($task) {
        if ($temp[$task.id]) {
            throw "Circular dependency detected involving $($task.id)"
        }
        if (-not $visited[$task.id]) {
            $temp[$task.id] = $true
            foreach ($depId in $task.depends) {
                $dep = $Tasks | Where-Object { $_.id -eq $depId }
                if ($dep) {
                    Visit $dep
                }
            }
            $temp[$task.id] = $false
            $visited[$task.id] = $true
            $script:result += $task
        }
    }
    
    foreach ($task in $Tasks) {
        if (-not $visited[$task.id]) {
            Visit $task
        }
    }
    
    return $result
}

try {
    $sortedTasks = Get-TopologicalOrder -Tasks $tasks
} catch {
    if ($Json) {
        [PSCustomObject]@{ error = $_.Exception.Message } | ConvertTo-Json -Compress
    } else {
        Write-Error $_.Exception.Message
    }
    exit 1
}

# Partition into batches
$batches = @()
$currentBatch = @()

foreach ($task in $sortedTasks) {
    $currentBatch += $task
    
    # Check if batch is full
    if ($currentBatch.Count -ge $BatchSize) {
        $batches += ,@($currentBatch)
        $currentBatch = @()
    }
}

# Add remaining tasks to last batch
if ($currentBatch.Count -gt 0) {
    # If last batch is too small and we have previous batches, consider merging
    if ($currentBatch.Count -lt 10 -and $batches.Count -gt 0) {
        # Only merge if it won't exceed 15
        $lastBatch = $batches[-1]
        if (($lastBatch.Count + $currentBatch.Count) -le 15) {
            $batches[-1] = $lastBatch + $currentBatch
        } else {
            $batches += ,@($currentBatch)
        }
    } else {
        $batches += ,@($currentBatch)
    }
}

# Create batches directory
$batchesDir = Join-Path $paths.FEATURE_DIR 'batches'
if (-not (Test-Path $batchesDir)) {
    New-Item -Path $batchesDir -ItemType Directory -Force | Out-Null
}

# Create evidence directory
$evidenceDir = Join-Path $paths.FEATURE_DIR 'evidence'
if (-not (Test-Path $evidenceDir)) {
    New-Item -Path $evidenceDir -ItemType Directory -Force | Out-Null
}

# Get feature name from branch
$featureName = $paths.CURRENT_BRANCH -replace '^\d+-', ''

# Create batch files
$batchFiles = @()
for ($i = 0; $i -lt $batches.Count; $i++) {
    $batchNum = $i + 1
    $batchName = "batch-{0:D3}" -f $batchNum
    $batchFile = Join-Path $batchesDir "$batchName.md"
    $batch = $batches[$i]
    
    $content = @"
# Batch $batchNum`: $featureName

**Created**: $(Get-Date -Format 'yyyy-MM-dd')
**Status**: pending
**Tasks**: 0 of $($batch.Count) complete

## Tasks

"@
    
    foreach ($task in $batch) {
        $content += "- [ ] $($task.id)"
        if ($task.depends.Count -gt 0) {
            $content += " [depends:$($task.depends -join ',')]"
        }
        $content += " $($task.testCase) $($task.description)`n"
    }
    
    $content += @"

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
"@
    
    Set-Content -Path $batchFile -Value $content -Encoding UTF8
    $batchFiles += $batchName
}

# Initialize batch state
$taskStates = @{}
foreach ($task in $sortedTasks) {
    $taskStates[$task.id] = @{ phase = "pending" }
}

$state = [ordered]@{
    activeBatch = $batchFiles[0]
    currentTask = $null
    taskStates = $taskStates
    batchStatus = "pending"
    completedBatches = @()
    totalBatches = $batches.Count
    created = (Get-Date -Format 'o')
}

$stateFile = Join-Path $paths.FEATURE_DIR 'batch-state.json'
$state | ConvertTo-Json -Depth 10 | Set-Content -Path $stateFile -Encoding UTF8

# Output results
if ($Json) {
    [PSCustomObject]@{
        success = $true
        batchesCreated = $batches.Count
        totalTasks = $sortedTasks.Count
        activeBatch = $batchFiles[0]
        batchDir = $batchesDir
        stateFile = $stateFile
        batches = @(
            for ($i = 0; $i -lt $batches.Count; $i++) {
                [PSCustomObject]@{
                    name = $batchFiles[$i]
                    taskCount = $batches[$i].Count
                }
            }
        )
    } | ConvertTo-Json -Depth 5 -Compress
} else {
    Write-Output "Batches created: $($batches.Count)"
    Write-Output "Total tasks: $($sortedTasks.Count)"
    Write-Output "Active batch: $($batchFiles[0])"
    Write-Output ""
    for ($i = 0; $i -lt $batches.Count; $i++) {
        Write-Output "  $($batchFiles[$i]): $($batches[$i].Count) tasks"
    }
}
