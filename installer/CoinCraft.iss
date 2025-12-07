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
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: postinstall skipifsilent

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

function GetDotnetDesktop80UrlX64(): String;
begin
  Result := 'https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-8.0-windows-x64-installer';
end;

function GetDotnetDesktop80UrlX86(): String;
begin
  Result := 'https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-8.0-windows-x86-installer';
end;

function HasDotnetDesktop80Arch(ArchKey: Integer; ArchName: String): Boolean;
var baseKey: String;
    names: TArrayOfString;
    i: Integer;
begin
  Result := False;
  baseKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\' + ArchName + '\sharedfx\Microsoft.WindowsDesktop.App';
  Log('Verificando ' + baseKey);
  if RegGetSubkeyNames(ArchKey, baseKey, names) then
  begin
    for i := 0 to GetArrayLength(names)-1 do
    begin
      if Copy(names[i], 1, 3) = '8.0' then
      begin
        Log('Encontrado subkey: ' + names[i]);
        Result := True;
        exit;
      end;
    end;
  end
  else
  begin
    Log('Chave ausente: ' + baseKey);
  end;
end;

function HasDotnetDesktop80(): Boolean;
var ok: Boolean;
begin
  Log('Detectando .NET Desktop Runtime 8.0');
  if IsWin64 then
  begin
    ok := HasDotnetDesktop80Arch(HKLM64, 'x64');
    if not ok then ok := HasDotnetDesktop80Arch(HKLM64, 'x86');
  end
  else
  begin
    ok := HasDotnetDesktop80Arch(HKLM32, 'x86');
  end;
  if ok then
    Log('Runtime 8.0 presente')
  else
  begin
    Log('Runtime 8.0 não encontrado');
    Log('Referência x64: ' + GetDotnetDesktop80UrlX64());
    Log('Referência x86: ' + GetDotnetDesktop80UrlX86());
  end;
  Result := ok;
end;

procedure InitializeWizard;
begin
  Log('Inicialização do instalador');
  HasDotnetDesktop80();
end;
