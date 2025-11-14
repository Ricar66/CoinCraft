Param(
  [Parameter(Mandatory=$false)] [string]$MsixPath = '.\\CoinCraft_x64.msix',
  [Parameter(Mandatory=$false)] [string]$CertPath = '.\\CoinCraft_dev_public.cer'
)

Write-Host '== CoinCraft Installer ==' -ForegroundColor Cyan
$ErrorActionPreference = 'Stop'

# Resolve paths to absolute
$MsixPath = [System.IO.Path]::GetFullPath($MsixPath)
$CertPath = [System.IO.Path]::GetFullPath($CertPath)
Write-Host "MSIX: $MsixPath"
Write-Host "CER : $CertPath"

if (-not (Test-Path $CertPath)) { throw "Certificado público não encontrado: $CertPath" }
if (-not (Test-Path $MsixPath)) { throw "Pacote MSIX não encontrado: $MsixPath" }

# Import certificate into TrustedPeople and Root (CurrentUser)
Write-Host 'Importando certificado em TrustedPeople...' -ForegroundColor Yellow
Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
Write-Host 'Importando certificado em Root...' -ForegroundColor Yellow
Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null

# Install MSIX
Write-Host 'Instalando MSIX...' -ForegroundColor Yellow
Add-AppxPackage -Path $MsixPath

Write-Host 'Instalação concluída.' -ForegroundColor Green
Write-Host 'Se houver erro de arquitetura, tente com CoinCraft_x86.msix.' -ForegroundColor DarkGray