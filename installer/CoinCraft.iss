; Inno Setup script para CoinCraft
#define MyAppName "CoinCraft"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CoinCraft"
#define MyAppExeName "CoinCraft.App.exe"

[Setup]
AppId={{6D0F1F4B-6B0A-4E6B-9A04-CCB2CB5F9C01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=dist
OutputBaseFilename=CoinCraft-Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
SetupIconFile=..\src\CoinCraft.App\Icon.ico
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "ptBR"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; Flags: unchecked

[Files]
Source: "..\src\CoinCraft.App\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--init-only"; Flags: nowait runhidden
Filename: "{app}\{#MyAppExeName}"; Description: "Executar {#MyAppName}"; Flags: postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{userappdata}\CoinCraft\license.dat"
Type: files; Name: "{userappdata}\CoinCraft\skip.lic"