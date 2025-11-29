; Script gerado para o Inno Setup
; O nome do aplicativo é definido aqui
#define MyAppName "CoinCraft"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "CodeCraftGenz"
#define MyAppURL "https://www.codecraftgenz.com.br/"
#define MyAppExeName "CoinCraft.App.exe"

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
OutputDir=..\installer\Output
OutputBaseFilename=InstalarCoinCraft_v1.0.1
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; ATENÇÃO: Aqui ele busca os arquivos que você gerou com o comando 'dotnet publish'
; O comando de publish deve ter sido: dotnet publish ... -o ./publish
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\LatoFont\*"; DestDir: "{app}\LatoFont"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "..\publish\public.xml"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\publish\public.pem"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: postinstall skipifsilent
