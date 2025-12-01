$ErrorActionPreference = "Stop"

Write-Host "Buildando a solução..." -ForegroundColor Cyan
dotnet build CoinCraft.sln -c Debug

Write-Host "Executando testes..." -ForegroundColor Cyan
$testResult = dotnet test src/CoinCraft.Tests/CoinCraft.Tests.csproj --collect:"XPlat Code Coverage" --results-directory ./tests/results

if ($LASTEXITCODE -eq 0) {
    Write-Host "Todos os testes passaram com sucesso!" -ForegroundColor Green
} else {
    Write-Host "Alguns testes falharam." -ForegroundColor Red
    exit 1
}

# Opcional: Gerar relatório de cobertura se 'reportgenerator' estiver instalado
# reportgenerator -reports:tests/results/**/coverage.cobertura.xml -targetdir:tests/coverage-report
