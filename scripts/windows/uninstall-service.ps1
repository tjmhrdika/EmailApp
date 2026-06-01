param(
    [string]$ServiceName = "CIP Station Alarm Notification"
)

$ErrorActionPreference = "Stop"
$ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
$ServiceInfo = Get-CimInstance Win32_Service -Filter "Name='$ServiceName'" -ErrorAction SilentlyContinue

if (-not $ExistingService) {
    Write-Host "Service not found: $ServiceName"
    exit 0
}

if ($ExistingService.Status -ne "Stopped") {
    Stop-Service -Name $ServiceName
    $ExistingService.WaitForStatus("Stopped", "00:00:30")
}

sc.exe delete "$ServiceName" | Out-Null
Start-Sleep -Seconds 3

if ($ServiceInfo -and $ServiceInfo.PathName -match '"?([^"]*EmailApp\.exe)"?') {
    $ExePath = $Matches[1]
    Get-CimInstance Win32_Process |
        Where-Object { $_.Name -eq "EmailApp.exe" -and $_.CommandLine -like "*$ExePath*" } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}

Write-Host "Service removed: $ServiceName"
