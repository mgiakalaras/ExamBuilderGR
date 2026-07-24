param(
    [string]$Tag = "v1.0.0",
    [string]$Branch = "main"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($Arguments -join ' ')"
    }
}

function Test-GitCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $oldPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "SilentlyContinue"
        & git @Arguments *> $null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }
}

Write-Host ""
Write-Host "ExamBuilder GR - v1.0.0 release" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

if (-not (Test-Path "ExamBuilderGR.sln")) {
    throw "Run this script from the repository root, next to ExamBuilderGR.sln."
}

if (-not (Test-Path ".git")) {
    throw "This folder is not a Git repository (.git was not found)."
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git was not found in PATH."
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) was not found in PATH."
}

& gh auth status
if ($LASTEXITCODE -ne 0) {
    throw "GitHub CLI is not authenticated. Run: gh auth login"
}

$originUrl = (& git remote get-url origin 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($originUrl)) {
    throw "The Git remote 'origin' is missing."
}

$currentBranch = (& git branch --show-current).Trim()
if ([string]::IsNullOrWhiteSpace($currentBranch)) {
    throw "Git is not currently on a branch."
}

if ($currentBranch -ne $Branch) {
    throw "Current branch is '$currentBranch'. Switch to '$Branch' and run the script again."
}

Write-Host "Repository: $originUrl"
Write-Host "Branch:     $currentBranch"
Write-Host "Tag:        $Tag"
Write-Host ""

$status = @(git status --porcelain)
if ($status.Count -gt 0) {
    Write-Host "Committing v1.0.0 changes..." -ForegroundColor Yellow
    Invoke-Git -Arguments @("add", ".")

    & git diff --cached --quiet
    if ($LASTEXITCODE -eq 1) {
        Invoke-Git -Arguments @("commit", "-m", "Release ExamBuilder GR v1.0.0")
    }
    elseif ($LASTEXITCODE -ne 0) {
        throw "Could not inspect staged Git changes."
    }
}
else {
    Write-Host "No uncommitted file changes were found. Continuing with push and tag." -ForegroundColor DarkGray
}

Write-Host "Pushing branch '$Branch'..." -ForegroundColor Yellow
Invoke-Git -Arguments @("push", "origin", $Branch)

$remoteTagExists = Test-GitCommand -Arguments @(
    "ls-remote",
    "--exit-code",
    "--tags",
    "origin",
    "refs/tags/$Tag"
)

if ($remoteTagExists) {
    Write-Host "Tag $Tag already exists on GitHub. Nothing else needs to be pushed." -ForegroundColor Green
}
else {
    $localTagExists = Test-GitCommand -Arguments @(
        "rev-parse",
        "--verify",
        "refs/tags/$Tag"
    )

    if (-not $localTagExists) {
        Write-Host "Creating tag $Tag..." -ForegroundColor Yellow
        Invoke-Git -Arguments @("tag", "-a", $Tag, "-m", "ExamBuilder GR v1.0.0")
    }
    else {
        Write-Host "Local tag $Tag already exists. It will now be pushed." -ForegroundColor Yellow
    }

    Write-Host "Pushing tag $Tag..." -ForegroundColor Yellow
    Invoke-Git -Arguments @("push", "origin", $Tag)
}

Write-Host ""
Write-Host "Release completed successfully." -ForegroundColor Green
Write-Host "GitHub Actions should now build the portable ZIP, single-file EXE, source ZIP and installer."
Write-Host ""
Write-Host "Check progress with:"
Write-Host "  gh run list --limit 5" -ForegroundColor Cyan
Write-Host ""
Write-Host "Open the repository with:"
Write-Host "  gh repo view --web" -ForegroundColor Cyan
