Param(
  [Parameter(Mandatory=$false)] [string]$MsixPath,
  [Parameter(Mandatory=$false)] [string]$CertPath,
  [switch]$CreateDesktopShortcut = $true,
  [switch]$AutoElevate = $true
)

Write-Host '== CoinCraft Installer ==' -ForegroundColor Cyan
$ErrorActionPreference = 'Stop'

# Funções auxiliares
function Test-Admin {
  $wi = [Security.Principal.WindowsIdentity]::GetCurrent()
  $wp = New-Object Security.Principal.WindowsPrincipal($wi)
  return $wp.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Resolve paths robustly (prefer current location and script folder over System32)
function Resolve-ExistingPath([string]$path) {
  try {
    if ([System.IO.Path]::IsPathRooted($path) -and (Test-Path $path)) {
      return (Resolve-Path $path).Path
    }
  } catch {}
  if (Test-Path $path) { return (Resolve-Path $path).Path }
  $leaf = Split-Path $path -Leaf
  $candidateScript = Join-Path $PSScriptRoot $leaf
  if (Test-Path $candidateScript) { return (Resolve-Path $candidateScript).Path }
  $candidateCwd = Join-Path (Get-Location).Path $leaf
  if (Test-Path $candidateCwd) { return (Resolve-Path $candidateCwd).Path }
  throw "Arquivo não encontrado: $path"
}

# Auto-detecção de caminhos se não informados
if (-not $MsixPath) {
  if ([Environment]::Is64BitOperatingSystem) {
    $MsixPath = '.\\CoinCraft_x64.msix'
  } else {
    $MsixPath = '.\\CoinCraft_x86.msix'
  }
}
if (-not $CertPath) {
  $CertPath = '.\\CoinCraft_public_der.cer'
}

$MsixPath = Resolve-ExistingPath $MsixPath
try { $CertPath = Resolve-ExistingPath $CertPath } catch {
  # fallback para Base64
  $CertPath = Resolve-ExistingPath '.\\CoinCraft_public_base64.cer'
}
Write-Host "MSIX: $MsixPath"
Write-Host "CER : $CertPath"

if (-not (Test-Path $CertPath)) { throw "Certificado público não encontrado: $CertPath" }
if (-not (Test-Path $MsixPath)) { throw "Pacote MSIX não encontrado: $MsixPath" }

# Autoelevação para importar em LocalMachine quando necessário
if ($AutoElevate -and -not (Test-Admin)) {
  Write-Host 'Solicitando elevação para importar certificado em LocalMachine...' -ForegroundColor Yellow
  $args = @('-NoProfile','-ExecutionPolicy','Bypass','-File', (Resolve-Path $MyInvocation.MyCommand.Path).Path,
            '-MsixPath', '"' + $MsixPath + '"', '-CertPath', '"' + $CertPath + '"',
            '-CreateDesktopShortcut', '-AutoElevate:$false')
  Start-Process -FilePath 'powershell.exe' -ArgumentList $args -Verb RunAs
  return
}

# Import certificate into TrustedPeople and Root (CurrentUser)
try {
  Write-Host 'Importando certificado em TrustedPeople...' -ForegroundColor Yellow
  Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
  Write-Host 'Importando certificado em Root...' -ForegroundColor Yellow
  Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
  # Tentar importar em LocalMachine Root (se em modo elevado)
  try {
    Write-Host 'Importando certificado em Root (LocalMachine)...' -ForegroundColor Yellow
    Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
    # TrustedPeople em LocalMachine pode não existir em algumas versões; tentar se disponível
    if (Test-Path 'Cert:\LocalMachine\TrustedPeople') {
      Write-Host 'Importando certificado em TrustedPeople (LocalMachine)...' -ForegroundColor Yellow
      Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
    }
  } catch {
    Write-Host 'Aviso: não foi possível importar em LocalMachine (execute como Administrador para isso).' -ForegroundColor DarkYellow
  }
} catch {
  Write-Host "Falha ao importar certificado: $($_.Exception.Message)" -ForegroundColor Red
  Write-Host 'Dica: tente usar CoinCraft_public_base64.cer se o DER falhar.' -ForegroundColor Yellow
  throw
}

# Remover pacote CoinCraft existente para evitar erro 0x80073CF3
Write-Host 'Verificando pacote CoinCraft instalado e removendo se necessário...' -ForegroundColor Yellow
try {
  $identityName = 'CoinCraft'
  $existing = Get-AppxPackage -Name $identityName -ErrorAction SilentlyContinue
  if ($existing) {
    Write-Host "Removendo pacote existente: $($existing.PackageFullName)" -ForegroundColor Yellow
    Remove-AppxPackage -Package $existing.PackageFullName
    Start-Sleep -Seconds 2
  }
} catch {
  Write-Warning "Falha ao remover pacote existente: $($_.Exception.Message)"
}

# Install MSIX
try {
  Write-Host 'Instalando MSIX...' -ForegroundColor Yellow
  Add-AppxPackage -Path $MsixPath -ForceUpdateFromAnyVersion
} catch {
  Write-Host "Falha ao instalar MSIX: $($_.Exception.Message)" -ForegroundColor Red
  $msg = $_.Exception.Message
  $activityId = $null
  if ($msg -match 'ActivityId:\s*([0-9a-fA-F-]+)') { $activityId = $matches[1] }
  if ($activityId) {
    Write-Host "ActivityId: $activityId" -ForegroundColor Yellow
    try {
      Get-AppxLog -ActivityId $activityId | Select-Object -Last 40 | Format-Table -AutoSize
    } catch {
      Write-Host "Não foi possível obter o AppxLog: $($_.Exception.Message)" -ForegroundColor DarkYellow
    }
  }
  # Sugerir habilitar Modo Desenvolvedor
  try {
    $devKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
    $devMode = (Get-ItemProperty -Path $devKey -Name 'AllowDevelopmentWithoutDevLicense' -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
    if ($devMode -ne 1) {
      Write-Host 'Dica: Habilite "Modo Desenvolvedor" em Configurações > Para Desenvolvedores.' -ForegroundColor DarkYellow
    }
  } catch {}
  Write-Host 'Sugestões: verifique arquitetura (x64/x86), certificado confiado (Root/TrustedPeople) e políticas corporativas.' -ForegroundColor DarkYellow
  throw
}

Write-Host 'Instalação concluída.' -ForegroundColor Green
Write-Host 'Se houver erro de arquitetura, tente com CoinCraft_x86.msix.' -ForegroundColor DarkGray

if ($CreateDesktopShortcut) {
  try {
    Write-Host 'Criando atalho na área de trabalho...' -ForegroundColor Yellow
    # Obter PackageFamilyName a partir do nome Identity do manifest
    $identityName = 'CoinCraft' # conforme Package.appxmanifest
    $pkg = Get-AppxPackage -Name $identityName -ErrorAction SilentlyContinue
    if (-not $pkg) { $pkg = Get-AppxPackage | Where-Object { $_.Name -eq $identityName } }
    if ($pkg) {
      $pfn = $pkg.PackageFamilyName
      $appId = 'CoinCraft' # conforme Application Id no manifest
      $target = "shell:AppsFolder/$pfn!$appId"
      $desktop = [Environment]::GetFolderPath('Desktop')
      $shortcutPath = Join-Path $desktop 'CoinCraft.lnk'
      $shell = New-Object -ComObject WScript.Shell
      $shortcut = $shell.CreateShortcut($shortcutPath)
      $shortcut.TargetPath = $target
      $shortcut.IconLocation = $target
      $shortcut.Save()
      Write-Host "Atalho criado: $shortcutPath" -ForegroundColor Green
    } else {
      Write-Host 'Não foi possível localizar o pacote instalado para criar o atalho.' -ForegroundColor Yellow
    }
  } catch {
    Write-Host "Falha ao criar atalho: $($_.Exception.Message)" -ForegroundColor Red
  }
}