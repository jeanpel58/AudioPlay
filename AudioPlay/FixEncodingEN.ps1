$filePath = "Resources.en.resx"
$content = Get-Content $filePath -Raw -Encoding UTF8

Write-Host "Correction des caractères mal encodés dans $filePath..."

# Compter les occurrences avant
$countBefore = ([regex]::Matches($content, "â€¢|â—|â–|â—€|â–¶")).Count
Write-Host "Caractères mal encodés trouvés: $countBefore"

# Remplacements
$content = $content -replace 'â€¢', '•'  # Puce
$content = $content -replace 'â—', '●'   # Cercle plein (REC)
$content = $content -replace 'â– ', '■ ' # Carré plein (STOP) avec espace
$content = $content -replace 'â—€', '◀'  # Flèche gauche
$content = $content -replace 'â–¶', '▶'  # Flèche droite (play)

# Compter après
$countAfter = ([regex]::Matches($content, "â€¢|â—|â–|â—€|â–¶")).Count
Write-Host "Caractères mal encodés restants: $countAfter"

# Sauvegarder avec UTF-8 BOM
$utf8Bom = New-Object System.Text.UTF8Encoding $true
[System.IO.File]::WriteAllText($filePath, $content, $utf8Bom)

Write-Host "✓ Correction terminée pour $filePath"
