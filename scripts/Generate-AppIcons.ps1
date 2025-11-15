# Requires: Windows PowerShell 5+ with System.Drawing available
param(
    [Parameter(Mandatory=$true)]
    [string]$SourceLogo,

    [string]$OutputAssetsPath = "src/CoinCraft.Package/Assets",
    [string]$WpfProjectPath = "src/CoinCraft.App"
)

function New-SquareImage {
    param(
        [System.Drawing.Image]$Image,
        [int]$Size
    )

    $square = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($square)
    $g.Clear([System.Drawing.Color]::FromArgb(0,0,0,0))
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Preserve aspect ratio: fit inside square and center
    $ratioX = $Size / $Image.Width
    $ratioY = $Size / $Image.Height
    $ratio = [Math]::Min($ratioX, $ratioY)
    $newWidth = [int]([Math]::Round($Image.Width * $ratio))
    $newHeight = [int]([Math]::Round($Image.Height * $ratio))
    $offsetX = [int](($Size - $newWidth) / 2)
    $offsetY = [int](($Size - $newHeight) / 2)

    $destRect = New-Object System.Drawing.Rectangle($offsetX, $offsetY, $newWidth, $newHeight)
    $g.DrawImage($Image, $destRect)
    $g.Dispose()
    return $square
}

function Save-Png {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [string]$Path
    )
    $null = New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($Path))
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
}

if (-not (Test-Path $SourceLogo)) {
    throw "Logo de origem não encontrado: $SourceLogo"
}

# Aviso para SVG
if ([System.IO.Path]::GetExtension($SourceLogo).ToLower() -eq ".svg") {
    throw "O arquivo fornecido é SVG. Converta para PNG (fundo transparente) e reexecute: .\\scripts\\Generate-AppIcons.ps1 -SourceLogo caminho\\logo.png"
}

Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile($SourceLogo)

# Generate required icons for the MSIX manifest
$icon44 = New-SquareImage -Image $img -Size 44
Save-Png -Bitmap $icon44 -Path (Join-Path $OutputAssetsPath 'Square44x44Logo.png')
$icon150 = New-SquareImage -Image $img -Size 150
Save-Png -Bitmap $icon150 -Path (Join-Path $OutputAssetsPath 'Square150x150Logo.png')

# StoreLogo: 50x50 required for DesktopBridge packaging
$store50 = New-SquareImage -Image $img -Size 50
Save-Png -Bitmap $store50 -Path (Join-Path $OutputAssetsPath 'StoreLogo.png')

$img.Dispose()
Write-Host "Ícones gerados em: $OutputAssetsPath" -ForegroundColor Green

# Gerar ícone do executável WPF (Icon.ico) a partir de 256x256
$icon256 = New-SquareImage -Image ([System.Drawing.Image]::FromFile($SourceLogo)) -Size 256
$icoPath = Join-Path $WpfProjectPath 'Icon.ico'

# Convert Bitmap to Icon and save
$hIcon = $icon256.GetHicon()
$iconObj = [System.Drawing.Icon]::FromHandle($hIcon)
$fs = [System.IO.File]::Open($icoPath, [System.IO.FileMode]::Create)
$iconObj.Save($fs)
$fs.Close()
$iconObj.Dispose()
$icon256.Dispose()
Write-Host "Ícone WPF gerado: $icoPath" -ForegroundColor Green