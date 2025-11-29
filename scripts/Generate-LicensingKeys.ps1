$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$adminPublish = Join-Path $root 'admin_publish'
$appPublish = Join-Path $root 'publish'
New-Item -ItemType Directory -Force -Path $adminPublish | Out-Null
New-Item -ItemType Directory -Force -Path $appPublish | Out-Null

Add-Type -AssemblyName System.Security
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider(2048)
$priv = $rsa.ToXmlString($true)
$pub = $rsa.ToXmlString($false)

$privPath = Join-Path $adminPublish 'private.xml'
$pubPath = Join-Path $appPublish 'public.xml'
[IO.File]::WriteAllText($privPath, $priv)
[IO.File]::WriteAllText($pubPath, $pub)

Write-Host "Gerado: $privPath"
Write-Host "Gerado: $pubPath"
