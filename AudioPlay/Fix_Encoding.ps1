# Script pour corriger l'encodage des noms de fichiers dans Resources.resx
$content = Get-Content "Resources.resx" -Raw -Encoding UTF8

# Corrections d'encodage
$content = $content -replace 'AudioPlay_MÃ©tadonnÃ©es__', 'AudioPlay_Métadonnées__'
$content = $content -replace 'AudioPlay_GÃ©rerListe', 'AudioPlay_GérerListe'
$content = $content -replace 'Ã©', 'é'
$content = $content -replace 'Ã ', 'à'
$content = $content -replace 'Ã¨', 'è'
$content = $content -replace 'Ã´', 'ô'
$content = $content -replace 'Ã®', 'î'
$content = $content -replace 'Ã»', 'û'
$content = $content -replace 'Ã§', 'ç'

# Sauvegarder avec le bon encodage
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Resolve-Path "Resources.resx").Path, $content, $utf8NoBom)

Write-Host "✅ Resources.resx corrigé avec UTF-8"
