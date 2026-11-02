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
    $startup = [Diagnostics.Stopwatch]::StartNew()
    $readyAt = $null
    while ($true) {
        $playerProcess.Refresh()
        if ($playerProcess.HasExited) { throw "Player exited unexpectedly: $($playerProcess.ExitCode)" }
        $output = if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Raw } else { '' }
        if ($output -match 'Exception|Crash!!!|Shader error|Failed to load') { throw "Player log contains an error. Read $log" }
        if ($null -eq $readyAt -and $output -match 'POC_READY') { $readyAt = $startup.Elapsed.TotalSeconds }
        if ($null -ne $readyAt -and $startup.Elapsed.TotalSeconds - $readyAt -ge 8) { break }
        if ($null -eq $readyAt -and $startup.Elapsed.TotalSeconds -ge 60) { throw "Player did not initialize within 60 seconds. Read $log" }
        Start-Sleep -Milliseconds 250
    }
    Write-Output ('PASS: Windows player initialized in {0:n2}s and remained running for 8 further seconds without logged runtime errors.' -f $readyAt)
} finally {
    if (-not $playerProcess.HasExited) { Stop-Process -Id $playerProcess.Id }
}
