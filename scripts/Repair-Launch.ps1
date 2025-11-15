Param(
  [Parameter(Mandatory=$false)] [string]$IdentityName = 'CoinCraft'
)

Write-Host '== CoinCraft Repair Launch ==' -ForegroundColor Cyan
$ErrorActionPreference = 'Stop'

try {
  $pkg = Get-AppxPackage -Name $IdentityName -ErrorAction SilentlyContinue
  if (-not $pkg) { $pkg = Get-AppxPackage | Where-Object { $_.Name -eq $IdentityName } }
  if (-not $pkg) { throw "Pacote '$IdentityName' não encontrado." }

  $pfn = $pkg.PackageFamilyName
  Write-Host ("PFN: " + $pfn) -ForegroundColor Gray
  $candidateIds = @('CoinCraft','App','Main')
  foreach ($appId in $candidateIds) {
    try {
      $target = ('shell:AppsFolder/{0}{1}{2}' -f $pfn, [char]33, $appId)
      Write-Host ("Tentando abrir: " + $target) -ForegroundColor Yellow
      Start-Process explorer.exe $target
      Write-Host ("OK: " + $appId) -ForegroundColor Green
      return
    } catch {}
  }
  Write-Host 'Não foi possível abrir com IDs padrões. Coletando AppxLog...' -ForegroundColor DarkYellow
  try {
    $log = Get-AppxLog -ActivityId 00000000-0000-0000-0000-000000000000 | Select-Object -Last 40
    $log | Format-Table -AutoSize
  } catch {}
  Write-Host 'Dica: verifique o Id do <Application> no manifest e tente novamente.' -ForegroundColor DarkYellow
} catch {
  Write-Host ("Erro: " + $_.Exception.Message) -ForegroundColor Red
  exit 1
}