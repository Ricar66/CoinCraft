# Script para corrigir ícones de atalhos do CoinCraft
# Localização: scripts/fix_shortcuts.ps1

$appName = "CoinCraft"
$targetIconName = "coincraft.ico"

# Caminho do ícone na pasta de instalação (assumindo instalação padrão)
# Tenta encontrar onde o app está instalado
$installPath = "$env:LOCALAPPDATA\Programs\CoinCraft" # Caminho padrão do Inno Setup com {autopf} muitas vezes vai para AppData se não for admin, ou Program Files.
# Vamos verificar Program Files também
if (-not (Test-Path "$installPath\CoinCraft.App.exe")) {
    $installPath = "${env:ProgramFiles(x86)}\CoinCraft"
}
if (-not (Test-Path "$installPath\CoinCraft.App.exe")) {
    $installPath = "$env:ProgramFiles\CoinCraft"
}

# Se não achou instalado, pode ser ambiente de dev. Vamos usar o ícone local do repo se existir.
if (-not (Test-Path "$installPath\CoinCraft.App.exe")) {
    Write-Host "Instalação não encontrada em locais padrão. Verificando ambiente local..."
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $repoIcon = "$scriptPath\..\src\CoinCraft.App\coincraft.ico"
    if (Test-Path $repoIcon) {
        $iconPath = $repoIcon
        Write-Host "Usando ícone do repositório: $iconPath"
    } else {
        Write-Error "Ícone não encontrado."
        exit 1
    }
} else {
    $iconPath = "$installPath\$targetIconName"
}

if (-not (Test-Path $iconPath)) {
    Write-Warning "Arquivo de ícone não encontrado em: $iconPath"
    # Fallback para o exe
    if (Test-Path "$installPath\CoinCraft.App.exe") {
        $iconPath = "$installPath\CoinCraft.App.exe"
    }
}

Write-Host "Ícone alvo: $iconPath"

$shell = New-Object -ComObject WScript.Shell

function Update-Shortcut($path) {
    if (Test-Path $path) {
        try {
            $shortcut = $shell.CreateShortcut($path)
            if ($shortcut.TargetPath -like "*CoinCraft*") {
                Write-Host "Atualizando ícone de: $path"
                $shortcut.IconLocation = "$iconPath,0"
                $shortcut.Save()
            }
        } catch {
            $err = $_
            Write-Warning ("Erro ao atualizar {0}: {1}" -f $path, $err)
        }
    }
}

# 1. Desktop (Usuário e Public)
$desktop = [Environment]::GetFolderPath("Desktop")
$commonDesktop = [Environment]::GetFolderPath("CommonDesktopDirectory")
Get-ChildItem "$desktop\*.lnk" -ErrorAction SilentlyContinue | ForEach-Object { Update-Shortcut $_.FullName }
Get-ChildItem "$commonDesktop\*.lnk" -ErrorAction SilentlyContinue | ForEach-Object { Update-Shortcut $_.FullName }

# 2. Start Menu (Usuário e Public)
$startMenu = [Environment]::GetFolderPath("StartMenu")
$commonStartMenu = [Environment]::GetFolderPath("CommonStartMenu")
Get-ChildItem "$startMenu\Programs\*.lnk" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { Update-Shortcut $_.FullName }
Get-ChildItem "$commonStartMenu\Programs\*.lnk" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { Update-Shortcut $_.FullName }

# 3. Taskbar (User Pinned)
$taskbarPath = "$env:APPDATA\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"
if (Test-Path $taskbarPath) {
    Get-ChildItem "$taskbarPath\*.lnk" -ErrorAction SilentlyContinue | ForEach-Object { Update-Shortcut $_.FullName }
}

# 4. Forçar refresh do Explorer (opcional, mas recomendado para ver mudanças)
# Não vamos matar o explorer em sessão remota para não desconectar, mas podemos notificar.
Write-Host "Atalhos atualizados. Se o ícone não mudar imediatamente, reinicie o Explorer ou faça logoff/login."
