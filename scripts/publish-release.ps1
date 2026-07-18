[CmdletBinding()]
param(
    [string]$Runtime = "win-x64",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$projectPath = Join-Path $root "ExamBuilderGR\ExamBuilderGR.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Δεν βρέθηκε το project: $projectPath"
}

[xml]$project = Get-Content $projectPath
$version = $project.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Δεν βρέθηκε Version στο ExamBuilderGR.csproj."
}

$dist = Join-Path $root "dist"
$portable = Join-Path $dist "portable"
$single = Join-Path $dist "single-file"

Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item $portable -ItemType Directory -Force | Out-Null
New-Item $single -ItemType Directory -Force | Out-Null

if (-not $SkipBuild) {
    dotnet restore "$root\ExamBuilderGR.sln"
    if ($LASTEXITCODE -ne 0) { throw "Απέτυχε το dotnet restore." }
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
if ($LASTEXITCODE -ne 0) { throw "Απέτυχε το portable publish." }

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
if ($LASTEXITCODE -ne 0) { throw "Απέτυχε το single-file publish." }

foreach ($folder in @($portable, $single)) {
    Copy-Item "$root\README.md" "$folder\README.md" -Force
    Copy-Item "$root\CHANGELOG.md" "$folder\CHANGELOG.md" -Force
    Copy-Item "$root\docs\START_HERE.txt" "$folder\START_HERE.txt" -Force
}

$portableZip = Join-Path $dist "ExamBuilderGR_v${version}_${Runtime}_portable.zip"
$singleZip = Join-Path $dist "ExamBuilderGR_v${version}_${Runtime}_single-file.zip"
Compress-Archive -Path "$portable\*" -DestinationPath $portableZip -Force
Compress-Archive -Path "$single\*" -DestinationPath $singleZip -Force

$checksums = @()
foreach ($file in @($portableZip, $singleZip)) {
    $hash = Get-FileHash $file -Algorithm SHA256
    $checksums += "$($hash.Hash)  $([System.IO.Path]::GetFileName($file))"
}
$checksums | Set-Content (Join-Path $dist "SHA256SUMS.txt") -Encoding UTF8

Write-Host ""
Write-Host "Έτοιμα αρχεία release:" -ForegroundColor Green
Write-Host "  $portableZip"
Write-Host "  $singleZip"
Write-Host "  $(Join-Path $dist 'SHA256SUMS.txt')"
