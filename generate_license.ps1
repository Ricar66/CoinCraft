param(
    [Parameter(Mandatory=$true)]
    [string]$Fingerprint
)

$privateKeyPath = Join-Path $PSScriptRoot "src\CoinCraft.App\private.xml"
if (-not (Test-Path $privateKeyPath)) {
    Write-Error "Private key not found at $privateKeyPath"
    exit 1
}

$keyText = Get-Content $privateKeyPath -Raw
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
$rsa.FromXmlString($keyText)

$data = [System.Text.Encoding]::UTF8.GetBytes($Fingerprint)
$sig = $rsa.SignData($data, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
$base64 = [Convert]::ToBase64String($sig)

Write-Host "License Key for Fingerprint '$Fingerprint':"
Write-Host $base64 -ForegroundColor Green
