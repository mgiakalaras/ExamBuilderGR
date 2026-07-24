# Scripts

## Άνοιγμα και build στο Visual Studio

```text
build_and_run.bat
```

## Portable και single-file release

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-release.ps1
```

## Installer Inno Setup

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

## Installer και ανέβασμα στο υπάρχον GitHub Release

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1 -UploadToGitHub
```

## Πρώτο GitHub push για νέο repository

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\github-first-push.ps1 -Visibility public
```

Τα PowerShell scripts είναι ASCII-only για συμβατότητα με Windows PowerShell 5.1.
