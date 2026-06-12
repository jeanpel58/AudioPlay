# Script pour corriger l'encodage espagnol
$content = Get-Content "Resources.es.resx" -Raw -Encoding UTF8

# Corrections d'encodage pour l'espagnol
$content = $content -replace 'Ã³', 'ó'
$content = $content -replace 'Ã©', 'é'
$content = $content -replace 'Ã­', 'í'
$content = $content -replace 'Ã¡', 'á'
$content = $content -replace 'Ãº', 'ú'
$content = $content -replace 'Ã±', 'ñ'
$content = $content -replace 'Â¡', '¡'
$content = $content -replace 'Â¿', '¿'
$content = $content -replace 'Ã'', 'Ñ'
$content = $content -replace 'Ã"', 'Ó'
$content = $content -replace 'Ã‰', 'É'
$content = $content -replace 'Ãš', 'Ú'
$content = $content -replace 'Ã', 'Á'
$content = $content -replace 'Ã', 'Í'
$content = $content -replace 'Ã¨', 'è'
$content = $content -replace 'Ãª', 'ê'
$content = $content -replace 'Ã ', 'à'
$content = $content -replace 'Ã´', 'ô'
$content = $content -replace 'Ã®', 'î'
$content = $content -replace 'Ã»', 'û'
$content = $content -replace 'Ã§', 'ç'
$content = $content -replace 'Ã¢', 'â'

# Sauvegarder avec le bon encodage
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Resolve-Path "Resources.es.resx").Path, $content, $utf8NoBom)

Write-Host "✅ Resources.es.resx corrigé avec UTF-8"
