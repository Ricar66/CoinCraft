param(
  [ValidateSet('x64','x86','both')]
  [string]$Platform = 'both',
  [string]$Configuration = 'Release',
  [switch]$SingleFile = $true,
  [switch]$SelfContained = $true
)

$ErrorActionPreference = 'Stop'

function Ensure-Dotnet() {
  try { & dotnet --version | Out-Null } catch { throw 'dotnet SDK não encontrado. Instale o .NET SDK 8.0 ou superior.' }
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$appProj = Join-Path $repoRoot 'src\CoinCraft.App\CoinCraft.App.csproj'
$distDir = Join-Path $repoRoot 'dist\CoinCraft_Distribuicao'
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

Ensure-Dotnet

function Publish-Exe([string]$rid) {
  Write-Host "Publicando executável $rid ($Configuration)" -ForegroundColor Cyan
  $props = @()
  if ($SingleFile) { $props += '-p:PublishSingleFile=true'; $props += '-p:IncludeNativeLibrariesForSelfExtract=true'; $props += '-p:EnableCompressionInSingleFile=true' }
  if ($SelfContained) { $props += '--self-contained true' } else { $props += '--self-contained false' }

  $cmd = @('publish', $appProj, '-c', $Configuration, '-r', $rid) + $props
  & dotnet $cmd | Write-Output

  $tfm = 'net8.0-windows10.0.19041.0'
  $pubDir = Join-Path $repoRoot "src\CoinCraft.App\bin\$Configuration\$tfm\$rid\publish"
  $exe = Join-Path $pubDir 'CoinCraft.App.exe'
  if (-not (Test-Path $exe)) { throw "Executável não encontrado em $pubDir" }
  $destName = "CoinCraft_${rid}.exe"
  $destPath = Join-Path $distDir $destName
  Copy-Item $exe $destPath -Force
  Write-Host "Copiado: $destName" -ForegroundColor Green
}

switch ($Platform) {
  'x64' { Publish-Exe 'win-x64' }
  'x86' { Publish-Exe 'win-x86' }
  'both' { Publish-Exe 'win-x64'; Publish-Exe 'win-x86' }
}

Write-Host "Concluído. Executáveis em $distDir" -ForegroundColor Green