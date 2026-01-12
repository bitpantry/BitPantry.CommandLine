#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Record a task phase (started, red, green) and capture evidence.

.DESCRIPTION
    Updates batch-state.json with the task phase and creates/updates
    the evidence file with test output and diff information.

.PARAMETER TaskId
    The task ID (e.g., T001)

.PARAMETER Phase
    The phase to record: started, red, green

.PARAMETER TestCommand
    The test command that was run (for red/green phases)

.PARAMETER ExitCode
    The exit code from the test run

.PARAMETER TestOutput
    The test output/error message

.PARAMETER TestFile
    The test file path (for red phase)

.PARAMETER TestMethod
    The test method name (for red phase)

.PARAMETER Retry
    Indicates this is a retry after recovery

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./record-task-phase.ps1 -TaskId T001 -Phase red -TestCommand "dotnet test" -ExitCode 1 -TestOutput "Failed"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$TaskId,
    
    [Parameter(Mandatory)]
    [ValidateSet('started', 'red', 'green')]
    [string]$Phase,
    
    [string]$TestCommand,
    [int]$ExitCode = -1,
    [string]$TestOutput,
    [string]$TestFile,
    [string]$TestMethod,
    [switch]$Retry,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

# Source common functions
. "$PSScriptRoot/common.ps1"

# Get feature paths
$paths = Get-FeaturePathsEnv
$stateFile = Join-Path $paths.FEATURE_DIR 'batch-state.json'
$evidenceDir = Join-Path $paths.FEATURE_DIR 'evidence'
$evidenceFile = Join-Path $evidenceDir "$TaskId.json"

# Ensure evidence directory exists
if (-not (Test-Path $evidenceDir)) {
    New-Item -Path $evidenceDir -ItemType Directory -Force | Out-Null
}

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

# Get current timestamp
$timestamp = Get-Date -Format 'o'

# Load or create evidence
$evidence = $null
if (Test-Path $evidenceFile -PathType Leaf) {
    $evidence = Get-Content $evidenceFile -Raw | ConvertFrom-Json
} else {
    # Get test case from batch file
    $testCase = ""
    $batchFile = Join-Path $paths.FEATURE_DIR 'batches' "$($state.activeBatch).md"
    if (Test-Path $batchFile -PathType Leaf) {
        $batchContent = Get-Content $batchFile -Raw
        $taskPattern = "^\s*-\s*\[\s*[xX ]?\s*\]\s+($TaskId)\s+(?:\[depends:[^\]]+\])?\s*@test-case:(\S+)"
        if ($batchContent -match $taskPattern) {
            $testCase = $matches[2]
        }
    }
    
    $evidence = [ordered]@{
        taskId = $TaskId
        testCase = $testCase
    }
}

# Ensure task state exists (for any phase)
if (-not $state.taskStates.$TaskId) {
    $state.taskStates | Add-Member -NotePropertyName $TaskId -NotePropertyValue @{ phase = 'pending'; status = 'not-started' } -Force
}

# Always set currentTask when recording a phase for a task (fixes orphaned in-progress states)
if ($state.currentTask -ne $TaskId) {
    $state.currentTask = $TaskId
}

# Update based on phase
switch ($Phase) {
    'started' {
        # Mark task as started, set as current task
        $state.taskStates.$TaskId.phase = 'started'
        $state.taskStates.$TaskId | Add-Member -NotePropertyName status -NotePropertyValue 'in-progress' -Force
        $state.batchStatus = 'in-progress'
    }
    
    'red' {
        # Record RED phase evidence
        $redData = [ordered]@{
            timestamp = $timestamp
            testCommand = $TestCommand
            exitCode = $ExitCode
            output = $TestOutput
        }
        
        if ($TestFile) { $redData.testFile = $TestFile }
        if ($TestMethod) { $redData.testMethod = $TestMethod }
        
        $evidence.red = $redData
        $state.taskStates.$TaskId.phase = 'red'
        $state.taskStates.$TaskId | Add-Member -NotePropertyName status -NotePropertyValue 'in-progress' -Force
    }
    
    'green' {
        # Record GREEN phase evidence
        $greenData = [ordered]@{
            timestamp = $timestamp
            testCommand = $TestCommand
            exitCode = $ExitCode
            output = $TestOutput
        }
        
        $evidence.green = $greenData
        $state.taskStates.$TaskId | Add-Member -NotePropertyName status -NotePropertyValue 'in-progress' -Force
        
        # Capture git diff
        try {
            $diffOutput = git diff HEAD --name-only 2>$null
            $diffPatch = git diff HEAD 2>$null
            
            if ($diffOutput) {
                $files = ($diffOutput -split "`n") | Where-Object { $_ -match '\S' }
                $evidence.diff = [ordered]@{
                    timestamp = $timestamp
                    files = @($files)
                    patch = ($diffPatch -join "`n")
                }
            } else {
                # Check staged changes
                $stagedOutput = git diff --cached --name-only 2>$null
                $stagedPatch = git diff --cached 2>$null
                
                if ($stagedOutput) {
                    $files = ($stagedOutput -split "`n") | Where-Object { $_ -match '\S' }
                    $evidence.diff = [ordered]@{
                        timestamp = $timestamp
                        files = @($files)
                        patch = ($stagedPatch -join "`n")
                        staged = $true
                    }
                }
            }
        } catch {
            # Git not available or not in repo
            $evidence.diff = [ordered]@{
                timestamp = $timestamp
                error = "Could not capture git diff: $($_.Exception.Message)"
            }
        }
        
        $state.taskStates.$TaskId.phase = 'green'
    }
}

# Save evidence
$evidence | ConvertTo-Json -Depth 10 | Set-Content -Path $evidenceFile -Encoding UTF8

# Save state
$state | ConvertTo-Json -Depth 10 | Set-Content -Path $stateFile -Encoding UTF8

# Output result
if ($Json) {
    [PSCustomObject]@{
        success = $true
        taskId = $TaskId
        phase = $Phase
        evidenceFile = $evidenceFile
        retry = $Retry.IsPresent
    } | ConvertTo-Json -Compress
} else {
    Write-Output "Recorded $Phase phase for $TaskId"
    Write-Output "Evidence: $evidenceFile"
}
