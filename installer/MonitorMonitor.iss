; MonitorMonitor installer (Inno Setup 6)
; Installs mmcli.exe + mmtray.exe, no .NET runtime required (both are native AOT).

#define MyAppName       "MonitorMonitor"
#define MyAppVersion    "0.1.0"
#define MyAppPublisher  "MonitorMonitor"
#define MyAppExeName    "mmtray.exe"

[Setup]
AppId={{F18A8E2E-6297-477F-85C7-0D72BCB710F9}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=dist
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ChangesEnvironment=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "addtopath"; Description: "Add to PATH (enables 'mmcli' from any shell)"; GroupDescription: "Additional options:"
Name: "autostart"; Description: "Start the tray on Windows login"; GroupDescription: "Additional options:"

[Files]
Source: "staging\mmcli.exe";  DestDir: "{app}"; Flags: ignoreversion
Source: "staging\mmtray.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName} Tray"; Filename: "{app}\mmtray.exe"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\mmtray.exe"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\mmtray.exe"; Description: "Launch tray now"; Flags: postinstall nowait skipifsilent

[Code]
const
  UserEnvKey   = 'Environment';
  SystemEnvKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment';

function GetEnvRoot(): Integer;
begin
  if IsAdminInstallMode() then
    Result := HKEY_LOCAL_MACHINE
  else
    Result := HKEY_CURRENT_USER;
end;

function GetEnvSubkey(): string;
begin
  if IsAdminInstallMode() then
    Result := SystemEnvKey
  else
    Result := UserEnvKey;
end;

procedure EnvAddPath(NewPath: string);
var
  Paths: string;
begin
  if not RegQueryStringValue(GetEnvRoot(), GetEnvSubkey(), 'Path', Paths) then
    Paths := '';
  if Pos(';' + Uppercase(NewPath) + ';', ';' + Uppercase(Paths) + ';') > 0 then
    exit;
  if (Paths <> '') and (Paths[Length(Paths)] <> ';') then
    Paths := Paths + ';';
  Paths := Paths + NewPath;
  RegWriteExpandStringValue(GetEnvRoot(), GetEnvSubkey(), 'Path', Paths);
end;

procedure EnvRemovePath(OldPath: string);
var
  Paths: string;
  P: Integer;
begin
  if not RegQueryStringValue(GetEnvRoot(), GetEnvSubkey(), 'Path', Paths) then
    exit;
  P := Pos(';' + Uppercase(OldPath) + ';', ';' + Uppercase(Paths) + ';');
  if P = 0 then
    exit;
  Delete(Paths, P, Length(OldPath) + 1);
  RegWriteExpandStringValue(GetEnvRoot(), GetEnvSubkey(), 'Path', Paths);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('addtopath') then
    EnvAddPath(ExpandConstant('{app}'));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    EnvRemovePath(ExpandConstant('{app}'));
end;
