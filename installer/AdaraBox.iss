; installer/AdaraBox.iss
; Inno Setup Script for Adara Box MVP v1.0.0
; Per-user installation (no admin privileges required)

#define MyAppName "Adara Box"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Adara Box Team"
#define MyAppURL "https://github.com/supremeinori/Adara-box"
#define MyAppExeName "AdaraBox.App.exe"
#define SourceDir "..\artifacts\publish"
#define OutputDir "..\artifacts\installer"

[Setup]
; Basic Application Info
AppId={{9463F1B5-F7E6-4A73-98C3-21950A9F9C3E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation Scope: Per-User into %LOCALAPPDATA%\Programs\AdaraBox
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\AdaraBox
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Operating System Requirements: Windows 10 build 19044 or higher (NFR-CP-04)
MinVersion=10.0.19044

; Single Instance Checking
AppMutex=Global\AdaraBox.SingleInstance
CloseApplications=yes
CloseApplicationsFilter=*.exe

; Output Setup Binary
OutputDir={#OutputDir}
OutputBaseFilename=AdaraBox-Setup-{#MyAppVersion}
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Disallow directory deletion that might affect user data
DirExistsWarning=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
english.RunAtStartup=Start Adara Box when Windows starts
english.CreateDesktopIcon=Create a desktop shortcut
english.ConfirmDeleteUserData=Do you want to completely delete all Adara Box workspaces, history, and settings?%n%nSelect 'No' if you wish to keep your data for future use.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:RunAtStartup}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Publish artifacts directory
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Registry]
; Run on Windows startup option (synchronized with M05 SettingKeys.RunOnStartup)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AdaraBox"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Prompt user before deleting %LOCALAPPDATA%\AdaraBox data folder during uninstall
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\AdaraBox');
    if DirExists(DataDir) then
    begin
      // Ask user with default button = MB_DEFBUTTON2 (No) to prevent accidental data loss
      if SuppressibleMsgBox(CustomMessage('ConfirmDeleteUserData'), mbConfirmation, MB_YESNO, IDNO) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
