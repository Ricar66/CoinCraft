param(
  [ValidateSet('x64','x86','both')]
  [string]$Platform = 'both',
  [string]$Configuration = 'Release',
  [string]$SignPfx = '',
  [string]$PfxPassword = ''
)

$ErrorActionPreference = 'Stop'

function Get-MSBuildPath {
  $paths = @(
    'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
  )
  foreach ($p in $paths) { if (Test-Path $p) { return $p } }
  throw 'MSBuild.exe não encontrado. Instale o Visual Studio 2022 (Community/Professional/Enterprise) com Windows App Packaging Tools.'
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$wapproj = Join-Path $repoRoot 'src\CoinCraft.Package\CoinCraft.Package.wapproj'
$distDir = Join-Path $repoRoot 'dist\CoinCraft_Distribuicao'
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$msbuild = Get-MSBuildPath

function Publish-MSIX([string]$plat) {
  Write-Host "Publicando MSIX $plat ($Configuration)" -ForegroundColor Cyan
  $props = @()
  $props += "/p:Configuration=$Configuration"
  $props += "/p:Platform=$plat"
  if ($SignPfx) {
    $props += '/p:AppxPackageSigningEnabled=true'
    $props += "/p:PackageCertificateKeyFile=$SignPfx"
    if ($PfxPassword) { $props += "/p:PackageCertificatePassword=$PfxPassword" }
  }

  & $msbuild $wapproj /t:Restore,Publish /m $props | Write-Output

  $appPackages = Join-Path $repoRoot 'src\CoinCraft.Package\AppPackages'
  $pattern = "CoinCraft.Package_*.msix"
  $searchDir = Join-Path $appPackages "CoinCraft.Package_1.0.0.0_${plat}_Test"
  if (-not (Test-Path $searchDir)) { $searchDir = $appPackages }
  $msix = Get-ChildItem -Path $searchDir -Filter $pattern -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $msix) { throw "MSIX não encontrado para $plat em $appPackages" }

  $destName = "CoinCraft_${plat}.msix"
  $destPath = Join-Path $distDir $destName
  Copy-Item $msix.FullName $destPath -Force
  Write-Host "Copiado: $destName" -ForegroundColor Green
}

switch ($Platform) {
  'x64' { Publish-MSIX 'x64' }
  'x86' { Publish-MSIX 'x86' }
  'both' { Publish-MSIX 'x64'; Publish-MSIX 'x86' }
}

Write-Host "Concluído. Pacotes em $distDir" -ForegroundColor Green