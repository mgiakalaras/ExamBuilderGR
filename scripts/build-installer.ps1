[CmdletBinding()]
param(
    [string]$Runtime = "win-x64",
    [string]$Tag = "v1.0.0",
    [switch]$UploadToGitHub,
    [string]$InnoCompiler = ""
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$project = Join-Path $root "ExamBuilderGR\ExamBuilderGR.csproj"
$solution = Join-Path $root "ExamBuilderGR.sln"
$installerScript = Join-Path $root "installer\ExamBuilderGR.iss"
$portable = Join-Path $root "dist\portable"
$installerOutput = Join-Path $root "dist\installer"

foreach ($required in @($project, $solution, $installerScript)) {
    if (-not (Test-Path $required)) {
        throw "Required file not found: $required"
    }
}

[xml]$projectXml = Get-Content $project
$version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "1.0.0"
}

# Convert SemVer/prerelease strings into a Windows-compatible numeric version.
# Example: 1.0.0 -> 1.0.0.0
$versionNumbers = [regex]::Matches($version, '\d+') |
    ForEach-Object { [int]$_.Value }

while ($versionNumbers.Count -lt 4) {
    $versionNumbers += 0
}

$binaryVersion = ($versionNumbers | Select-Object -First 4) -join '.'

function Find-InnoCompiler {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (Test-Path $ExplicitPath) {
            return (Resolve-Path $ExplicitPath).Path
        }

        throw "The Inno Setup compiler was not found at: $ExplicitPath"
    }

    $fromPath = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Source
    }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 7\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )

    if (${env:ProgramFiles(x86)}) {
        $candidates += (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 7\ISCC.exe")
        $candidates += (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe")
    }

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    $registryPaths = @(
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )

    foreach ($registryPath in $registryPaths) {
        $entries = Get-ItemProperty $registryPath -ErrorAction SilentlyContinue |
            Where-Object {
                $_.DisplayName -like "Inno Setup*" -and
                -not [string]::IsNullOrWhiteSpace($_.InstallLocation)
            }

        foreach ($entry in $entries) {
            $candidate = Join-Path $entry.InstallLocation "ISCC.exe"
            if (Test-Path $candidate) {
                return (Resolve-Path $candidate).Path
            }
        }
    }

    return $null
}

$iscc = Find-InnoCompiler -ExplicitPath $InnoCompiler

if (-not $iscc) {
    throw "Inno Setup compiler ISCC.exe could not be located."
}

Write-Host "Application version: $version"
Write-Host "Binary version:      $binaryVersion"
Write-Host "Inno compiler:       $iscc"
Write-Host ""

Write-Host "Publishing portable self-contained build..."

Remove-Item $portable -Recurse -Force -ErrorAction SilentlyContinue
New-Item $portable -ItemType Directory -Force | Out-Null

dotnet restore $solution
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed."
}

dotnet publish $project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $portable

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

foreach ($file in @("README.md", "CHANGELOG.md")) {
    $source = Join-Path $root $file
    if (Test-Path $source) {
        Copy-Item $source (Join-Path $portable $file) -Force
    }
}

Remove-Item $installerOutput -Recurse -Force -ErrorAction SilentlyContinue
New-Item $installerOutput -ItemType Directory -Force | Out-Null

Write-Host "Compiling installer with Inno Setup..."

& $iscc `
    "/DMyAppVersion=$version" `
    "/DMyBinaryVersion=$binaryVersion" `
    "/DSourceDir=$portable" `
    $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed."
}

$setup = Get-ChildItem $installerOutput -Filter "ExamBuilderGR_Setup_v*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $setup) {
    throw "Installer EXE was not created."
}

$hash = Get-FileHash $setup.FullName -Algorithm SHA256
$checksumFile = Join-Path $installerOutput "$($setup.BaseName)_SHA256.txt"
"$($hash.Hash)  $($setup.Name)" | Set-Content $checksumFile -Encoding ASCII

Write-Host ""
Write-Host "Installer created successfully:" -ForegroundColor Green
Write-Host "  $($setup.FullName)"
Write-Host "  $checksumFile"

if ($UploadToGitHub) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI was not found."
    }

    Write-Host "Uploading installer to GitHub Release $Tag..."

    & gh release upload $Tag $setup.FullName $checksumFile --clobber

    if ($LASTEXITCODE -ne 0) {
        throw "GitHub Release upload failed."
    }

    Write-Host "Installer uploaded to GitHub Release $Tag." -ForegroundColor Green
}
