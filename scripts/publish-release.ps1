[CmdletBinding()]
param(
    [string]$Runtime = "win-x64",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$solutionPath = Join-Path $root "ExamBuilderGR.sln"
$projectPath = Join-Path $root "ExamBuilderGR\ExamBuilderGR.csproj"

foreach ($required in @($solutionPath, $projectPath)) {
    if (-not (Test-Path $required)) {
        throw "Required file not found: $required"
    }
}

[xml]$project = Get-Content $projectPath
$version = $project.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Version was not found in ExamBuilderGR.csproj."
}

$dist = Join-Path $root "dist"
$portable = Join-Path $dist "portable"
$single = Join-Path $dist "single-file"

Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item $portable -ItemType Directory -Force | Out-Null
New-Item $single -ItemType Directory -Force | Out-Null

if (-not $SkipRestore) {
    dotnet restore $solutionPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed."
    }
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $portable

if ($LASTEXITCODE -ne 0) {
    throw "Portable publish failed."
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $single

if ($LASTEXITCODE -ne 0) {
    throw "Single-file publish failed."
}

foreach ($folder in @($portable, $single)) {
    foreach ($file in @("README.md", "CHANGELOG.md")) {
        $source = Join-Path $root $file
        if (Test-Path $source) {
            Copy-Item $source (Join-Path $folder $file) -Force
        }
    }

    $startHere = Join-Path $root "docs\START_HERE.txt"
    if (Test-Path $startHere) {
        Copy-Item $startHere (Join-Path $folder "START_HERE.txt") -Force
    }
}

$portableZip = Join-Path $dist "ExamBuilderGR_v${version}_${Runtime}_portable.zip"
$singleZip = Join-Path $dist "ExamBuilderGR_v${version}_${Runtime}_single-file.zip"

Compress-Archive -Path "$portable\*" -DestinationPath $portableZip -Force
Compress-Archive -Path "$single\*" -DestinationPath $singleZip -Force

$checksums = foreach ($file in @($portableZip, $singleZip)) {
    $hash = Get-FileHash $file -Algorithm SHA256
    "$($hash.Hash)  $([System.IO.Path]::GetFileName($file))"
}

$checksumPath = Join-Path $dist "SHA256SUMS.txt"
$checksums | Set-Content $checksumPath -Encoding ASCII

Write-Host ""
Write-Host "Release files created successfully:" -ForegroundColor Green
Write-Host "  $portableZip"
Write-Host "  $singleZip"
Write-Host "  $checksumPath"
