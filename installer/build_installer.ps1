# Script para construir o instalador do CoinCraft e garantir versão única
# Localização: installer/build_installer.ps1

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectRoot = "$scriptPath\.."
$installerDir = $scriptPath
$publishUnified = "$projectRoot\publish_final"
$publishDirX86 = "$publishUnified\x86"
$publishDirX64 = "$publishUnified\x64"
$outputExe = "$installerDir\Output\SetupCoinCraft.exe"

Write-Host "=== Iniciando Build do Instalador CoinCraft ===" -ForegroundColor Cyan

# 1. Limpeza de versões anteriores e duplicatas
Write-Host "1. Verificando versões antigas..."
Get-ChildItem "$installerDir\SetupCoinCraft_*.exe" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
if (Test-Path $outputExe) { Remove-Item $outputExe -Force -ErrorAction SilentlyContinue }

# Remove quaisquer arquivos com padrão de versão antiga
Get-ChildItem "$installerDir\CoinCraftSetup_*.exe" | Remove-Item -Force -Verbose -ErrorAction SilentlyContinue
Get-ChildItem "$installerDir\InstalarCoinCraft_*.exe" | Remove-Item -Force -Verbose -ErrorAction SilentlyContinue
if (Test-Path "$installerDir\CoinCraftSetup.exe") { Remove-Item "$installerDir\CoinCraftSetup.exe" -Force -Verbose }
Get-ChildItem "$installerDir\Output" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse

# 2. Publicação do Projeto .NET
Write-Host "2. Publicando aplicação .NET e unificando estrutura..."
if (Test-Path $publishUnified) { Remove-Item $publishUnified -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publishDirX86 | Out-Null
New-Item -ItemType Directory -Force -Path $publishDirX64 | Out-Null

# Pastas antigas (serão removidas se existirem)
$oldX86 = "$projectRoot\publish_final_x86"
$oldX64 = "$projectRoot\publish_final_x64"

dotnet publish "$projectRoot\src\CoinCraft.App\CoinCraft.App.csproj" -c Release -r win-x86 -o $publishDirX86 /p:DebugType=None /p:DebugSymbols=false /p:PublishReadyToRun=false
dotnet publish "$projectRoot\src\CoinCraft.App\CoinCraft.App.csproj" -c Release -r win-x64 -o $publishDirX64 /p:DebugType=None /p:DebugSymbols=false /p:PublishReadyToRun=false


if (-not (Test-Path "$publishDirX86\CoinCraft.App.exe")) { Write-Error "Falha na publicação x86. Executável não encontrado." }
if (-not (Test-Path "$publishDirX64\CoinCraft.App.exe")) { Write-Error "Falha na publicação x64. Executável não encontrado." }

# Verificar se public.xml foi copiado
foreach ($d in @($publishDirX86, $publishDirX64)) {
    if (-not (Test-Path "$d\public.xml")) {
        Write-Warning "public.xml não encontrado em $d. Copiando manualmente..."
        Copy-Item "$projectRoot\src\CoinCraft.App\public.xml" "$d\public.xml" -Force
    }
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
    Write-Host "   Conteúdo x64: $(Get-ChildItem $publishDirX64 | Measure-Object | Select-Object -ExpandProperty Count) itens"
    Write-Host "   Conteúdo x86: $(Get-ChildItem $publishDirX86 | Measure-Object | Select-Object -ExpandProperty Count) itens"
} else {
    Write-Error "FALHA: O instalador não foi gerado."
}

Write-Host "=== Processo Concluído ===" -ForegroundColor Cyan
