# README için kitaplar, kitap detayı ve ödünç kayıtları sayfalarının masaüstü ve telefon
# ekran görüntülerini docs\images klasörüne alır. Playwright'ın komut satırı aracını kullanır.
# Ödünç kayıtları sayfası boş görünmesin diye tarayıcıya örnek kayıtlar ve bir profil yüklenir:
# biri aktif, biri teslimi yaklaşan, biri gecikmiş, biri iade edilmiş.
#
# Çalıştırma (repo kök klasöründe):
#   powershell -ExecutionPolicy Bypass -File tools\ekran-goruntuleri.ps1
# Yerel çalışan uygulama için:
#   powershell -ExecutionPolicy Bypass -File tools\ekran-goruntuleri.ps1 -BaseUrl https://localhost:7001/

param(
    [string]$BaseUrl = "https://eminekibar.github.io/kutuphane-odunc-takip/"
)

$ErrorActionPreference = "Stop"

$root      = Split-Path -Parent $PSScriptRoot
$outDir    = Join-Path $root "docs\images"
$booksPath = Join-Path $root "KutuphaneOdunc\wwwroot\data\books.json"
$cli       = Join-Path $root "KutuphaneOdunc.E2ETests\bin\Debug\net8.0\playwright.ps1"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

if (-not $BaseUrl.EndsWith("/")) { $BaseUrl += "/" }
$origin = ([Uri]$BaseUrl).GetLeftPart([UriPartial]::Authority)

# Playwright'ın komut satırı aracı E2E projesinin derleme çıktısında geliyor.
if (-not (Test-Path $cli)) {
    dotnet build (Join-Path $root "KutuphaneOdunc.E2ETests") | Out-Host
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# --- Örnek veri: tarihler bugüne göre hesaplanır ---------------------------------------------
$today = (Get-Date).Date
function Day([int]$offset) { $today.AddDays($offset).ToString("yyyy-MM-ddT00:00:00") }
$year = $today.Year

$loans = @(
    [ordered]@{ Id = "LIB-$year-101"; BookId = "B001"; BorrowerName = "Ayşe Yılmaz";  Email = "ayse@ornek.com";
                BorrowDate = (Day (-4));  DueDate = (Day 10); ReturnedAt = $null;    IsExtended = $false },
    [ordered]@{ Id = "LIB-$year-102"; BookId = "B005"; BorrowerName = "Ayşe Yılmaz";  Email = "ayse@ornek.com";
                BorrowDate = (Day (-12)); DueDate = (Day 2);   ReturnedAt = $null;    IsExtended = $false },
    [ordered]@{ Id = "LIB-$year-103"; BookId = "B003"; BorrowerName = "Mehmet Demir"; Email = "mehmet@ornek.com";
                BorrowDate = (Day (-17)); DueDate = (Day (-3)); ReturnedAt = $null;    IsExtended = $false },
    [ordered]@{ Id = "LIB-$year-104"; BookId = "B010"; BorrowerName = "Ayşe Yılmaz";  Email = "ayse@ornek.com";
                BorrowDate = (Day (-20)); DueDate = (Day (-6)); ReturnedAt = (Day (-8)); IsExtended = $false }
)

# Kopya sayıları: books.json'daki değerden iade edilmemiş kayıtlar düşülür.
$books = [IO.File]::ReadAllText($booksPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
$availability = [ordered]@{}
foreach ($book in $books) {
    $active = @($loans | Where-Object { $_.BookId -eq $book.id -and $null -eq $_.ReturnedAt }).Count
    $availability[$book.id] = [Math]::Max(0, $book.availableCopies - $active)
}

$userProfile = [ordered]@{ FullName = "Ayşe Yılmaz"; Email = "ayse@ornek.com" }

# localStorage'daki değerler metin olarak saklanıyor; uygulamanın yazdığı biçimle aynı JSON.
$state = [ordered]@{
    cookies = @()
    origins = @(
        [ordered]@{
            origin       = $origin
            localStorage = @(
                [ordered]@{ name = "kutuphane.loans";        value = (ConvertTo-Json -InputObject $loans -Compress -Depth 5) },
                [ordered]@{ name = "kutuphane.availability"; value = (ConvertTo-Json -InputObject $availability -Compress) },
                [ordered]@{ name = "kutuphane.profile";      value = (ConvertTo-Json -InputObject $userProfile -Compress) }
            )
        }
    )
}

# Playwright dosyayı BOM'suz UTF-8 bekliyor.
$statePath = Join-Path $env:TEMP "kutuphane-ekran-durum.json"
[IO.File]::WriteAllText($statePath, (ConvertTo-Json -InputObject $state -Depth 6), $utf8NoBom)

# --- Ekran görüntüleri -----------------------------------------------------------------------
$pages = @(
    @{ Name = "kitaplar";         Path = "books";      WaitFor = ".book-card" },
    @{ Name = "kitap-detay";      Path = "books/B001"; WaitFor = ".similar-books" },
    @{ Name = "odunc-kayitlarim"; Path = "my-loans";   WaitFor = ".loan-summary" }
)

$sizes = @(
    @{ Name = "masaustu"; Viewport = "1280,800" },
    @{ Name = "telefon";  Viewport = "390,844" }
)

foreach ($page in $pages) {
    foreach ($size in $sizes) {
        $file = Join-Path $outDir "$($page.Name)-$($size.Name).png"
        Write-Host "$($page.Path) ($($size.Name)) -> $file"

        & $cli screenshot `
            --browser chromium `
            --viewport-size $size.Viewport `
            --load-storage $statePath `
            --wait-for-selector $page.WaitFor `
            --wait-for-timeout 500 `
            ($BaseUrl + $page.Path) $file

        if ($LASTEXITCODE -ne 0) { throw "Ekran görüntüsü alınamadı: $($page.Path)" }
    }
}

Remove-Item $statePath
Write-Host "Bitti: $outDir"
