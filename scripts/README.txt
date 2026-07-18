ExamBuilder GR - GitHub first push fix

Reason for the error
--------------------
Windows PowerShell 5.1 read Greek text in the old .ps1 file with the wrong
encoding. The corrupted characters caused a ParserError before execution.

Important
---------
Run this script from the ROOT of the latest GitHub-ready project, where the
ExamBuilderGR.sln file is located.

Do not run it from an old v0.2.3 project folder unless that folder truly
contains the latest v0.7.2 RC3 source.

Steps
-----
1. Extract the latest:
   ExamBuilderGR_v0_7_2_RC3_GitHubReady.zip

2. Copy github-first-push-fixed.ps1 into that extracted root folder.

3. Open PowerShell in that folder.

4. Sign in if needed:
   gh auth login

5. Run:
   powershell -ExecutionPolicy Bypass -File .\github-first-push-fixed.ps1 -Visibility public

Optional parameters:
   -RepositoryName ExamBuilderGR
   -Tag v0.7.2-rc.3

The script is ASCII-only to avoid PowerShell encoding problems.
