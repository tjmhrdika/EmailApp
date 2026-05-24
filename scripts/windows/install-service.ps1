param(
    [string]$ServiceName = "CIP Station Alarm Notification",
    [string]$PublishPath = "$PSScriptRoot\..\..\publish",
    [string]$Urls = "http://0.0.0.0:5146"
)

$ErrorActionPreference = "Stop"
$ResolvedPublishPath = Resolve-Path $PublishPath
$ExePath = Join-Path $ResolvedPublishPath "EmailApp.exe"

if (-not (Test-Path $ExePath)) {
    throw "EmailApp.exe not found. Run scripts\windows\publish-service.ps1 first."
}

$ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($ExistingService) {
    if ($ExistingService.Status -ne "Stopped") {
        sc.exe stop "$ServiceName" | Out-Null
        Start-Sleep -Seconds 3
    }

    sc.exe delete "$ServiceName" | Out-Null
    Start-Sleep -Seconds 2
}

$BinaryPath = "`"$ExePath`" --urls $Urls"

sc.exe create "$ServiceName" binPath= "$BinaryPath" start= auto | Out-Null
sc.exe description "$ServiceName" "CIP Station alarm email notification service" | Out-Null
sc.exe start "$ServiceName" | Out-Null

Write-Host "Service installed and started: $ServiceName"
