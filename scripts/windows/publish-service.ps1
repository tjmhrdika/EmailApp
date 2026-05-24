param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputPath = "$PSScriptRoot\..\..\publish"
)

$ErrorActionPreference = "Stop"
$ProjectPath = Resolve-Path "$PSScriptRoot\..\..\EmailApp.csproj"
$ResolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

dotnet publish $ProjectPath -c $Configuration -r $Runtime --self-contained false -o $ResolvedOutputPath

Write-Host "Published to $ResolvedOutputPath"
