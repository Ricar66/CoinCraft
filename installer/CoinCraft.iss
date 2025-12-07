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
OutputDir=.
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

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; ATENÇÃO: Aqui ele busca os arquivos que você gerou com o comando 'dotnet publish' para x86 (Universal)
Source: "..\publish_final\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\coincraft.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\coincraft.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: postinstall skipifsilent
