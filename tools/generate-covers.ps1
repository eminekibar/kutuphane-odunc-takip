# books.json'daki her kitap için KutuphaneOdunc/wwwroot/images/<id>.svg kapak görseli üretir.
# Görsel yüklenemezse kullanılacak images/default.svg dosyasını da üretir.
# Çalıştırma (repo kök klasöründe):
#   powershell -ExecutionPolicy Bypass -File tools\generate-covers.ps1

$ErrorActionPreference = "Stop"

$root      = Split-Path -Parent $PSScriptRoot
$booksPath = Join-Path $root "KutuphaneOdunc\wwwroot\data\books.json"
$imagesDir = Join-Path $root "KutuphaneOdunc\wwwroot\images"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

# Kategori -> (zemin rengi, bant rengi)
$colors = @{
    "Bilgisayar Bilimleri" = @("#1E4D8C", "#163A69")
    "Bilim"                = @("#1F7A5C", "#175C45")
    "Edebiyat"             = @("#8C2F4B", "#6A2338")
    "Tarih"                = @("#8A5A1E", "#684417")
    "Felsefe"              = @("#4B3F8C", "#382F69")
}
$defaultColors = @("#5A6270", "#444A55")

function Escape-Xml([string]$text) {
    return [System.Security.SecurityElement]::Escape($text)
}

# Metni kelime bölmeden, satır başına en fazla $max karakter olacak şekilde satırlara ayırır.
function Split-Lines([string]$text, [int]$max) {
    $lines = New-Object System.Collections.Generic.List[string]
    $current = ""
    foreach ($word in ($text -split '\s+')) {
        if ($current.Length -gt 0 -and ($current.Length + 1 + $word.Length) -gt $max) {
            $lines.Add($current)
            $current = $word
        } elseif ($current.Length -gt 0) {
            $current = "$current $word"
        } else {
            $current = $word
        }
    }
    if ($current.Length -gt 0) { $lines.Add($current) }
    return ,$lines
}

function New-CoverSvg([string]$title, [string]$author, [string]$category) {
    if ($colors.ContainsKey($category)) { $c = $colors[$category] } else { $c = $defaultColors }
    $bg = $c[0]; $band = $c[1]

    $lines = Split-Lines $title 16
    if ($lines.Count -le 4) { $fontSize = 26 } else { $fontSize = 22 }
    $lineHeight = $fontSize * 1.25
    $firstY = 225 - ($lines.Count - 1) * $lineHeight / 2

    # Koordinatlar tam sayıya yuvarlanıyor. Türkçe Windows'ta ondalık ayraç virgül
    # olduğu için ondalıklı sayı SVG'ye "225,5" diye yazılır ve dosya bozulur.
    $tspans = ""
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $y = [int][math]::Round($firstY + $i * $lineHeight)
        $tspans += "<tspan x=""150"" y=""$y"">$(Escape-Xml $lines[$i])</tspan>"
    }

    return @"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 450" width="300" height="450">
  <rect width="300" height="450" fill="$bg"/>
  <rect width="300" height="56" fill="$band"/>
  <rect x="18" y="74" width="264" height="302" fill="none" stroke="#FFFFFF" stroke-opacity="0.35" stroke-width="2"/>
  <text x="150" y="35" fill="#FFFFFF" fill-opacity="0.85" font-family="Segoe UI, Arial, sans-serif" font-size="15" text-anchor="middle">$(Escape-Xml $category)</text>
  <text fill="#FFFFFF" font-family="Segoe UI, Arial, sans-serif" font-size="$fontSize" font-weight="700" text-anchor="middle">$tspans</text>
  <rect y="394" width="300" height="56" fill="$band"/>
  <text x="150" y="428" fill="#FFFFFF" font-family="Segoe UI, Arial, sans-serif" font-size="17" text-anchor="middle">$(Escape-Xml $author)</text>
</svg>
"@
}

New-Item -ItemType Directory -Force -Path $imagesDir | Out-Null

$books = Get-Content -Raw -Encoding UTF8 $booksPath | ConvertFrom-Json
$count = 0
foreach ($book in $books) {
    $svg  = New-CoverSvg $book.title $book.author $book.category
    $path = Join-Path $imagesDir "$($book.id).svg"
    [System.IO.File]::WriteAllText($path, $svg, $utf8NoBom)
    $count++
}

$defaultSvg = New-CoverSvg "Kapak görseli yok" "" ""
[System.IO.File]::WriteAllText((Join-Path $imagesDir "default.svg"), $defaultSvg, $utf8NoBom)

Write-Host "$count kapak ve default.svg üretildi: $imagesDir"
