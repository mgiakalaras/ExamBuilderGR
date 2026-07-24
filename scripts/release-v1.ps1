param(
    [string]$Tag = "v1.0.0"
)

$ErrorActionPreference = "Stop"

function Run-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($Arguments -join ' ')"
    }
}

function Test-NativeCommand {
    param(
        [string]$Command,
        [string[]]$Arguments
    )

    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "SilentlyContinue"
        & $Command @Arguments *> $null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

if (-not (Test-Path "ExamBuilderGR.sln")) {
    throw "Run this script from the repository root, next to ExamBuilderGR.sln."
}

if (-not (Test-Path ".git")) {
    throw "This folder is not the existing Git repository. Copy the v1.0.0 files over the repository first."
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git was not found."
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI was not found."
}

& gh auth status
if ($LASTEXITCODE -ne 0) {
    throw "GitHub CLI is not authenticated. Run: gh auth login"
}

$status = @(git status --porcelain)
if ($status.Count -eq 0) {
    Write-Host "No file changes were found."
}
else {
    Run-Git add .
    Run-Git commit -m "Release ExamBuilder GR v1.0.0"
}

Run-Git push origin main

$tagExists = Test-NativeCommand "git" @("rev-parse", "--verify", "refs/tags/$Tag")
if ($tagExists) {
    throw "Tag $Tag already exists locally. Delete it or choose another tag before continuing."
}

Run-Git tag -a $Tag -m "ExamBuilder GR v1.0.0"
Run-Git push origin $Tag

Write-Host ""
Write-Host "Release tag pushed successfully: $Tag" -ForegroundColor Green
Write-Host "GitHub Actions will build the portable ZIP, single-file EXE, source ZIP and Windows installer."
Write-Host "Open the repository with: gh repo view --web"
