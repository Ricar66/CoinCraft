; Script gerado para o Inno Setup
; O nome do aplicativo é definido aqui
#define MyAppName "CoinCraft"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "CodeCraftGenz"
#define MyAppURL "https://www.codecraftgenz.com.br/"
#define MyAppExeName "CoinCraft.App.exe"
#define MyAppCopyright "Copyright (C) 2025 CodeCraftGenz"

[Setup]
; Este GUID deve ser único para o seu app. Se quiser gerar um novo, use o menu Tools -> Generate GUID do Inno Setup
AppId={{E8A5E397-3335-4924-8173-13206228E4E5}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Remove a mensagem "O instalador já está rodando" se for atualização silenciosa
SetupMutex=CoinCraftSetupMutex
; Local onde o instalador final será salvo (pasta Output na raiz ou onde você preferir)
OutputDir=Output
OutputBaseFilename=SetupCoinCraft
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupIconFile=coincraft.ico
UninstallDisplayIcon={app}\coincraft.ico
AppCopyright={#MyAppCopyright}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Instalador do CoinCraft
VersionInfoCopyright={#MyAppCopyright}
VersionInfoProductName={#MyAppName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "forcex86"; Description: "Instalar versão 32-bit (x86)"; Flags: unchecked

[Files]
; Conteúdo x64 (instalado automaticamente em sistemas 64-bit, exceto se força x86)
Source: "..\publish_final\x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: UseX64
; Conteúdo x86 (instalado em sistemas 32-bit ou quando forçado)
Source: "..\publish_final\x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: UseX86
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\coincraft.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\coincraft.ico"

[Run]
; Executa instalação via Winget se runtime estiver ausente (fallback abre página de download)
Filename: "winget"; Parameters: "install --id Microsoft.DotNet.DesktopRuntime.8 --source winget --silent --accept-package-agreements --accept-source-agreements"; StatusMsg: "Instalando .NET Desktop Runtime 8.0..."; Flags: runhidden waituntilterminated; Check: (not HasWinDesktopRuntime80())
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: postinstall skipifsilent; Check: HasWinDesktopRuntime80

[Code]
function IsForceX86(): Boolean;
begin
  Result := WizardIsTaskSelected('forcex86');
end;

function UseX64(): Boolean;
begin
  Result := IsWin64 and (not IsForceX86);
end;

function UseX86(): Boolean;
begin
  Result := (not IsWin64) or IsForceX86;
end;

function HasWinDesktopRuntime80(): Boolean;
var SubKeys: TArrayOfString;
    i: Integer;
    KeyPath: String;
begin
  Result := False;
  if IsWin64 then
    KeyPath := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App'
  else
    KeyPath := 'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App';

  if RegGetSubkeyNames(HKLM, KeyPath, SubKeys) then
  begin
    for i := 0 to GetArrayLength(SubKeys)-1 do
    begin
      if Pos('8.0', SubKeys[i]) = 1 then
      begin
        Result := True;
        exit;
      end;
    end;
  end;
end;

procedure InitializeWizard;
var ok: Boolean;
    err: Integer;
begin
  if not HasWinDesktopRuntime80() then
  begin
    MsgBox('O .NET Desktop Runtime 8.0 não foi detectado neste computador. O instalador abrirá a página oficial para download. Após instalar o runtime, execute novamente este instalador.', mbInformation, MB_OK);
    ok := ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0', '', '', SW_SHOWNORMAL, False, err);
  end;
end;
