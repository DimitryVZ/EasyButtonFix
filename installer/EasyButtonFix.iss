; Inno-Setup-Skript für EasyButtonFix
; Bauen: .\build.ps1  (oder in Inno Setup öffnen und F9)

#define AppName      "EasyButtonFix"
#define AppExe       "EasyButtonFix.exe"
#define BinDir       "..\src\EasyButtonFix\bin\Release\net48"
#define AppVersion   GetStringFileInfo(BinDir + "\" + AppExe, "ProductVersion")
#define AppPublisher "DimitryVZ"
#define AppURL       "https://github.com/DimitryVZ/EasyButtonFix"

[Setup]
; Die AppId NIE ändern – daran erkennt Windows Updates der gleichen App
AppId={{08218F3C-F7ED-431A-8A8E-08A6D3BDDF73}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Ohne Admin-Rechte für den aktuellen Nutzer installieren (optional für alle)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\publish
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
SetupIconFile=..\src\EasyButtonFix\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
WizardSizePercent=100
Compression=lzma2/ultra64
SolidCompression=yes
MinVersion=10.0
; Sprache automatisch nach Windows wählen, Auswahl nur zeigen, wenn keine passt
ShowLanguageDialog=auto
; Läuft die App gerade, wird sie vor Update/Deinstallation per "--exit" beendet (siehe [Code])
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
de.Autostart=Mit Windows starten (empfohlen)
en.Autostart=Start with Windows (recommended)
de.Extra=Zusätzlich:
en.Extra=Additional options:
de.Launch=EasyButtonFix jetzt starten
en.Launch=Launch EasyButtonFix now

[Tasks]
Name: "autostart";   Description: "{cm:Autostart}";              GroupDescription: "{cm:Extra}"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}";      GroupDescription: "{cm:Extra}"; Flags: unchecked

[Files]
Source: "{#BinDir}\*.exe";    DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDir}\*.dll";    DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDir}\*.config"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}";  Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Gleicher Eintrag wie der Schalter "Mit Windows starten" in der App
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; \
    ValueData: """{app}\{#AppExe}"" --tray"; Tasks: autostart

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:Launch}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\{#AppName}"

[Code]
// Laufende App sauber beenden (sie speichert dabei ihre Einstellungen und entfernt das Tray-Symbol)
procedure CloseRunningApp();
var
  Exe: String;
  R: Integer;
begin
  Exe := ExpandConstant('{app}\{#AppExe}');
  if FileExists(Exe) then
  begin
    Exec(Exe, '--exit', '', SW_HIDE, ewWaitUntilTerminated, R);
    Sleep(500);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  CloseRunningApp();
  Result := '';
end;

function InitializeUninstall(): Boolean;
begin
  CloseRunningApp();
  Result := True;
end;

// Autostart-Eintrag bei der Deinstallation immer entfernen – auch wenn er erst später in der App aktiviert wurde
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#AppName}');
end;
