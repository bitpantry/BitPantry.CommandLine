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

.EXAMPLE
    ./record-task-phase.ps1 -TaskId T001 -Phase green -TestFilter "MyTestMethod" -Json
    # Auto-runs: dotnet test --filter MyTestMethod and captures results
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$TaskId,
    
    [Parameter(Mandatory)]
    [ValidateSet('started', 'red', 'green', 'verified')]
    [string]$Phase,
    
    [string]$TestFilter,  # New parameter - if provided, auto-runs the test
    [string]$TestCommand,
    [int]$ExitCode = -1,
    [string]$TestOutput,
    [string]$TestFile,
    [string]$TestMethod,
    [switch]$Retry,
    [switch]$PreCompleted,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

# Source common functions
. "$PSScriptRoot/common.ps1"

# Auto-run test if TestFilter is provided and we're recording red/green phase
if ($TestFilter -and ($Phase -eq 'red' -or $Phase -eq 'green')) {
    $TestCommand = "dotnet test --filter `"$TestFilter`""
    
    # Run the test and capture output
    $testResult = & dotnet test --filter $TestFilter 2>&1
    $ExitCode = $LASTEXITCODE
    
    # Extract summary from test output - join all lines for searching
    $outputLines = $testResult -join "`n"
    
    # Look for various dotnet test summary formats
    # Format 1: "Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1"
    # Format 2: "Test summary: total: 1, failed: 0, succeeded: 1"
    # Format 3: "Failed!  - Failed:     1, Passed:     0"
    if ($outputLines -match '((?:Passed!|Failed!)\s*-\s*Failed:\s*\d+,\s*Passed:\s*\d+[^\r\n]*)') {
        $TestOutput = $matches[1].Trim()
    } elseif ($outputLines -match '(Test summary:[^\r\n]+)') {
        $TestOutput = $matches[1].Trim()
    } elseif ($outputLines -match '(total:\s*\d+,\s*failed:\s*\d+,\s*succeeded:\s*\d+[^\r\n]*)') {
        $TestOutput = $matches[1].Trim()
    } else {
        # Take last few meaningful lines
        $meaningfulLines = ($testResult | Where-Object { $_ -match '\S' } | Select-Object -Last 5) -join "; "
        if ($meaningfulLines.Length -gt 500) {
            $TestOutput = $meaningfulLines.Substring(0, 500)
        } else {
            $TestOutput = $meaningfulLines
        }
    }
}

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
        
        # If test passed during RED and PreCompleted is set, mark it
        if ($PreCompleted -or ($ExitCode -eq 0)) {
            $redData.preCompleted = $true
            $redData.note = "Test passed immediately - behavior was already implemented"
        }
        
        $evidence | Add-Member -NotePropertyName red -NotePropertyValue $redData -Force
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
        
        $evidence | Add-Member -NotePropertyName green -NotePropertyValue $greenData -Force
        $state.taskStates.$TaskId | Add-Member -NotePropertyName status -NotePropertyValue 'in-progress' -Force
        
        # Capture git diff
        try {
            $diffOutput = git diff HEAD --name-only 2>$null
            $diffPatch = git diff HEAD 2>$null
            
            if ($diffOutput) {
                $files = ($diffOutput -split "`n") | Where-Object { $_ -match '\S' }
                $diffData = [ordered]@{
                    timestamp = $timestamp
                    files = @($files)
                    patch = ($diffPatch -join "`n")
                }
                $evidence | Add-Member -NotePropertyName diff -NotePropertyValue $diffData -Force
            } else {
                # Check staged changes
                $stagedOutput = git diff --cached --name-only 2>$null
                $stagedPatch = git diff --cached 2>$null
                
                if ($stagedOutput) {
                    $files = ($stagedOutput -split "`n") | Where-Object { $_ -match '\S' }
                    $diffData = [ordered]@{
                        timestamp = $timestamp
                        files = @($files)
                        patch = ($stagedPatch -join "`n")
                        staged = $true
                    }
                    $evidence | Add-Member -NotePropertyName diff -NotePropertyValue $diffData -Force
                }
            }
        } catch {
            # Git not available or not in repo
            $diffData = [ordered]@{
                timestamp = $timestamp
                error = "Could not capture git diff: $($_.Exception.Message)"
            }
            $evidence | Add-Member -NotePropertyName diff -NotePropertyValue $diffData -Force
        }
        
        $state.taskStates.$TaskId.phase = 'green'
    }
    
    'verified' {
        # Mark task as verified (completed)
        $state.taskStates.$TaskId.phase = 'verified'
        $state.taskStates.$TaskId | Add-Member -NotePropertyName status -NotePropertyValue 'verified' -Force
        $state.taskStates.$TaskId | Add-Member -NotePropertyName verifiedAt -NotePropertyValue $timestamp -Force
        
        if ($PreCompleted) {
            $state.taskStates.$TaskId | Add-Member -NotePropertyName preCompleted -NotePropertyValue $true -Force
        }
        
        # Clear current task if this was the current task
        if ($state.currentTask -eq $TaskId) {
            $state.currentTask = $null
        }
        
        # Add verified timestamp to evidence if it exists
        if ($evidence) {
            $evidence | Add-Member -NotePropertyName verifiedAt -NotePropertyValue $timestamp -Force
            if ($PreCompleted) {
                $evidence | Add-Member -NotePropertyName preCompleted -NotePropertyValue $true -Force
            }
        }
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
