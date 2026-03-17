[CmdletBinding()]
param(
    [string]$ProjectPath = (Get-Location).Path,
    [string]$UnityPath,
    [string[]]$AssemblyNames,
    [int]$CompilationTimeoutMinutes = 10,
    [int]$TestTimeoutMinutes = 0,
    [int]$EditModeTimeoutMinutes = 30,
    [int]$PlayModeTimeoutMinutes = 30,
    [int]$AnalyzerTimeoutMinutes = 10,
    [int]$AnalyzerTestsTimeoutMinutes = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($TestTimeoutMinutes -gt 0) {
    $EditModeTimeoutMinutes = $TestTimeoutMinutes
    $PlayModeTimeoutMinutes = $TestTimeoutMinutes
}

$scriptDirectory = Split-Path -Parent $PSCommandPath
$checkScriptsAsmdefReferencesPath = Join-Path $scriptDirectory "check-scripts-asmdef-references.ps1"
$checkCompilationPath = Join-Path $scriptDirectory "check-unity-compilation.ps1"
$runEditModeTestsPath = Join-Path $scriptDirectory "run-editmode-tests.ps1"
$runPlayModeTestsPath = Join-Path $scriptDirectory "run-playmode-tests.ps1"
$checkAnalyzersPath = Join-Path $scriptDirectory "check-analyzers.ps1"

if (-not (Test-Path $checkScriptsAsmdefReferencesPath)) { throw "Missing script: $checkScriptsAsmdefReferencesPath" }
if (-not (Test-Path $checkCompilationPath)) { throw "Missing script: $checkCompilationPath" }
if (-not (Test-Path $runEditModeTestsPath)) { throw "Missing script: $runEditModeTestsPath" }
if (-not (Test-Path $runPlayModeTestsPath)) { throw "Missing script: $runPlayModeTestsPath" }
if (-not (Test-Path $checkAnalyzersPath)) { throw "Missing script: $checkAnalyzersPath" }

$asmdefAuditExitCode = 1
$asmdefAuditTotal = -1
$asmdefAuditIssues = @()
$compilationExitCode = 1
$editModeExitCode = 1
$playModeExitCode = 1
$analyzerTotal = -1
$analyzerBlockers = @()
$analyzerDiagnostics = @()

Write-Host "Change validation started..."
Write-Host ""
Write-Host "[1/5] Running scripts asmdef reference audit"

try {
    $asmdefAuditOutput = & $checkScriptsAsmdefReferencesPath -ProjectPath $ProjectPath | ForEach-Object { "$_" }
    $asmdefAuditExitCode = if (Test-Path variable:LASTEXITCODE) { [int]$LASTEXITCODE } else { 1 }
} catch {
    $asmdefAuditOutput = @(
        "TOTAL:-1",
        ("ISSUE:ScriptFailure|{0}|n/a|{1}" -f $checkScriptsAsmdefReferencesPath, $_.Exception.Message)
    )
    $asmdefAuditExitCode = 1
}

$asmdefAuditOutput |
    Where-Object { $_ -notlike "ISSUE:*" } |
    ForEach-Object { Write-Host $_ }

$asmdefTotalLine = $asmdefAuditOutput | Where-Object { $_ -match "^TOTAL:-?\d+$" } | Select-Object -First 1
if ($asmdefTotalLine -and $asmdefTotalLine -match "^TOTAL:(-?\d+)$") {
    $asmdefAuditTotal = [int]$matches[1]
} else {
    $asmdefAuditTotal = -1
}

$asmdefAuditIssues = @($asmdefAuditOutput | Where-Object { $_ -like "ISSUE:*" })

Write-Host ""
Write-Host "[2/5] Running compilation precheck"

$compilationArgs = @{
    ProjectPath = $ProjectPath
    TimeoutMinutes = $CompilationTimeoutMinutes
}
if ($UnityPath) { $compilationArgs.UnityPath = $UnityPath }

try {
    & $checkCompilationPath @compilationArgs
    $compilationExitCode = if (Test-Path variable:LASTEXITCODE) { [int]$LASTEXITCODE } else { 1 }
} catch {
    Write-Host ("Compilation precheck failed before completion: {0}" -f $_.Exception.Message)
    $compilationExitCode = 1
}

Write-Host ""
Write-Host "[3/5] Running EditMode tests"

$editModeArgs = @{
    ProjectPath = $ProjectPath
    TimeoutMinutes = $EditModeTimeoutMinutes
}
if ($UnityPath) { $editModeArgs.UnityPath = $UnityPath }
if ($AssemblyNames -and $AssemblyNames.Count -gt 0) { $editModeArgs.AssemblyNames = $AssemblyNames }

if ($compilationExitCode -eq 0) {
    try {
        & $runEditModeTestsPath @editModeArgs
        $editModeExitCode = if (Test-Path variable:LASTEXITCODE) { [int]$LASTEXITCODE } else { 1 }
    } catch {
        Write-Host ("Test runner failed before completion: {0}" -f $_.Exception.Message)
        $editModeExitCode = 1
    }
} else {
    Write-Host "Skipped EditMode tests because compilation precheck failed."
    $editModeExitCode = 1
}

Write-Host ""
Write-Host "[4/5] Running PlayMode tests"

$playModeArgs = @{
    ProjectPath = $ProjectPath
    TimeoutMinutes = $PlayModeTimeoutMinutes
}
if ($UnityPath) { $playModeArgs.UnityPath = $UnityPath }
if ($AssemblyNames -and $AssemblyNames.Count -gt 0) { $playModeArgs.AssemblyNames = $AssemblyNames }

if ($compilationExitCode -eq 0) {
    try {
        & $runPlayModeTestsPath @playModeArgs
        $playModeExitCode = if (Test-Path variable:LASTEXITCODE) { [int]$LASTEXITCODE } else { 1 }
    } catch {
        Write-Host ("Test runner failed before completion: {0}" -f $_.Exception.Message)
        $playModeExitCode = 1
    }
} else {
    Write-Host "Skipped PlayMode tests because compilation precheck failed."
    $playModeExitCode = 1
}

Write-Host ""
Write-Host "[5/5] Running analyzer check (includes analyzer unit tests)"

try {
    $analyzerOutput = & $checkAnalyzersPath -ProjectPath $ProjectPath -TimeoutMinutes $AnalyzerTimeoutMinutes -AnalyzerTestsTimeoutMinutes $AnalyzerTestsTimeoutMinutes | ForEach-Object { "$_" }
} catch {
    $analyzerOutput = @(
        "TOTAL:-1",
        ("BLOCKER:Analyzer script failed before completion: {0}" -f $_.Exception.Message)
    )
}

$analyzerOutput |
    Where-Object { $_ -notlike "DIAG:*" } |
    ForEach-Object { Write-Host $_ }

$totalLine = $analyzerOutput | Where-Object { $_ -match "^TOTAL:\d+$" } | Select-Object -First 1
if ($totalLine -and $totalLine -match "^TOTAL:(\d+)$") {
    $analyzerTotal = [int]$matches[1]
} else {
    $analyzerTotal = -1
}

$analyzerBlockers = @($analyzerOutput | Where-Object { $_ -like "BLOCKER:*" })
$analyzerDiagnostics = @(
    $analyzerOutput |
        Where-Object { $_ -like "DIAG:*" } |
        ForEach-Object { $_.Substring(5) }
)

$compilationPassed = ($compilationExitCode -eq 0)
$editModePassed = ($compilationPassed -and $editModeExitCode -eq 0)
$playModePassed = ($compilationPassed -and $playModeExitCode -eq 0)
$testsPassed = ($editModePassed -and $playModePassed)
$asmdefAuditPassed = ($asmdefAuditExitCode -eq 0 -and $asmdefAuditTotal -eq 0 -and $asmdefAuditIssues.Count -eq 0)
$validationGatePassed = ($asmdefAuditPassed -and $testsPassed)
$analyzersPassed = ($analyzerTotal -eq 0 -and $analyzerBlockers.Count -eq 0)

$finalExitCode = 0
if (-not $validationGatePassed -and -not $analyzersPassed) {
    $finalExitCode = 3
} elseif (-not $validationGatePassed) {
    $finalExitCode = 1
} elseif (-not $analyzersPassed) {
    $finalExitCode = 2
}

Write-Host ""
Write-Host "Change Validation Summary"
Write-Host "----------------------------"
if ($asmdefAuditTotal -ge 0) {
    Write-Host ("Scripts asmdef audit: {0} (TOTAL:{1})" -f ($(if ($asmdefAuditPassed) { "PASS" } else { "FAIL" }), $asmdefAuditTotal))
} else {
    Write-Host "Scripts asmdef audit: FAIL (could not parse TOTAL line)"
}
Write-Host ("Compilation: {0} (exit code {1})" -f ($(if ($compilationPassed) { "PASS" } else { "FAIL" }), $compilationExitCode))
if ($testsPassed) {
    Write-Host "Tests: PASS"
} else {
    if ($compilationPassed) {
        Write-Host ("Tests: FAIL (EditMode exit code {0}, PlayMode exit code {1})" -f $editModeExitCode, $playModeExitCode)
    } else {
        Write-Host "Tests: SKIPPED (compilation failed)"
    }
}

if ($analyzerTotal -ge 0) {
    Write-Host ("Analyzers: {0} (TOTAL:{1}, BLOCKERS:{2})" -f ($(if ($analyzersPassed) { "PASS" } else { "FAIL" }), $analyzerTotal, $analyzerBlockers.Count))
} else {
    Write-Host "Analyzers: FAIL (could not parse TOTAL line)"
}

if ($finalExitCode -eq 0) {
    Write-Host ""
    Write-Host "Quality gates are clean. Changes are ready to commit."
} else {
    Write-Host ""
    Write-Host "Quality gates failed. Fix issues, then rerun this script."
}

if (-not $analyzersPassed) {
    Write-Host ""
    Write-Host "AGENT_TASK_BEGIN"
    Write-Host "Task: Fix all analyzer diagnostics and blockers listed below."
    Write-Host "Validation: Re-run .agents/scripts/validate-changes.ps1 and ensure TOTAL:0 with no BLOCKER lines."
    Write-Host "AGENT_TASK_END"
    Write-Host ""
    Write-Host "AGENT_ANALYZER_DIAGNOSTICS_BEGIN"
    foreach ($line in $analyzerBlockers) { Write-Host $line }
    foreach ($line in $analyzerDiagnostics) { Write-Host $line }
    Write-Host "AGENT_ANALYZER_DIAGNOSTICS_END"
}

if (-not $asmdefAuditPassed) {
    Write-Host ""
    Write-Host "AGENT_ASMDEF_AUDIT_BEGIN"
    foreach ($line in $asmdefAuditIssues) { Write-Host $line }
    Write-Host "AGENT_ASMDEF_AUDIT_END"
}

exit $finalExitCode
