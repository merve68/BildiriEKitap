$root = Join-Path (Split-Path $PSScriptRoot -Parent) "samples"
New-Item -ItemType Directory -Force -Path $root | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem

function New-Docx {
    param($Path, $Title, $Body)
    if (Test-Path $Path) { Remove-Item $Path -Force }
    $zip = [System.IO.Compression.ZipFile]::Open($Path, 'Create')
    try {
        $entries = @{
            "[Content_Types].xml" = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>'
            "_rels/.rels" = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>'
        }
        $entries["word/document.xml"] = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    <w:p><w:pPr><w:pStyle w:val="Heading1"/></w:pPr><w:r><w:t>$Title</w:t></w:r></w:p>
    <w:p><w:r><w:t>$Body</w:t></w:r></w:p>
    <w:p><w:r><w:t>Iletisim: Info@akap.tr ve 0312 343 10 33. Adres Yildizevler Mah. 708. Cad. No:14 kalsin. Yil 2026.</w:t></w:r></w:p>
  </w:body>
</w:document>
"@
        foreach ($name in $entries.Keys) {
            $entry = $zip.CreateEntry($name)
            $stream = $entry.Open()
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($entries[$name])
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Close()
        }
    }
    finally { $zip.Dispose() }
}

1..10 | ForEach-Object {
    New-Docx (Join-Path $root ("bildiri{0:00}.docx" -f $_)) "Bildiri $_ Basligi" "Bu bildirinin govde metnidir. Sempozyum katkisi $_."
}

$curlArgs = @('-s', '-F', 'ad=Sempozyum 2026')
Get-ChildItem $root -Filter *.docx | ForEach-Object {
    $curlArgs += '-F'
    $curlArgs += "files=@$($_.FullName)"
}
$curlArgs += 'http://localhost:5141/api/kitaplar'
Write-Output ("files=" + (Get-ChildItem $root -Filter *.docx).Count)
$createdRaw = & curl.exe @curlArgs
Write-Output $createdRaw
$created = $createdRaw | ConvertFrom-Json
if (-not $created.id) { exit 1 }
curl.exe -s -X POST "http://localhost:5141/api/kitaplar/$($created.id)/olustur" | Out-Null
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 1
    $status = curl.exe -s "http://localhost:5141/api/kitaplar/$($created.id)" | ConvertFrom-Json
    Write-Output "$($status.durum) $($status.asama) $($status.hataMesaji)"
    if ($status.durum -eq 'Tamamlandi' -or $status.durum -eq 'Failed') {
        $status | ConvertTo-Json -Depth 4
        curl.exe -s -o (Join-Path $root "kitap.pdf") "http://localhost:5141/api/kitaplar/$($created.id)/pdf"
        Get-Item (Join-Path $root "kitap.pdf") | Select-Object FullName, Length
        break
    }
}
