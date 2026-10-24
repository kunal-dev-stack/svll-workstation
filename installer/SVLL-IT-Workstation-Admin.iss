; =========================================================================
; Shree Vasu Logistics Limited - IT Workstation ADMIN EDITION Setup Script
; Developed by: Kunal Turkar
; =========================================================================

#define MyAppName "SVLL IT Workstation (Admin Edition)"
#define MyAppVersion "8.3"
#define MyAppPublisher "Shree Vasu Logistics Limited"
#define MyAppURL "https://svll.in"
#define MyAppExeName "SVLL-IT-Workstation-Admin.exe"

[Setup]
AppId={{5E97D4B6-6B22-4CA2-9D0E-C24A5FD029F1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\ShreeVasuLogistics\ITWorkstationAdmin
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\release
OutputBaseFilename=SVLL-IT-Workstation-Admin-v8.3-Setup
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
Source: "..\publish-admin\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "app_logo.ico"; DestDir: "{app}"; DestName: "app_logo.ico"; Flags: ignoreversion
Source: "..\src\tools\*"; DestDir: "{app}\tools"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\tools\*"; DestDir: "{commonpf}\RustDesk"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_logo.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_logo.ico"; Tasks: desktopicon

[Run]
Filename: "{commonpf}\RustDesk\rustdesk.exe"; Parameters: "--silent-install"; Flags: runhidden; Check: FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe'))
Filename: "{app}\tools\rustdesk.exe"; Parameters: "--silent-install"; Flags: runhidden; Check: not FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe'))
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""UltraRemotePort21118"" dir=in action=allow protocol=TCP localport=21118 enable=yes"; Flags: runhidden
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""UltraRemoteUDP"" dir=in action=allow protocol=UDP localport=21118-21119 enable=yes"; Flags: runhidden
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""UltraRemoteApp"" dir=in action=allow program=""{commonpf}\RustDesk\rustdesk.exe"" enable=yes"; Flags: runhidden; Check: FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe'))
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""UltraRemoteApp2"" dir=in action=allow program=""{app}\tools\rustdesk.exe"" enable=yes"; Flags: runhidden
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent


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

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('net.exe', 'stop RustDesk', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM rustdesk.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
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
      JsonContent := '{"RelayServerUrl":"http://192.168.10.222:5000"}';
      SaveStringToFile(CfgFile, JsonContent, False);
    end;
  end;
end;
