param(
    [string]$UnityPath,
    [ValidateSet('All', 'Core', 'EditMode', 'PlayMode')][string]$Suite = 'All',
    [string]$TestFilter
)
$ErrorActionPreference = 'Stop'
$timer = [Diagnostics.Stopwatch]::StartNew()
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet run --project Tests/CoreChecks/CoreChecks.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Core checks failed' }
    if ($Suite -eq 'Core') { return }
    if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) { throw 'Pass -UnityPath with the full path to Unity.exe. Core checks alone do not validate gameplay.' }
    New-Item -ItemType Directory -Force TestResults | Out-Null
    $modes = if ($Suite -eq 'All') { @('EditMode', 'PlayMode') } else { @($Suite) }
    foreach ($mode in $modes) {
        $result = Join-Path $root "TestResults/$mode.xml"
        if (Test-Path -LiteralPath $result) { Remove-Item -LiteralPath $result }
        $arguments = @('-batchmode','-nographics','-projectPath', ('"' + $root + '"'), '-runTests','-testPlatform',$mode,'-testResults',('"' + $result + '"'),'-logFile',('"' + (Join-Path $root "TestResults/$mode.log") + '"'))
        if ($TestFilter) {
            if ($TestFilter.Contains('"')) { throw 'TestFilter cannot contain double quotes' }
            $arguments += @('-testFilter', ('"' + $TestFilter + '"'))
        }
        $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
        # Wait for this editor, not a shared licensing child used by another session.
        $process.WaitForExit()
        if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $result)) { throw "$mode did not complete successfully. Read TestResults/$mode.log." }
        [xml]$report = Get-Content -LiteralPath $result
        if ($report.'test-run'.result -ne 'Passed' -or [int]$report.'test-run'.total -eq 0) { throw "$mode tests failed or no tests ran" }
    }
} finally {
    Pop-Location
    Write-Host ('Local validation elapsed: {0:n1}s (suite: {1}, filter: {2})' -f $timer.Elapsed.TotalSeconds, $Suite, $TestFilter)
}
