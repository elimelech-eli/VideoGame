param([string]$CaptureDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$player=Join-Path $root 'Builds/Windows/VideoGame.exe'
if(!(Test-Path -LiteralPath $player)){throw 'Build the Windows player from this checkout first.'}
$prior=[Environment]::GetEnvironmentVariable('POC_TRANSIT_MISSION','Process')
try {
    $env:POC_TRANSIT_MISSION=if($CaptureDirectory){[IO.Path]::GetFullPath($CaptureDirectory)}else{'play'}
    $arguments=@('-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-logFile',('"'+(Join-Path $root 'TestResults/TransitMissionPlayer.log')+'"'))
    Start-Process -FilePath $player -ArgumentList $arguments | Out-Null
} finally {
    [Environment]::SetEnvironmentVariable('POC_TRANSIT_MISSION',$prior,'Process')
}
Write-Host 'Transit mission opened in an isolated session. Normal combat and extraction are active. No existing checkpoint/preferences are loaded or saved.'
