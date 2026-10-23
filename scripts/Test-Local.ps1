param([string]$UnityPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet run --project Tests/CoreChecks/CoreChecks.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Core checks failed' }
    if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) { throw 'Pass -UnityPath with the full path to Unity.exe. Core checks alone do not validate gameplay.' }
    New-Item -ItemType Directory -Force TestResults | Out-Null
    foreach ($mode in @('EditMode', 'PlayMode')) {
        $result = Join-Path $root "TestResults/$mode.xml"
        if (Test-Path -LiteralPath $result) { Remove-Item -LiteralPath $result }
        $process = Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-nographics','-projectPath', ('"' + $root + '"'), '-runTests','-testPlatform',$mode,'-testResults',('"' + $result + '"'),'-logFile',('"' + (Join-Path $root "TestResults/$mode.log") + '"')) -PassThru -Wait -WindowStyle Hidden
        if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $result)) { throw "$mode did not complete successfully. Read TestResults/$mode.log." }
        [xml]$report = Get-Content -LiteralPath $result
        if ($report.'test-run'.result -ne 'Passed' -or [int]$report.'test-run'.total -eq 0) { throw "$mode tests failed or no tests ran" }
    }
} finally { Pop-Location }
