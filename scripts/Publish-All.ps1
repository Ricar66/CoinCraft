$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$appProj = Join-Path $root 'src\CoinCraft.App\CoinCraft.App.csproj'
$adminProj = Join-Path $root 'src\CoinCraft.Admin\CoinCraft.Admin.csproj'
$appOut = Join-Path $root 'publish'
$adminOut = Join-Path $root 'admin_publish'
dotnet publish $appProj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $appOut | Out-Host
dotnet publish $adminProj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o $adminOut | Out-Host
$pubXml = Join-Path $appOut 'public.xml'
$pubPem = Join-Path $appOut 'public.pem'
if (-not (Test-Path $pubXml) -and (Test-Path (Join-Path $root 'publish\public.xml'))) { Copy-Item (Join-Path $root 'publish\public.xml') $pubXml -Force }
if (-not (Test-Path $pubPem) -and (Test-Path (Join-Path $root 'publish\public.pem'))) { Copy-Item (Join-Path $root 'publish\public.pem') $pubPem -Force }
$isccPaths = @('C:\Program Files (x86)\Inno Setup 6\ISCC.exe','C:\Program Files\Inno Setup 6\ISCC.exe')
$iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($iscc) { & $iscc (Join-Path $root 'installer\CoinCraft.iss') /Q | Out-Host }
Write-Output "App: $appOut"
Write-Output "Admin: $adminOut"
