# Δημιουργία installer για το ExamBuilder GR

Το project περιλαμβάνει έτοιμο Inno Setup script και PowerShell build script.

## 1. Εγκατάσταση Inno Setup 7

```powershell
winget install --id JRSoftware.InnoSetup.7 -e -s winget -i
```

Το script εντοπίζει αυτόματα το `ISCC.exe` είτε έχει εγκατασταθεί στο `Program Files` είτε στο `%LOCALAPPDATA%`.

## 2. Δημιουργία installer

Από τον κεντρικό φάκελο, δίπλα στο `ExamBuilderGR.sln`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

Δημιουργούνται:

```text
dist\installer\ExamBuilderGR_Setup_v0.7.3-rc.4.exe
dist\installer\ExamBuilderGR_Setup_v0.7.3-rc.4_SHA256.txt
```

Η εμφανιζόμενη έκδοση είναι `0.7.3-rc.4`, ενώ η αριθμητική έκδοση Windows είναι `0.7.3.4`.

Ο οδηγός εγκατάστασης είναι στα αγγλικά. Η εφαρμογή παραμένει κανονικά στα ελληνικά.

## 3. Δημιουργία και ανέβασμα στο υπάρχον GitHub Release

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1 -UploadToGitHub
```

Το script ανεβάζει το installer και το SHA-256 checksum στο tag `v0.7.3-rc.4`.

## 4. Εγκατάσταση

Η εφαρμογή εγκαθίσταται ανά χρήστη εδώ:

```text
%LOCALAPPDATA%\Programs\ExamBuilderGR
```

Δεν απαιτούνται δικαιώματα διαχειριστή. Η απεγκατάσταση δεν διαγράφει τα διαγωνίσματα, τα πρότυπα, τη βιβλιοθήκη ερωτήσεων ή τις ρυθμίσεις του χρήστη.
