param(
  [string]$InnoSetupCompiler = "",
  [string]$ScriptPath = "installer/CoinCraft.iss",
  [switch]$NoPause
)

powershell -File scripts/Publish-SelfContained.ps1 -NoPause

# Encerrar instância do app antes de publicar/empacotar
try {
  Get-Process -Name 'CoinCraft.App' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 1
} catch {}

$candidates = @(
  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
  "C:\Program Files\Inno Setup 6\ISCC.exe"
)
if ([string]::IsNullOrWhiteSpace($InnoSetupCompiler)) {
  foreach ($c in $candidates) { if (Test-Path $c) { $InnoSetupCompiler = $c; break } }
}
if ([string]::IsNullOrWhiteSpace($InnoSetupCompiler)) {
  Write-Host "Instale o Inno Setup 6 e execute novamente: $ScriptPath" -ForegroundColor Yellow
} else {
  & $InnoSetupCompiler $ScriptPath
  Write-Host "Installer gerado em pasta dist" -ForegroundColor Green
}
if (-not $NoPause) { Read-Host "Pressione Enter para sair" }