param(
    [Parameter(Mandatory)][long]$RunId,
    [string]$Repository = 'elimelech-eli/VideoGame',
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
# Reuse Git's credential manager without printing or persisting its token.
$env:GIT_TERMINAL_PROMPT = '0'
$credential = "protocol=https`nhost=github.com`n`n" | git credential fill
if ($LASTEXITCODE -ne 0) { throw 'GitHub credential lookup failed' }
$passwordLine = $credential | Where-Object { $_.StartsWith('password=') } | Select-Object -First 1
if (-not $passwordLine) { throw 'No GitHub credential available' }
$headers = @{ Authorization = 'Bearer ' + $passwordLine.Substring(9); Accept = 'application/vnd.github+json' }
$timer = [Diagnostics.Stopwatch]::StartNew()
$previous = ''
while ($true) {
    $run = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/actions/runs/$RunId" -Headers $headers
    $state = "$($run.status) $($run.conclusion)".Trim()
    if ($state -ne $previous) {
        Write-Host "Run $RunId : $state"
        $previous = $state
    }
    if ($run.status -eq 'completed') {
        $jobs = Invoke-RestMethod -Uri "$($run.jobs_url)?per_page=100" -Headers $headers
        $jobs.jobs | Select-Object name, conclusion, @{n='seconds';e={
            if ($_.started_at -and $_.completed_at) { ([datetime]$_.completed_at - [datetime]$_.started_at).TotalSeconds }
        }} | Format-Table -AutoSize
        Write-Host $run.html_url
        if ($run.conclusion -ne 'success') { throw "CI concluded: $($run.conclusion)" }
        return
    }
    if ($timer.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
        Write-Host "Still pending; resume with -RunId $RunId. $($run.html_url)"
        return
    }
    Start-Sleep -Seconds ([Math]::Min(30, [Math]::Max(1, [int]($TimeoutSeconds - $timer.Elapsed.TotalSeconds))))
}
