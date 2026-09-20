#define MyAppName "SharedFolderApp Client"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Vlad"
#define MyAppExeName "SharedFolderApp.Client.exe"

[Setup]
AppId={{B8D3A5E1-7C42-4F2A-91D6-3E8B5A7C2140}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\SharedFolderApp\Client
DefaultGroupName=SharedFolderApp Client

OutputDir=..\installers
OutputBaseFilename=SharedFolderApp-Client-Setup

Compression=lzma
SolidCompression=yes

ArchitecturesInstallIn64BitMode=x64

WizardStyle=modern

[Files]
Source: "..\publish\Client\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\ClientFolder"

[Icons]
Name: "{autodesktop}\SharedFolderApp Client"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\SharedFolderApp Client"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить SharedFolderApp Client"; Flags: nowait postinstall skipifsilent