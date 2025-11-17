param(
  [string]$PackagePath,
  [string]$InstallDir,
  [switch]$NoPause
)

if (-not (Test-Path $PackagePath)) { throw "Pacote não encontrado: $PackagePath" }
if (-not (Test-Path $InstallDir)) { throw "Diretório de instalação não encontrado: $InstallDir" }

$temp = New-Item -ItemType Directory -Force -Path (Join-Path $env:TEMP ("CoinCraftUpdate_" + [guid]::NewGuid().ToString()))
Expand-Archive -Path $PackagePath -DestinationPath $temp.FullName -Force

# Encerrar instância do app antes de substituir arquivos
try {
  Get-Process -Name 'CoinCraft.App' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  Wait-Process -Name 'CoinCraft.App' -Timeout 5 -ErrorAction SilentlyContinue
} catch {}

# Presume que o conteúdo do zip é uma pasta 'payload' com os binários
$payload = Join-Path $temp.FullName 'payload'
if (-not (Test-Path $payload)) { $payload = $temp.FullName }

Get-ChildItem -Path $payload -Recurse | ForEach-Object {
  $dest = $_.FullName.Replace($payload, $InstallDir)
  if ($_.PSIsContainer) {
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
  } else {
    Copy-Item -Path $_.FullName -Destination $dest -Force
  }
}

Write-Host "Atualização aplicada em: $InstallDir" -ForegroundColor Green
if (-not $NoPause) { Read-Host "Pressione Enter para sair" }