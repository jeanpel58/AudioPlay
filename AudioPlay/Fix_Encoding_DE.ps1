# Script pour corriger l'encodage allemand
$content = Get-Content "Resources.de.resx" -Raw -Encoding UTF8

# Corrections d'encodage pour l'allemand
$content = $content -replace 'Ã¤', 'ä'
$content = $content -replace 'Ã¶', 'ö'
$content = $content -replace 'Ã¼', 'ü'
$content = $content -replace 'ÃŸ', 'ß'
$content = $content -replace 'Ã„', 'Ä'
$content = $content -replace 'Ã–', 'Ö'
$content = $content -replace 'Ãœ', 'Ü'

# Corrections générales (au cas où)
$content = $content -replace 'Ã©', 'é'
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
[System.IO.File]::WriteAllText((Resolve-Path "Resources.de.resx").Path, $content, $utf8NoBom)

Write-Host "✅ Resources.de.resx corrigé avec UTF-8"
