param(
  [string]$ProjectPath = "src/CoinCraft.App/CoinCraft.App.csproj",
  [string]$Runtime = "win-x64",
  [string]$Configuration = "Release",
  [string]$Output = "publish",
  [switch]$NoPause
)

$outDir = Join-Path (Split-Path $ProjectPath -Parent) $Output
try {
  Get-Process -Name 'CoinCraft.App' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  Start-Sleep -Milliseconds 500
} catch {}
dotnet publish $ProjectPath -c $Configuration -r $Runtime --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true -o $outDir
Write-Host "Publicado em: $outDir" -ForegroundColor Green
if (-not $NoPause) { Read-Host "Pressione Enter para sair" }