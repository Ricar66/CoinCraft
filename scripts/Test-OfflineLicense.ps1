param(
  [Parameter(Mandatory=$true)][string]$Fingerprint,
  [Parameter(Mandatory=$true)][string]$LicenseBase64,
  [string]$PublicXmlPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'publish\public.xml')
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $PublicXmlPath)) { Write-Error "PublicXmlPath não encontrado: $PublicXmlPath"; exit 1 }

$pubText = Get-Content -Raw -Path $PublicXmlPath
$sig = [Convert]::FromBase64String($LicenseBase64)
$data = [Text.Encoding]::UTF8.GetBytes($Fingerprint)

Add-Type -AssemblyName System.Security
try {
  $rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
  $rsa.FromXmlString($pubText)
  $ok = $rsa.VerifyData($data, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
  Write-Output ("Resultado: " + ($ok ? 'Válido' : 'Inválido'))
} catch {
  Write-Error $_.Exception.Message
  exit 1
}
