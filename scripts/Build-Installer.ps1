$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$iss = Join-Path $root 'installer\CoinCraft.iss'
if (-not (Test-Path $iss)) { Write-Error "Arquivo não encontrado: $iss"; exit 1 }

$candidates = @(
  'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
  'C:\Program Files\Inno Setup 6\ISCC.exe'
)

$iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
  Write-Error 'Inno Setup não encontrado. Instale o Inno Setup 6 e tente novamente.'
  exit 1
}

& $iscc $iss /Q
Write-Output "Installer compilado com sucesso."
