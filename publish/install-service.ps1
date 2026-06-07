param(
    [string]$ServiceName = "CIP Station Alarm Notification",
    [string]$PublishPath = "",
    [string]$Urls = "http://0.0.0.0:5146"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    if (Test-Path (Join-Path $PSScriptRoot "EmailApp.exe")) {
        $PublishPath = $PSScriptRoot
    }
    else {
        $PublishPath = "$PSScriptRoot\..\..\publish"
    }
}

$ResolvedPublishPath = Resolve-Path $PublishPath
$ExePath = Join-Path $ResolvedPublishPath "EmailApp.exe"

if (-not (Test-Path $ExePath)) {
    throw "EmailApp.exe not found in $ResolvedPublishPath."
}

$ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($ExistingService) {
    if ($ExistingService.Status -ne "Stopped") {
        Stop-Service -Name $ServiceName
        $ExistingService.WaitForStatus("Stopped", "00:00:30")
    }

    sc.exe delete "$ServiceName" | Out-Null
    Start-Sleep -Seconds 3
}

$ExistingProcesses = Get-CimInstance Win32_Process |
    Where-Object { $_.Name -eq "EmailApp.exe" -and $_.CommandLine -like "*$ExePath*" }

foreach ($Process in $ExistingProcesses) {
    Stop-Process -Id $Process.ProcessId -Force -ErrorAction SilentlyContinue
}

$Deadline = (Get-Date).AddSeconds(30)
do {
    $RunningProcess = Get-CimInstance Win32_Process |
        Where-Object { $_.Name -eq "EmailApp.exe" -and $_.CommandLine -like "*$ExePath*" } |
        Select-Object -First 1

    if (-not $RunningProcess) {
        break
    }

    Start-Sleep -Milliseconds 500
} while ((Get-Date) -lt $Deadline)

if ($RunningProcess) {
    throw "EmailApp.exe is still running from $ResolvedPublishPath. Stop it before installing the service."
}

$BinaryPath = "`"$ExePath`" --urls $Urls"

sc.exe create "$ServiceName" binPath= "$BinaryPath" start= auto | Out-Null
sc.exe description "$ServiceName" "CIP Station alarm email notification service" | Out-Null
sc.exe start "$ServiceName" | Out-Null

Write-Host "Service installed and started: $ServiceName"
