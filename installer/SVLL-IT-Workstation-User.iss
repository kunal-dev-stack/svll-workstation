; =========================================================================
; Shree Vasu Logistics Limited - IT Workstation USER EDITION Setup Script
; Developed by: Kunal Turkar
; =========================================================================

#define MyAppName "SVLL IT Support Workstation"
#define MyAppVersion "8.3"
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
OutputBaseFilename=SVLL-IT-Workstation-User-v8.3-Setup
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
Source: "..\src\tools\*"; DestDir: "{app}\tools"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\tools\*"; DestDir: "{commonpf}\RustDesk"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_logo.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_logo.ico"; Tasks: desktopicon

[Run]
Filename: "{commonpf}\RustDesk\rustdesk.exe"; Parameters: "--silent-install"; Flags: runhidden; Check: FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe'))
Filename: "{app}\tools\rustdesk.exe"; Parameters: "--silent-install"; Flags: runhidden; Check: not FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe'))
Filename: "{commonpf}\RustDesk\rustdesk.exe"; Parameters: "--password SVLL@Remote2026"; Flags: runhidden; Check: FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe'))
Filename: "{app}\tools\rustdesk.exe"; Parameters: "--password SVLL@Remote2026"; Flags: runhidden; Check: not FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe'))
Filename: "net.exe"; Parameters: "start RustDesk"; Flags: runhidden
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""UltraRemotePort21118"" dir=in action=allow protocol=TCP localport=21118 enable=yes"; Flags: runhidden
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""UltraRemoteUDP"" dir=in action=allow protocol=UDP localport=21118-21119 enable=yes"; Flags: runhidden
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""UltraRemoteApp"" dir=in action=allow program=""{commonpf}\RustDesk\rustdesk.exe"" enable=yes"; Flags: runhidden; Check: FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe'))
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""UltraRemoteApp2"" dir=in action=allow program=""{app}\tools\rustdesk.exe"" enable=yes"; Flags: runhidden
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

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('net.exe', 'stop RustDesk', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM rustdesk.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

procedure ConfigureDirectServer(const TomlFile: string);
var
  Lines: TArrayOfString;
  I: Integer;
  FoundDirectServer: Boolean;
  FoundDirectPort: Boolean;
  FoundOptions: Boolean;
  TomlContent: string;
  ParentDir: string;
begin
  ParentDir := ExtractFileDir(TomlFile);
  ForceDirectories(ParentDir);

  FoundDirectServer := False;
  FoundDirectPort := False;
  FoundOptions := False;

  if FileExists(TomlFile) then
  begin
    if LoadStringsFromFile(TomlFile, Lines) then
    begin
      for I := 0 to GetArrayLength(Lines) - 1 do
      begin
        if Pos('[options]', Lines[I]) > 0 then
          FoundOptions := True;
        if Pos('direct-server', Lines[I]) > 0 then
        begin
          Lines[I] := 'direct-server = ''Y''';
          FoundDirectServer := True;
        end;
        if Pos('direct-access-port', Lines[I]) > 0 then
        begin
          Lines[I] := 'direct-access-port = ''21118''';
          FoundDirectPort := True;
        end;
      end;

      if not FoundDirectServer then
      begin
        SetArrayLength(Lines, GetArrayLength(Lines) + 2);
        if not FoundOptions then
        begin
          SetArrayLength(Lines, GetArrayLength(Lines) + 1);
          Lines[GetArrayLength(Lines) - 3] := '[options]';
        end;
        Lines[GetArrayLength(Lines) - 2] := 'direct-server = ''Y''';
        Lines[GetArrayLength(Lines) - 1] := 'direct-access-port = ''21118''';
      end;

      SaveStringsToFile(TomlFile, Lines, False);
    end;
  end
  else
  begin
    TomlContent := '[options]' + #13#10 + 'direct-server = ''Y''' + #13#10 + 'direct-access-port = ''21118''' + #13#10;
    SaveStringToFile(TomlFile, TomlContent, False);
  end;
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

    // Enable direct-server in LocalService and User profiles for unattended peer connections
    ConfigureDirectServer(ExpandConstant('{win}\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk2.toml'));
    ConfigureDirectServer(ExpandConstant('{win}\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk.toml'));
    ConfigureDirectServer(ExpandConstant('{userappdata}\RustDesk\config\RustDesk2.toml'));
    ConfigureDirectServer(ExpandConstant('{userappdata}\RustDesk\config\RustDesk.toml'));

    // Set service ImagePath: prioritize C:\Program Files\RustDesk\rustdesk.exe if present, else {app}\tools\rustdesk.exe
    if FileExists(ExpandConstant('{commonpf}\RustDesk\rustdesk.exe')) then
      RegWriteStringValue(HKEY_LOCAL_MACHINE, 'SYSTEM\CurrentControlSet\Services\RustDesk', 'ImagePath', '"' + ExpandConstant('{commonpf}\RustDesk\rustdesk.exe') + '" --service')
    else
      RegWriteStringValue(HKEY_LOCAL_MACHINE, 'SYSTEM\CurrentControlSet\Services\RustDesk', 'ImagePath', '"' + ExpandConstant('{app}\tools\rustdesk.exe') + '" --service');
  end;
end;


