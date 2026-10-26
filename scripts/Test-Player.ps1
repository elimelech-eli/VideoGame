param([string]$PlayerPath = 'Builds/Windows/VideoGame.exe')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$executable = [IO.Path]::GetFullPath((Join-Path $root $PlayerPath))
if (-not (Test-Path -LiteralPath $executable)) { throw 'Build the Windows player first' }
$log = Join-Path $root 'TestResults/Player.log'
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log }
$playerProcess = Start-Process -FilePath $executable -ArgumentList '-batchmode','-nographics','-logFile',('"' + $log + '"') -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Seconds 8
    $playerProcess.Refresh()
    if ($playerProcess.HasExited) { throw "Player exited unexpectedly: $($playerProcess.ExitCode)" }
    $output = Get-Content -LiteralPath $log -Raw
    if ($output -match 'Exception|Crash!!!|Shader error|Failed to load') { throw "Player log contains an error. Read $log" }
    if ($output -notmatch 'POC_READY') { throw "Player did not finish initializing. Read $log" }
    Write-Output 'PASS: Windows player initialized and remained running for 8 seconds without logged runtime errors.'
} finally {
    if (-not $playerProcess.HasExited) { Stop-Process -Id $playerProcess.Id }
}
