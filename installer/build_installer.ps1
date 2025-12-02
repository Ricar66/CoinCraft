# Script para construir o instalador do CoinCraft e garantir versão única
# Localização: installer/build_installer.ps1

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectRoot = "$scriptPath\.."
$installerDir = $scriptPath
$publishDir = "$projectRoot\publish_final"
$outputExe = "$installerDir\CoinCraftSetup.exe"

Write-Host "=== Iniciando Build do Instalador CoinCraft ===" -ForegroundColor Cyan

# 1. Limpeza de versões anteriores e duplicatas
Write-Host "1. Verificando versões antigas..."
if (Test-Path $outputExe) {
    Remove-Item $outputExe -Force
    Write-Host "   Removido instalador anterior." -ForegroundColor Yellow
}

# Remove quaisquer arquivos com padrão de versão antiga (ex: *_v3.exe, *_v4.exe)
Get-ChildItem "$installerDir\InstalarCoinCraft_Setup_*.exe" | Remove-Item -Force -Verbose
Get-ChildItem "$installerDir\Output" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse

# 2. Publicação do Projeto .NET
Write-Host "2. Publicando aplicação .NET..."
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish "$projectRoot\src\CoinCraft.App\CoinCraft.App.csproj" -c Release -o $publishDir /p:DebugType=None /p:DebugSymbols=false

if (-not (Test-Path "$publishDir\CoinCraft.App.exe")) {
    Write-Error "Falha na publicação. Executável não encontrado."
}

# 3. Compilação do Instalador (Inno Setup)
Write-Host "3. Compilando Instalador com Inno Setup..."
# Tenta encontrar o ISCC no PATH ou em locais comuns
$iscc = "ISCC.exe"
if (-not (Get-Command $iscc -ErrorAction SilentlyContinue)) {
    $possiblePaths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    foreach ($p in $possiblePaths) {
        if (Test-Path $p) {
            $iscc = $p
            break
        }
    }
}

if (-not (Get-Command $iscc -ErrorAction SilentlyContinue) -and -not (Test-Path $iscc)) {
    Write-Warning "Compilador Inno Setup (ISCC.exe) não encontrado no PATH ou locais padrão."
    Write-Warning "Por favor, instale o Inno Setup 6+ e adicione ao PATH, ou compile o .iss manualmente."
    exit 1
}

& $iscc "$installerDir\CoinCraft.iss" | Out-Null

# 4. Validação Final
if (Test-Path $outputExe) {
    Write-Host "SUCESSO: Instalador gerado em:" -ForegroundColor Green
    Write-Host "   $outputExe" -ForegroundColor Green
    Write-Host "   Tamanho: $(("{0:N2} MB" -f ((Get-Item $outputExe).Length / 1MB)))"
} else {
    Write-Error "FALHA: O instalador não foi gerado."
}

Write-Host "=== Processo Concluído ===" -ForegroundColor Cyan
