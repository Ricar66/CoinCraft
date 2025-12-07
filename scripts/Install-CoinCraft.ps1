Param(
  [Parameter(Mandatory=$false)] [string]$MsixPath,
  [Parameter(Mandatory=$false)] [string]$CertPath,
  [switch]$CreateDesktopShortcut = $true
)

Write-Host '== CoinCraft Installer (PS DIST) ==' -ForegroundColor Cyan
$ErrorActionPreference = 'Stop'

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

if (-not $MsixPath) {
  try {
    $distDir = Join-Path $PSScriptRoot '..\\dist\\CoinCraft_Distribuicao'
    if (Test-Path $distDir) {
      $bundle = Get-ChildItem -Path $distDir -File -Filter *.msixbundle -ErrorAction SilentlyContinue | Select-Object -First 1
      if ($bundle) { $MsixPath = $bundle.FullName }
    }
  } catch {}
  if (-not $MsixPath) {
    if ([Environment]::Is64BitOperatingSystem) { $MsixPath = '.\\CoinCraft_x64.msix' } else { $MsixPath = '.\\CoinCraft_x86.msix' }
  }
}
if (-not $CertPath) { $CertPath = (Join-Path $PSScriptRoot 'certs\\CoinCraft_public_der.cer') }

try { $CertPath = Resolve-ExistingPath $CertPath } catch { $CertPath = Resolve-ExistingPath (Join-Path $PSScriptRoot 'certs\\CoinCraft_public_base64.cer') }
Write-Host "CER : $CertPath"

if (-not (Test-Path $CertPath)) { throw "Certificado público não encontrado: $CertPath" }

if (-not (Test-Path $MsixPath)) {
  Write-Host "Tentando baixar automaticamente o MSIX do GitHub..." -ForegroundColor Yellow
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  function Get-GitHubMsix([string]$Repo, [string]$Tag) {
    try {
      $headers = @{ 'User-Agent'='CoinCraftInstaller'; 'Accept'='application/vnd.github+json' }
      $uri = if ($Tag) { "https://api.github.com/repos/$Repo/releases/tags/$Tag" } else { "https://api.github.com/repos/$Repo/releases/latest" }
      $release = Invoke-RestMethod -Headers $headers -Uri $uri -ErrorAction Stop
      $assets = $release.assets
      if (-not $assets -or $assets.Count -eq 0) { return $null }
      # Preferir msixbundle
      $asset = $assets | Where-Object { $_.name -match 'CoinCraft_.*\.msixbundle$' } | Select-Object -First 1
      if (-not $asset) {
        # Fallback para MSIX por arquitetura
        $arch = if ([Environment]::Is64BitOperatingSystem) { 'x64' } else { 'x86' }
        $asset = $assets | Where-Object { $_.name -match ("CoinCraft_" + $arch + "\.msix$") } | Select-Object -First 1
        if (-not $asset) { $asset = $assets | Where-Object { $_.name -match 'CoinCraft_.*\.msix$' } | Select-Object -First 1 }
      }
      if (-not $asset) { return $null }
      $dest = Join-Path (Get-Location).Path $asset.name
      Write-Host ("Baixando " + $asset.name + "...") -ForegroundColor Cyan
      Invoke-WebRequest -Headers $headers -UseBasicParsing -Uri $asset.browser_download_url -OutFile $dest
      Write-Host ("OK: " + $dest) -ForegroundColor Green
      return $dest
    } catch {
      Write-Host ("Falha no download: " + $_.Exception.Message) -ForegroundColor DarkYellow
      return $null
    }
  }
  $repo = 'Ricar66/CoinCraft'
  $downloaded = Get-GitHubMsix -Repo $repo -Tag $null
  if (-not $downloaded) { $downloaded = Get-GitHubMsix -Repo $repo -Tag 'v1.0.2' }
  if ($downloaded) { $MsixPath = $downloaded }

  if (-not (Test-Path $MsixPath)) {
    Write-Host "Pacote não encontrado: $MsixPath" -ForegroundColor Yellow
    Write-Host "Selecione manualmente o arquivo (.msixbundle ou .msix)..." -ForegroundColor Yellow
    try {
      Add-Type -AssemblyName System.Windows.Forms
      $dlg = New-Object System.Windows.Forms.OpenFileDialog
      $dlg.Filter = 'Pacotes (*.msix;*.msixbundle)|*.msix;*.msixbundle'
      $dlg.Title = 'Selecione o pacote MSIX do CoinCraft'
      $dlg.InitialDirectory = (Get-Location).Path
      if ($dlg.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $MsixPath = $dlg.FileName
        Write-Host "MSIX selecionado: $MsixPath" -ForegroundColor Green
      } else { throw "Pacote MSIX não selecionado." }
    } catch { Write-Host "Falha ao abrir seletor: $($_.Exception.Message)" -ForegroundColor Red; throw }
  }
}
Write-Host "Pacote: $MsixPath"

try {
  Write-Host 'Importando certificado em TrustedPeople...' -ForegroundColor Yellow
  Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
  Write-Host 'Importando certificado em Root...' -ForegroundColor Yellow
  Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
  try {
    Write-Host 'Importando certificado em Root (LocalMachine)...' -ForegroundColor Yellow
    Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
    if (Test-Path 'Cert:\LocalMachine\TrustedPeople') {
      Write-Host 'Importando certificado em TrustedPeople (LocalMachine)...' -ForegroundColor Yellow
      Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
    }
  } catch { Write-Host 'Aviso: não foi possível importar em LocalMachine.' -ForegroundColor DarkYellow }
} catch { Write-Host "Falha ao importar certificado: $($_.Exception.Message)" -ForegroundColor Red; throw }

try {
  Write-Host 'Verificando pacote CoinCraft instalado e removendo se necessário...' -ForegroundColor Yellow
  $identityName = 'CoinCraft'
  $existing = Get-AppxPackage -Name $identityName -ErrorAction SilentlyContinue
  if ($existing) {
    $existing | ForEach-Object {
      try { Remove-AppxPackage -Package $_.PackageFullName } catch { Write-Warning "Falha ao remover $($_.PackageFullName): $($_.Exception.Message)" }
    }
    Start-Sleep -Seconds 2
  }
} catch { Write-Warning "Falha ao remover pacote existente: $($_.Exception.Message)" }

try {
  Write-Host 'Instalando pacote...' -ForegroundColor Yellow
  Add-AppxPackage -Path $MsixPath -ForceUpdateFromAnyVersion
} catch {
  Write-Host "Falha ao instalar pacote: $($_.Exception.Message)" -ForegroundColor Red
  $msg = $_.Exception.Message
  $activityId = $null
  if ($msg -match 'ActivityId:\s*([0-9a-fA-F-]+)') { $activityId = $matches[1] }
  if ($activityId) {
    Write-Host "ActivityId: $activityId" -ForegroundColor Yellow
    try { Get-AppxLog -ActivityId $activityId | Select-Object -Last 40 | Format-Table -AutoSize } catch {}
  }
  throw
}

Write-Host 'Instalação concluída.' -ForegroundColor Green

if ($CreateDesktopShortcut) {
  try {
    Write-Host 'Criando atalho na área de trabalho...' -ForegroundColor Yellow
    $identityName = 'CoinCraft'
    $pkg = Get-AppxPackage -Name $identityName -ErrorAction SilentlyContinue
    if (-not $pkg) { $pkg = Get-AppxPackage | Where-Object { $_.Name -eq $identityName } }
    if ($pkg) {
      $pfn = $pkg.PackageFamilyName
      $appId = 'CoinCraft'
      $target = ('shell:AppsFolder/{0}{1}{2}' -f $pfn, [char]33, $appId)
      $desktop = [Environment]::GetFolderPath('Desktop')
      $shortcutPath = Join-Path $desktop 'CoinCraft.lnk'
      $shell = New-Object -ComObject WScript.Shell
      $shortcut = $shell.CreateShortcut($shortcutPath)
      $shortcut.TargetPath = $target
      $shortcut.IconLocation = $target
      $shortcut.Save()
      Write-Host "Atalho criado: $shortcutPath" -ForegroundColor Green
    } else { Write-Host 'Não foi possível localizar o pacote para criar o atalho.' -ForegroundColor Yellow }
  } catch { Write-Host "Falha ao criar atalho: $($_.Exception.Message)" -ForegroundColor Red }
}

# Tentativa de abrir o aplicativo imediatamente após a instalação
try {
  Write-Host 'Abrindo o CoinCraft...' -ForegroundColor Yellow
  $identityName = 'CoinCraft'
  $pkg = Get-AppxPackage -Name $identityName -ErrorAction SilentlyContinue
  if (-not $pkg) { $pkg = Get-AppxPackage | Where-Object { $_.Name -eq $identityName } }
  if ($pkg) {
    $pfn = $pkg.PackageFamilyName
    $appIds = @('CoinCraft','App')
    $launched = $false
    foreach ($appId in $appIds) {
      try {
        $target = ('shell:AppsFolder/{0}{1}{2}' -f $pfn, [char]33, $appId)
        Start-Process explorer.exe $target
        $launched = $true
        break
      } catch {}
    }
    if (-not $launched) { Write-Host 'Não foi possível iniciar via shell:AppsFolder. Verifique o Id do Application no manifest.' -ForegroundColor DarkYellow }
  } else {
    Write-Host 'Pacote não localizado para abertura automática.' -ForegroundColor DarkYellow
  }
} catch { Write-Host "Falha ao abrir o aplicativo: $($_.Exception.Message)" -ForegroundColor Red }