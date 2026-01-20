#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Analyze workflow artifacts for consistency and integrity.

.DESCRIPTION
    Validates tasks.md format, batch integrity, evidence completeness,
    and other workflow artifacts based on the specified phase.

.PARAMETER Phase
    The phase to analyze: tasks, batches, evidence

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./analyze-workflow.ps1 -Phase tasks -Json
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('tasks', 'batches', 'evidence')]
    [string]$Phase,
    
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

# Source common functions
. "$PSScriptRoot/common.ps1"

# Get feature paths
$paths = Get-FeaturePathsEnv

$issues = @()
$warnings = @()

function Add-Issue {
    param([string]$Category, [string]$Message, [string]$Severity = "ERROR")
    
    $script:issues += [PSCustomObject]@{
        severity = $Severity
        category = $Category
        message = $Message
    }
}

function Add-Warning {
    param([string]$Category, [string]$Message)
    
    $script:warnings += [PSCustomObject]@{
        severity = "WARNING"
        category = $Category
        message = $Message
    }
}

# ============================================================================
# Phase: Tasks - Validate tasks.md format
# ============================================================================
if ($Phase -eq 'tasks') {
    $tasksFile = $paths.TASKS
    
    if (-not (Test-Path $tasksFile -PathType Leaf)) {
        Add-Issue -Category "FileExists" -Message "tasks.md not found at $tasksFile"
    } else {
        $content = Get-Content $tasksFile -Raw
        $taskPattern = '^\s*-\s*\[\s*[xX ]?\s*\]\s+(T\d+)\s+(?:\[depends:([^\]]+)\])?\s*(@test-case:\S+)?\s*(.+)?$'
        
        $tasks = @{}
        $lineNum = 0
        
        foreach ($line in ($content -split "`n")) {
            $lineNum++
            
            # Check for task-like lines
            if ($line -match '^\s*-\s*\[') {
                if ($line -match $taskPattern) {
                    $taskId = $matches[1]
                    $depends = if ($matches[2]) { $matches[2] -split ',' | ForEach-Object { $_.Trim() } } else { @() }
                    $testCase = $matches[3]
                    $description = $matches[4]
                    
                    # Check for duplicate task IDs
                    if ($tasks.ContainsKey($taskId)) {
                        Add-Issue -Category "Duplicate" -Message "Duplicate task ID: $taskId (lines $($tasks[$taskId].line) and $lineNum)"
                    }
                    
                    $tasks[$taskId] = @{
                        line = $lineNum
                        depends = $depends
                        testCase = $testCase
                        description = $description
                    }
                    
                    # Validate @test-case reference
                    if (-not $testCase) {
                        Add-Issue -Category "TestCase" -Message "$taskId (line $lineNum): Missing @test-case reference"
                    }
                    
                    # Validate description
                    if (-not $description -or $description.Trim().Length -eq 0) {
                        Add-Warning -Category "Description" -Message "$taskId (line $lineNum): Missing or empty description"
                    }
                } else {
                    # Line looks like a task but doesn't match pattern
                    if ($line -match 'T\d+') {
                        Add-Warning -Category "Format" -Message "Line $lineNum may be a malformed task: $($line.Trim().Substring(0, [Math]::Min(50, $line.Trim().Length)))..."
                    }
                }
            }
        }
        
        # Validate dependencies exist
        foreach ($taskId in $tasks.Keys) {
            foreach ($depId in $tasks[$taskId].depends) {
                if (-not $tasks.ContainsKey($depId)) {
                    Add-Issue -Category "Dependency" -Message "${taskId}: Invalid dependency [$depId] - task does not exist"
                }
            }
        }
        
        # Check for circular dependencies
        function Test-CircularDep {
            param([string]$TaskId, [hashtable]$Visited, [hashtable]$Stack)
            
            $Visited[$TaskId] = $true
            $Stack[$TaskId] = $true
            
            foreach ($depId in $tasks[$TaskId].depends) {
                if (-not $Visited[$depId]) {
                    $cycle = Test-CircularDep -TaskId $depId -Visited $Visited -Stack $Stack
                    if ($cycle) { return $cycle }
                } elseif ($Stack[$depId]) {
                    return "$depId -> $TaskId"
                }
            }
            
            $Stack[$TaskId] = $false
            return $null
        }
        
        $visited = @{}
        $stack = @{}
        foreach ($taskId in $tasks.Keys) {
            if (-not $visited[$taskId]) {
                $cycle = Test-CircularDep -TaskId $taskId -Visited $visited -Stack $stack
                if ($cycle) {
                    Add-Issue -Category "Circular" -Message "Circular dependency detected: $cycle"
                    break
                }
            }
        }
        
        # Cross-reference with test-cases.md if exists
        $testCasesFile = Join-Path $paths.FEATURE_DIR 'test-cases.md'
        if (Test-Path $testCasesFile -PathType Leaf) {
            $testCasesContent = Get-Content $testCasesFile -Raw
            $testCasePattern = '\|\s*(UX-\d+|CV-\d+|DF-\d+|EH-\d+)\s*\|'
            $definedTestCases = @{}
            
            foreach ($match in [regex]::Matches($testCasesContent, $testCasePattern)) {
                $tcId = $match.Groups[1].Value
                $definedTestCases[$tcId] = $true
            }
            
            # Check each task's test case reference exists
            foreach ($taskId in $tasks.Keys) {
                $tcRef = $tasks[$taskId].testCase -replace '@test-case:', ''
                if ($tcRef -and -not $definedTestCases.ContainsKey($tcRef)) {
                    Add-Warning -Category "TestCase" -Message "${taskId}: Test case $tcRef not found in test-cases.md"
                }
            }
        }
    }
}

# ============================================================================
# Phase: Batches - Validate batch files
# ============================================================================
if ($Phase -eq 'batches') {
    $batchesDir = Join-Path $paths.FEATURE_DIR 'batches'
    
    if (-not (Test-Path $batchesDir -PathType Container)) {
        Add-Issue -Category "Directory" -Message "Batches directory not found. Run create-batch.ps1 first."
    } else {
        $batchFiles = Get-ChildItem -Path $batchesDir -Filter "batch-*.md" | Sort-Object Name
        
        if ($batchFiles.Count -eq 0) {
            Add-Issue -Category "NoBatches" -Message "No batch files found in $batchesDir"
        }
        
        $allBatchTasks = @{}
        $previousBatchTasks = @{}
        
        foreach ($batchFile in $batchFiles) {
            $batchName = $batchFile.BaseName
            $content = Get-Content $batchFile.FullName -Raw
            
            $taskPattern = '^\s*-\s*\[[xX ]\]\s+(T\d+)\s+(?:\[depends:([^\]]+)\])?\s*@test-case:(\S+)\s+(.+)$'
            $batchTasks = @()
            
            foreach ($line in ($content -split "`n")) {
                if ($line -match $taskPattern) {
                    $taskId = $matches[1]
                    $depends = if ($matches[2]) { $matches[2] -split ',' | ForEach-Object { $_.Trim() } } else { @() }
                    
                    $batchTasks += @{
                        id = $taskId
                        depends = $depends
                    }
                    
                    # Check for task in multiple batches
                    if ($allBatchTasks.ContainsKey($taskId)) {
                        Add-Issue -Category "Duplicate" -Message "Task $taskId appears in multiple batches: $($allBatchTasks[$taskId]) and $batchName"
                    }
                    $allBatchTasks[$taskId] = $batchName
                    
                    # Check dependencies are in this or previous batches
                    foreach ($depId in $depends) {
                        if (-not $previousBatchTasks.ContainsKey($depId) -and -not ($batchTasks | Where-Object { $_.id -eq $depId })) {
                            # Dependency might be in a later batch or doesn't exist
                            if (-not $allBatchTasks.ContainsKey($depId)) {
                                Add-Issue -Category "Dependency" -Message "${batchName}/${taskId}: Dependency $depId not in this or previous batch"
                            }
                        }
                    }
                }
            }
            
            # Check batch size
            if ($batchTasks.Count -lt 10 -and $batchFile -ne $batchFiles[-1]) {
                Add-Warning -Category "BatchSize" -Message "$batchName has only $($batchTasks.Count) tasks (recommended: 10-15)"
            }
            if ($batchTasks.Count -gt 15) {
                Add-Warning -Category "BatchSize" -Message "$batchName has $($batchTasks.Count) tasks (recommended: 10-15)"
            }
            
            # Add this batch's tasks to previous for next iteration
            foreach ($task in $batchTasks) {
                $previousBatchTasks[$task.id] = $batchName
            }
        }
    }
}

# ============================================================================
# Phase: Evidence - Validate evidence files
# ============================================================================
if ($Phase -eq 'evidence') {
    $evidenceDir = Join-Path $paths.FEATURE_DIR 'evidence'
    $stateFile = Join-Path $paths.FEATURE_DIR 'batch-state.json'
    
    if (-not (Test-Path $stateFile -PathType Leaf)) {
        Add-Issue -Category "State" -Message "batch-state.json not found"
    } else {
        $state = Get-Content $stateFile -Raw | ConvertFrom-Json
        
        # Determine if active batch is a backfill batch
        $isBackfill = $false
        $activeBatchTasks = @{}
        if ($state.activeBatch) {
            $batchFile = Join-Path $paths.FEATURE_DIR "batches/$($state.activeBatch).md"
            if (Test-Path $batchFile -PathType Leaf) {
                $batchContent = Get-Content $batchFile -Raw
                if ($batchContent -match '(?i)\*\*Type\*\*:\s*backfill') {
                    $isBackfill = $true
                }
                # Extract task IDs from this batch
                $taskMatches = [regex]::Matches($batchContent, '- \[[ x]\] (T\d{3})')
                foreach ($m in $taskMatches) {
                    $activeBatchTasks[$m.Groups[1].Value] = $true
                }
            }
        }
        
        # Get tasks that should have evidence (green or verified) - only for active batch
        foreach ($taskId in ($state.taskStates.PSObject.Properties.Name)) {
            # Skip tasks not in the active batch
            if ($activeBatchTasks.Count -gt 0 -and -not $activeBatchTasks.ContainsKey($taskId)) {
                continue
            }
            
            $taskState = $state.taskStates.$taskId
            
            if ($taskState.phase -in @('green', 'verified')) {
                $evidenceFile = Join-Path $evidenceDir "$taskId.json"
                
                if (-not (Test-Path $evidenceFile -PathType Leaf)) {
                    Add-Issue -Category "MissingEvidence" -Message "$taskId in $($taskState.phase) phase but no evidence file"
                } else {
                    # Validate evidence content
                    $evidence = Get-Content $evidenceFile -Raw | ConvertFrom-Json
                    
                    # Check if this is a setup task (no test case) - skip TDD validation
                    $isSetupTask = [string]::IsNullOrEmpty($evidence.testCase)
                    
                    # RED phase validation - skip for backfill batches and setup tasks
                    if (-not $isBackfill -and -not $isSetupTask) {
                        if (-not $evidence.red) {
                            Add-Issue -Category "Evidence" -Message "${taskId}: Missing RED phase in evidence"
                        } elseif ($evidence.red.exitCode -eq 0) {
                            Add-Issue -Category "Evidence" -Message "${taskId}: RED phase shows passing test (exit code 0)"
                        }
                    }
                    
                    # GREEN phase validation - skip for setup tasks
                    if (-not $isSetupTask) {
                        if (-not $evidence.green) {
                            Add-Issue -Category "Evidence" -Message "${taskId}: Missing GREEN phase in evidence"
                        } elseif ($evidence.green.exitCode -ne 0) {
                            Add-Issue -Category "Evidence" -Message "${taskId}: GREEN phase shows failing test (exit code $($evidence.green.exitCode))"
                        }
                    }
                    
                    # DIFF is optional for backfill and setup tasks
                    if (-not $isBackfill -and -not $isSetupTask -and -not $evidence.diff) {
                        Add-Warning -Category "Evidence" -Message "${taskId}: Missing DIFF section in evidence"
                    }
                    
                    # Check sequence - only for non-backfill and non-setup tasks
                    if (-not $isBackfill -and -not $isSetupTask -and $evidence.red.timestamp -and $evidence.green.timestamp) {
                        $redTime = [DateTime]::Parse($evidence.red.timestamp)
                        $greenTime = [DateTime]::Parse($evidence.green.timestamp)
                        if ($greenTime -lt $redTime) {
                            Add-Issue -Category "Sequence" -Message "${taskId}: GREEN timestamp before RED (invalid sequence)"
                        }
                    }
                }
            }
        }
    }
}

# ============================================================================
# Output Results
# ============================================================================
$criticalCount = ($issues | Where-Object { $_.severity -eq 'ERROR' }).Count
$warningCount = $warnings.Count
$valid = ($criticalCount -eq 0)

if ($Json) {
    [PSCustomObject]@{
        valid = $valid
        phase = $Phase
        criticalIssues = $criticalCount
        warnings = $warningCount
        issues = $issues
        warningList = $warnings
    } | ConvertTo-Json -Depth 5 -Compress
} else {
    if ($valid -and $warningCount -eq 0) {
        Write-Output "✓ $Phase validation passed"
    } elseif ($valid) {
        Write-Output "✓ $Phase validation passed with $warningCount warning(s)"
    } else {
        Write-Output "✗ $Phase validation FAILED"
    }
    
    if ($issues.Count -gt 0) {
        Write-Output ""
        Write-Output "Issues:"
        foreach ($issue in $issues) {
            Write-Output "  [$($issue.severity)] $($issue.category): $($issue.message)"
        }
    }
    
    if ($warnings.Count -gt 0) {
        Write-Output ""
        Write-Output "Warnings:"
        foreach ($warning in $warnings) {
            Write-Output "  [WARNING] $($warning.category): $($warning.message)"
        }
    }
}

if (-not $valid) {
    exit 1
}
