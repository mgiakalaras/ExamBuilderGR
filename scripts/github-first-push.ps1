[CmdletBinding()]
param(
    [string]$RepositoryName = "ExamBuilderGR",
    [ValidateSet("public", "private")]
    [string]$Visibility = "public",
    [string]$Tag = "v0.7.2-rc.3"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Δεν βρέθηκε το Git. Εγκατέστησε το Git for Windows."
}
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "Δεν βρέθηκε το GitHub CLI (gh)."
}

gh auth status
if ($LASTEXITCODE -ne 0) {
    throw "Δεν είσαι συνδεδεμένος στο GitHub CLI. Τρέξε πρώτα: gh auth login"
}

if (-not (Test-Path ".git")) {
    git init
    git branch -M main
}

git add .
$status = git status --porcelain
if ($status) {
    git commit -m "Initial release: ExamBuilder GR $Tag"
}

$remote = git remote get-url origin 2>$null
if (-not $remote) {
    gh repo create $RepositoryName --$Visibility --source . --remote origin --push
} else {
    git push -u origin main
}

$existingTag = git tag --list $Tag
if (-not $existingTag) {
    git tag -a $Tag -m "ExamBuilder GR $Tag"
}

git push origin $Tag

Write-Host ""
Write-Host "Το repository και το tag ανέβηκαν." -ForegroundColor Green
Write-Host "Το GitHub Actions workflow θα δημιουργήσει αυτόματα το Windows release."
