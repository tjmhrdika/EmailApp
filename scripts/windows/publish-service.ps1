param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputPath = "$PSScriptRoot\..\..\publish",
    [bool]$SelfContained = $true
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Resolve-Path "$PSScriptRoot\..\.."
$ProjectPath = Resolve-Path "$ProjectRoot\EmailApp.csproj"
$InstallerProjectPath = Resolve-Path "$ProjectRoot\tools\windows\EmailApp.Installer\EmailApp.Installer.csproj"
$UninstallerProjectPath = Resolve-Path "$ProjectRoot\tools\windows\EmailApp.Uninstaller\EmailApp.Uninstaller.csproj"
$ResolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$InstallerBuildPath = Join-Path $ProjectRoot "obj\installer-publish"
$UninstallerBuildPath = Join-Path $ProjectRoot "obj\uninstaller-publish"

function Invoke-DotNet {
    dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE"
    }
}

if (Test-Path $ResolvedOutputPath) {
    Remove-Item -LiteralPath $ResolvedOutputPath -Recurse -Force
}

if (Test-Path $InstallerBuildPath) {
    Remove-Item -LiteralPath $InstallerBuildPath -Recurse -Force
}

if (Test-Path $UninstallerBuildPath) {
    Remove-Item -LiteralPath $UninstallerBuildPath -Recurse -Force
}

New-Item -ItemType Directory -Path $ResolvedOutputPath -Force | Out-Null

Invoke-DotNet publish $ProjectPath -c $Configuration -r $Runtime --self-contained $SelfContained -o $ResolvedOutputPath

Invoke-DotNet publish $UninstallerProjectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $UninstallerBuildPath

Invoke-DotNet publish $InstallerProjectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $InstallerBuildPath

Copy-Item -LiteralPath (Join-Path $UninstallerBuildPath "EmailApp.Uninstaller.exe") -Destination (Join-Path $ResolvedOutputPath "uninstall.exe") -Force
Copy-Item -LiteralPath (Join-Path $InstallerBuildPath "EmailApp.Installer.exe") -Destination (Join-Path $ResolvedOutputPath "setup.exe") -Force

Remove-Item -LiteralPath $InstallerBuildPath -Recurse -Force
Remove-Item -LiteralPath $UninstallerBuildPath -Recurse -Force

Write-Host "Published to $ResolvedOutputPath"
Write-Host "Run setup.exe as administrator on the target Windows machine to install the service permanently."
