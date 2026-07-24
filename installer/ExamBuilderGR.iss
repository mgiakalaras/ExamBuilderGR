; ExamBuilder GR installer

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#ifndef MyBinaryVersion
  #define MyBinaryVersion "1.0.0.0"
#endif

#ifndef SourceDir
  #define SourceDir "..\dist\portable"
#endif

#define MyAppName "ExamBuilder GR"
#define MyAppPublisher "Marios Giakalaras"
#define MyAppExeName "ExamBuilderGR.exe"
#define MyAppURL "https://github.com/mgiakalaras/ExamBuilderGR"

[Setup]
AppId={{8B5B1D70-7F2A-4E15-997B-1B87CB6A74D3}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\ExamBuilderGR
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\dist\installer
OutputBaseFilename=ExamBuilderGR_Setup_v{#MyAppVersion}
SetupIconFile=..\ExamBuilderGR\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyBinaryVersion}
VersionInfoProductVersion={#MyBinaryVersion}
VersionInfoTextVersion={#MyAppVersion}
VersionInfoProductTextVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} installer
VersionInfoProductName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
