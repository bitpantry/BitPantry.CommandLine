#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validate task evidence for completeness and correctness.

.DESCRIPTION
    Checks that the evidence file for a task contains all required
    sections with valid data, and that the sequence is correct.

.PARAMETER TaskId
    The task ID to validate (e.g., T001)

.PARAMETER Json
    Output results as JSON

.EXAMPLE
    ./check-task-evidence.ps1 -TaskId T001 -Json
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$TaskId,
    
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

# Source common functions
. "$PSScriptRoot/common.ps1"

# Get feature paths
$paths = Get-FeaturePathsEnv
$evidenceDir = Join-Path $paths.FEATURE_DIR 'evidence'
$evidenceFile = Join-Path $evidenceDir "$TaskId.json"

# Validation results
$checks = @()
$allPassed = $true

function Add-Check {
    param([string]$Name, [bool]$Passed, [string]$Message, [string]$Code)
    
    $script:checks += [PSCustomObject]@{
        check = $Name
        passed = $Passed
        message = $Message
        code = $Code
    }
    
    if (-not $Passed) {
        $script:allPassed = $false
    }
}

# Check 1: Evidence file exists
if (-not (Test-Path $evidenceFile -PathType Leaf)) {
    Add-Check -Name "Evidence file exists" -Passed $false -Message "Evidence file not found: $evidenceFile" -Code "MISSING_EVIDENCE"
    
    if ($Json) {
        [PSCustomObject]@{
            valid = $false
            taskId = $TaskId
            failureCode = "MISSING_EVIDENCE"
            failureReason = "Evidence file not found"
            checks = $checks
        } | ConvertTo-Json -Depth 5 -Compress
    } else {
        Write-Output "FAILED: Evidence file not found for $TaskId"
    }
    exit 1
}

Add-Check -Name "Evidence file exists" -Passed $true -Message "Found $evidenceFile" -Code $null

# Load evidence
$evidence = Get-Content $evidenceFile -Raw | ConvertFrom-Json

# Check 2: RED section exists
if (-not $evidence.red) {
    Add-Check -Name "RED section exists" -Passed $false -Message "No RED phase recorded" -Code "MISSING_RED"
} else {
    Add-Check -Name "RED section exists" -Passed $true -Message "RED phase found" -Code $null
    
    # Check 3: RED shows failure (exitCode != 0) - unless preCompleted
    if ($evidence.red.exitCode -eq 0) {
        if ($evidence.preCompleted -or $evidence.red.preCompleted) {
            Add-Check -Name "RED shows failure" -Passed $true -Message "Pre-completed: test passed immediately (behavior already implemented)" -Code $null
        } else {
            Add-Check -Name "RED shows failure" -Passed $false -Message "Test passed during RED phase (exit code 0) - invalid test. Use -PreCompleted if behavior was already implemented." -Code "RED_PASSED"
        }
    } else {
        Add-Check -Name "RED shows failure" -Passed $true -Message "Test failed as expected (exit code $($evidence.red.exitCode))" -Code $null
    }
}

# Check 4: GREEN section exists
if (-not $evidence.green) {
    Add-Check -Name "GREEN section exists" -Passed $false -Message "No GREEN phase recorded" -Code "MISSING_GREEN"
} else {
    Add-Check -Name "GREEN section exists" -Passed $true -Message "GREEN phase found" -Code $null
    
    # Check 5: GREEN shows success (exitCode == 0)
    if ($evidence.green.exitCode -ne 0) {
        Add-Check -Name "GREEN shows success" -Passed $false -Message "Test still failing (exit code $($evidence.green.exitCode))" -Code "GREEN_FAILED"
    } else {
        Add-Check -Name "GREEN shows success" -Passed $true -Message "Test passed (exit code 0)" -Code $null
    }
}

# Check 6: DIFF section exists
if (-not $evidence.diff) {
    Add-Check -Name "DIFF section exists" -Passed $false -Message "No implementation diff recorded" -Code "MISSING_DIFF"
} else {
    Add-Check -Name "DIFF section exists" -Passed $true -Message "DIFF found" -Code $null
    
    # Check 7: Files changed
    if (-not $evidence.diff.files -or $evidence.diff.files.Count -eq 0) {
        # Check if there's an error instead
        if ($evidence.diff.error) {
            Add-Check -Name "Files changed" -Passed $false -Message "Diff capture error: $($evidence.diff.error)" -Code "DIFF_ERROR"
        } else {
            Add-Check -Name "Files changed" -Passed $false -Message "No files changed during implementation" -Code "NO_CHANGES"
        }
    } else {
        Add-Check -Name "Files changed" -Passed $true -Message "$($evidence.diff.files.Count) file(s) changed" -Code $null
    }
}

# Check 8: Valid sequence (GREEN timestamp > RED timestamp)
if ($evidence.red -and $evidence.green -and $evidence.red.timestamp -and $evidence.green.timestamp) {
    $redTime = [DateTime]::Parse($evidence.red.timestamp)
    $greenTime = [DateTime]::Parse($evidence.green.timestamp)
    
    if ($greenTime -lt $redTime) {
        Add-Check -Name "Valid sequence" -Passed $false -Message "GREEN recorded before RED (invalid sequence)" -Code "INVALID_SEQUENCE"
    } else {
        Add-Check -Name "Valid sequence" -Passed $true -Message "RED → GREEN sequence valid" -Code $null
    }
}

# Determine overall failure code (first failure)
$failureCode = $null
$failureReason = $null
foreach ($check in $checks) {
    if (-not $check.passed -and $check.code) {
        $failureCode = $check.code
        $failureReason = $check.message
        break
    }
}

# Output results
if ($Json) {
    [PSCustomObject]@{
        valid = $allPassed
        taskId = $TaskId
        failureCode = $failureCode
        failureReason = $failureReason
        checks = $checks
        evidence = [PSCustomObject]@{
            hasRed = $null -ne $evidence.red
            hasGreen = $null -ne $evidence.green
            hasDiff = $null -ne $evidence.diff
            testCase = $evidence.testCase
        }
    } | ConvertTo-Json -Depth 5 -Compress
} else {
    if ($allPassed) {
        Write-Output "✓ Evidence valid for $TaskId"
    } else {
        Write-Output "✗ Evidence INVALID for $TaskId"
        Write-Output "Failure: $failureCode - $failureReason"
    }
    Write-Output ""
    foreach ($check in $checks) {
        $symbol = if ($check.passed) { "✓" } else { "✗" }
        Write-Output "  $symbol $($check.check): $($check.message)"
    }
}

if (-not $allPassed) {
    exit 1
}
