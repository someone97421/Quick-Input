param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "artifacts\publish"
$finalDir = Join-Path $root "artifacts"
$finalExe = Join-Path $finalDir "QuickInput.exe"

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $finalDir | Out-Null

dotnet publish $root `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

$builtExe = Join-Path $publishDir "QuickInput.exe"
Copy-Item -LiteralPath $builtExe -Destination $finalExe -Force
Write-Host "Published: $finalExe"
