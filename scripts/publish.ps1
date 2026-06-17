param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$KeepOld
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $root "artifacts"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

function Clear-Artifacts {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return
    }

    Get-ChildItem -LiteralPath $Path -Force | ForEach-Object {
        Remove-ItemWithRetry -Path $_.FullName
    }
}

function Remove-ItemWithRetry {
    param([string]$Path)

    $lastError = $null
    for ($attempt = 1; $attempt -le 10; $attempt++) {
        try {
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            return
        }
        catch {
            $lastError = $_
            Start-Sleep -Milliseconds 500
        }
    }

    throw $lastError
}

function Stop-ArtifactQuickInputProcesses {
    param([string]$Path)

    $artifactRoot = [System.IO.Path]::GetFullPath($Path).TrimEnd('\')

    Get-Process -Name "QuickInput*" -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $processPath = [System.IO.Path]::GetFullPath($_.Path)
        }
        catch {
            return
        }

        if ($processPath.StartsWith($artifactRoot, [StringComparison]::OrdinalIgnoreCase)) {
            Write-Host "Stopping running artifact: $processPath"
            Stop-Process -Id $_.Id -Force
            Wait-Process -Id $_.Id -Timeout 5 -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
        }
    }
}

function Publish-QuickInput {
    param(
        [string]$Name,
        [bool]$SelfContained
    )

    $publishDir = Join-Path $artifactsDir "publish-$Name"
    $finalExe = Join-Path $artifactsDir "QuickInput-$Name-$timestamp.exe"

    if (Test-Path $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }

    $selfContainedValue = if ($SelfContained) { "true" } else { "false" }
    $compressionValue = if ($SelfContained) { "true" } else { "false" }

    $publishOutput = dotnet publish $root `
        -c $Configuration `
        -r $Runtime `
        --self-contained $selfContainedValue `
        -p:UseAppHost=true `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=$compressionValue `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir 2>&1

    $publishOutput | ForEach-Object {
        Write-Host $_
    }

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $Name."
    }

    $builtExe = Join-Path $publishDir "QuickInput.exe"
    Copy-Item -LiteralPath $builtExe -Destination $finalExe -Force
    Remove-Item -LiteralPath $publishDir -Recurse -Force

    return Get-Item -LiteralPath $finalExe
}

New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null

if (-not $KeepOld) {
    Stop-ArtifactQuickInputProcesses -Path $artifactsDir
    Write-Host "Cleaning old artifacts: $artifactsDir"
    Clear-Artifacts -Path $artifactsDir
}

$outputs = @(
    Publish-QuickInput -Name "self-contained-$Runtime" -SelfContained $true
    Publish-QuickInput -Name "framework-dependent-$Runtime" -SelfContained $false
)

Write-Host ""
Write-Host "Published builds:"
$outputs | ForEach-Object {
    $sizeMb = [Math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.FullName) ($sizeMb MB)"
}
