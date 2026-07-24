param(
    [ValidateSet("public", "private")]
    [string]$Visibility = "public",

    [string]$RepositoryName = "ExamBuilderGR",

    [string]$Tag = "v1.0.0"
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

$hasCommit = Test-NativeCommand "git" @("rev-parse", "--verify", "HEAD")
$hasChanges = -not (Test-NativeCommand "git" @("diff", "--cached", "--quiet"))

if ($hasChanges) {
    $message = if ($hasCommit) {
        "Prepare ExamBuilder GR $Tag"
    }
    else {
        "Initial release: ExamBuilder GR $Tag"
    }

    Write-Host "Creating commit..."
    Run-Git commit -m $message
}
else {
    Write-Host "No new files to commit."
}

$remoteNames = @(& git remote)
$remoteExists = $remoteNames -contains "origin"

if (-not $remoteExists) {
    $owner = (& gh api user --jq ".login").Trim()
    if ([string]::IsNullOrWhiteSpace($owner)) {
        throw "Could not determine the GitHub account name."
    }

    $repoFullName = "$owner/$RepositoryName"
    $repositoryExists = Test-NativeCommand "gh" @("repo", "view", $repoFullName)

    if ($repositoryExists) {
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
    $remoteUrl = (& git remote get-url origin).Trim()
    Write-Host "Remote origin already exists:"
    Write-Host $remoteUrl
}

Write-Host "Pushing main branch..."
Run-Git push -u origin main

$tagExists = Test-NativeCommand "git" @("rev-parse", "--verify", "refs/tags/$Tag")
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
