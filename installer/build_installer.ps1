# Script para construir o instalador do CoinCraft e garantir versão única
# Localização: installer/build_installer.ps1

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectRoot = "$scriptPath\.."
$installerDir = $scriptPath
$publishDir = "$projectRoot\publish_final"
$outputExe = "$installerDir\SetupCoinCraft.exe"

Write-Host "=== Iniciando Build do Instalador CoinCraft ===" -ForegroundColor Cyan

# 1. Limpeza de versões anteriores e duplicatas
Write-Host "1. Verificando versões antigas..."
if (Test-Path $outputExe) {
    try {
        Remove-Item $outputExe -Force -ErrorAction Stop
        Write-Host "   Removido instalador anterior." -ForegroundColor Yellow
    } catch {
        Write-Warning "   Não foi possível remover o instalador anterior (em uso). Prosseguindo."
    }
}

# Remove quaisquer arquivos com padrão de versão antiga
Get-ChildItem "$installerDir\CoinCraftSetup_*.exe" | Remove-Item -Force -Verbose -ErrorAction SilentlyContinue
Get-ChildItem "$installerDir\InstalarCoinCraft_*.exe" | Remove-Item -Force -Verbose -ErrorAction SilentlyContinue
if (Test-Path "$installerDir\CoinCraftSetup.exe") { Remove-Item "$installerDir\CoinCraftSetup.exe" -Force -Verbose }
Get-ChildItem "$installerDir\Output" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse

# 2. Publicação do Projeto .NET
Write-Host "2. Publicando aplicação .NET..."
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
#dotnet publish "$projectRoot\src\CoinCraft.App\CoinCraft.App.csproj" -c Release -o $publishDir /p:DebugType=None /p:DebugSymbols=false
# Publica x86 self-contained para garantir que o instalador tenha todos os binários
dotnet publish "$projectRoot\src\CoinCraft.App\CoinCraft.App.csproj" -c Release -r win-x86 -o $publishDir --self-contained true /p:DebugType=None /p:DebugSymbols=false

if (-not (Test-Path "$publishDir\CoinCraft.App.exe")) {
    Write-Error "Falha na publicação. Executável não encontrado."
}

# Verificar se public.xml foi copiado
if (-not (Test-Path "$publishDir\public.xml")) {
    Write-Warning "public.xml não encontrado no publish. Copiando manualmente..."
    Copy-Item "$projectRoot\src\CoinCraft.App\public.xml" "$publishDir\public.xml"
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
