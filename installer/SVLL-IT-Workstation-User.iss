; =========================================================================
; Shree Vasu Logistics Limited - IT Workstation USER EDITION Setup Script
; Developed by: Kunal Turkar
; =========================================================================

#define MyAppName "SVLL IT Support Workstation"
#define MyAppVersion "8.0"
#define MyAppPublisher "Shree Vasu Logistics Limited"
#define MyAppURL "https://svll.in"
#define MyAppExeName "SVLL-IT-Workstation-User.exe"

[Setup]
AppId={{8B31E97C-3944-4DE5-B002-32988DC039F2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\ShreeVasuLogistics\ITWorkstation
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\release
OutputBaseFilename=SVLL-IT-Workstation-User-v8.0-Setup
SetupIconFile=app_logo.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=classic
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish-user\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "app_logo.ico"; DestDir: "{app}"; DestName: "app_logo.ico"; Flags: ignoreversion
Source: "..\Complain Log sheet IT - New Complaint.csv"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_logo.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_logo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\MeshAgent.exe"; Parameters: "-install"; Flags: runhidden; Check: FileExists(ExpandConstant('{app}\MeshAgent.exe'))
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall

[Code]
#ifdef UNICODE
  #define AW "W"
#else
  #define AW "A"
#endif

function ShowWindow(hWnd: HWND; uCmdShow: Integer): BOOL;
external 'ShowWindow@user32.dll stdcall';

procedure InitializeWizard;
begin
  ShowWindow(WizardForm.Handle, 3);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  DataDir: string;
  CfgFile: string;
  JsonContent: string;
begin
  if CurStep = ssPostInstall then
  begin
    DataDir := ExpandConstant('{commonappdata}\SVLL-IT-Workstation');
    ForceDirectories(DataDir);
    CfgFile := DataDir + '\admin_security.json';
    if not FileExists(CfgFile) then
    begin
      JsonContent := '{"RelayServerUrl":"http://192.168.10.222:5000","MeshCentralUrl":"http://192.168.10.222:8086"}';
      SaveStringToFile(CfgFile, JsonContent, False);
    end;
  end;
end;
