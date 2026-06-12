# Script pour corriger l'encodage italien
$content = Get-Content "Resources.it.resx" -Raw -Encoding UTF8

# Corrections d'encodage pour l'italien
$content = $content -replace 'Ã ', 'à'
$content = $content -replace 'Ã¨', 'è'
$content = $content -replace 'Ã©', 'é'
$content = $content -replace 'Ã¬', 'ì'
$content = $content -replace 'Ã²', 'ò'
$content = $content -replace 'Ã¹', 'ù'
$content = $content -replace 'Ã€', 'À'
$content = $content -replace 'Ãˆ', 'È'
$content = $content -replace 'Ã‰', 'É'
$content = $content -replace 'ÃŒ', 'Ì'
$content = $content -replace 'Ã'', 'Ò'
$content = $content -replace 'Ã™', 'Ù'

# Corrections générales
$content = $content -replace 'Ãª', 'ê'
$content = $content -replace 'Ã´', 'ô'
$content = $content -replace 'Ã®', 'î'
$content = $content -replace 'Ã»', 'û'
$content = $content -replace 'Ã§', 'ç'
$content = $content -replace 'Ã¢', 'â'

# Sauvegarder avec le bon encodage
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Resolve-Path "Resources.it.resx").Path, $content, $utf8NoBom)

Write-Host "✅ Resources.it.resx corrigé avec UTF-8"
