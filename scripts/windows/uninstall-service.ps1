param(
    [string]$ServiceName = "CIP Station Alarm Notification"
)

$ErrorActionPreference = "Stop"
$ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $ExistingService) {
    Write-Host "Service not found: $ServiceName"
    exit 0
}

if ($ExistingService.Status -ne "Stopped") {
    sc.exe stop "$ServiceName" | Out-Null
    Start-Sleep -Seconds 3
}

sc.exe delete "$ServiceName" | Out-Null

Write-Host "Service removed: $ServiceName"
