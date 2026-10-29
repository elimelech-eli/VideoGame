param([string]$CaptureDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$player=Join-Path $root 'Builds/Windows/VideoGame.exe'
if(!(Test-Path -LiteralPath $player)){throw 'Build the Windows player from this lab-review checkout first.'}
$prior=[Environment]::GetEnvironmentVariable('POC_LAB_REVIEW','Process')
try {
    $env:POC_LAB_REVIEW=if($CaptureDirectory){[IO.Path]::GetFullPath($CaptureDirectory)}else{'play'}
    $arguments=@('-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-logFile',('"'+(Join-Path $root 'TestResults/LabReviewPlayer.log')+'"'))
    Start-Process -FilePath $player -ArgumentList $arguments | Out-Null
} finally {
    [Environment]::SetEnvironmentVariable('POC_LAB_REVIEW',$prior,'Process')
}
Write-Host 'Lab review opened. WASD/right-click/controller movement. Alt+F4 closes the isolated review.'
