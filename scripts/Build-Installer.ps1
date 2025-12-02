$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$iss = Join-Path $root 'installer\CoinCraft.iss'
if (-not (Test-Path $iss)) { Write-Error "Arquivo não encontrado: $iss"; exit 1 }

# Publish .NET App
$publishDir = Join-Path $root 'publish_final'
Write-Output "Publicando aplicação para: $publishDir"
if (-not (Test-Path $publishDir)) { New-Item -ItemType Directory -Path $publishDir | Out-Null }

# Run dotnet publish
& dotnet publish "$root\src\CoinCraft.App\CoinCraft.App.csproj" -c Release -o "$publishDir"
if ($LASTEXITCODE -ne 0) { Write-Error "Erro ao publicar a aplicação via dotnet publish"; exit 1 }

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
if ($LASTEXITCODE -ne 0) { 
    Write-Error "Erro ao compilar o instalador."
    exit 1 
}

Write-Output "Installer compilado com sucesso."
