#define MyAppName "SharedFolderApp Server"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Vlad"
#define MyAppExeName "SharedFolderApp.Server.exe"

[Setup]
AppId={{5A2F8C71-3D94-4B6E-A217-9F4C8D6B3052}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\SharedFolderApp\Server
DefaultGroupName=SharedFolderApp Server

OutputDir=..\installers
OutputBaseFilename=SharedFolderApp-Server-Setup

Compression=lzma
SolidCompression=yes

ArchitecturesInstallIn64BitMode=x64

WizardStyle=modern

[Files]
Source: "..\publish\Server\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\ServerFolder"

[Icons]
Name: "{autodesktop}\SharedFolderApp Server"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\SharedFolderApp Server"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить SharedFolderApp Server"; Flags: nowait postinstall skipifsilent