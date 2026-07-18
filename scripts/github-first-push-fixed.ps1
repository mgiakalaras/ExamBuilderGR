param(
    [ValidateSet("public", "private")]
    [string]$Visibility = "public",

    [string]$RepositoryName = "ExamBuilderGR",

    [string]$Tag = "v0.7.2-rc.3"
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found. Install it and reopen PowerShell."
    }
}

function Run-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($Arguments -join ' ')"
    }
}

Require-Command "git"
Require-Command "gh"

$solution = Get-ChildItem -Path . -Filter "*.sln" -File | Select-Object -First 1
if (-not $solution) {
    throw "No .sln file was found in the current folder. Run this script from the project root."
}

Write-Host ""
Write-Host "Project root: $((Get-Location).Path)"
Write-Host "Solution:     $($solution.Name)"
Write-Host "Repository:   $RepositoryName"
Write-Host "Visibility:   $Visibility"
Write-Host "Tag:          $Tag"
Write-Host ""

& gh auth status
if ($LASTEXITCODE -ne 0) {
    throw "GitHub CLI is not authenticated. Run: gh auth login"
}

if (-not (Test-Path ".git")) {
    Write-Host "Initializing Git repository..."
    Run-Git init
}

$currentBranch = (& git branch --show-current).Trim()
if ([string]::IsNullOrWhiteSpace($currentBranch)) {
    Run-Git checkout -b main
}
elseif ($currentBranch -ne "main") {
    Run-Git branch -M main
}

$userName = (& git config user.name).Trim()
if ([string]::IsNullOrWhiteSpace($userName)) {
    Run-Git config user.name "Marios Giakalaras"
}

$userEmail = (& git config user.email).Trim()
if ([string]::IsNullOrWhiteSpace($userEmail)) {
    $ghLogin = (& gh api user --jq ".login").Trim()
    if ([string]::IsNullOrWhiteSpace($ghLogin)) {
        throw "Could not determine the GitHub login."
    }
    Run-Git config user.email "$ghLogin@users.noreply.github.com"
}

Write-Host "Adding project files..."
Run-Git add .

$hasCommit = $true
& git rev-parse --verify HEAD *> $null
if ($LASTEXITCODE -ne 0) {
    $hasCommit = $false
}

& git diff --cached --quiet
$hasChanges = ($LASTEXITCODE -ne 0)

if ($hasChanges) {
    $message = if ($hasCommit) {
        "Prepare ExamBuilder GR $Tag"
    } else {
        "Initial release: ExamBuilder GR $Tag"
    }

    Write-Host "Creating commit..."
    Run-Git commit -m $message
}
else {
    Write-Host "No new files to commit."
}

$remoteUrl = (& git remote get-url origin 2>$null)
$remoteExists = ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($remoteUrl))

if (-not $remoteExists) {
    $owner = (& gh api user --jq ".login").Trim()
    if ([string]::IsNullOrWhiteSpace($owner)) {
        throw "Could not determine the GitHub account name."
    }

    $repoFullName = "$owner/$RepositoryName"

    & gh repo view $repoFullName *> $null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "GitHub repository already exists. Adding it as origin..."
        Run-Git remote add origin "https://github.com/$repoFullName.git"
    }
    else {
        Write-Host "Creating GitHub repository..."
        & gh repo create $RepositoryName --$Visibility --source . --remote origin
        if ($LASTEXITCODE -ne 0) {
            throw "Could not create the GitHub repository."
        }
    }
}
else {
    Write-Host "Remote origin already exists:"
    Write-Host $remoteUrl
}

Write-Host "Pushing main branch..."
Run-Git push -u origin main

& git rev-parse $Tag *> $null
$tagExists = ($LASTEXITCODE -eq 0)

if (-not $tagExists) {
    Write-Host "Creating tag $Tag..."
    Run-Git tag -a $Tag -m "ExamBuilder GR $Tag"
}
else {
    Write-Host "Tag $Tag already exists locally."
}

Write-Host "Pushing tag $Tag..."
& git push origin $Tag
if ($LASTEXITCODE -ne 0) {
    Write-Warning "The tag may already exist remotely. Check the GitHub repository."
}

Write-Host ""
Write-Host "Completed successfully."
Write-Host "Open the repository with:"
Write-Host "  gh repo view --web"
Write-Host ""
