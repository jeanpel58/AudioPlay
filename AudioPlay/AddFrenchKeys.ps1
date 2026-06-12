# Script pour ajouter les clés manquantes au fichier Resources.resx

$file = "Resources.resx"
$keys1 = Get-Content "FRANCAIS_KEYS_TO_ADD.txt" -Raw
$keys2 = Get-Content "FRANCAIS_DJ_KEYS.txt" -Raw

$content = Get-Content $file -Raw

# Ajouter </root> si manquant
if (-not $content.EndsWith("</root>")) {
	$content += "`r`n</root>"
}

# Insérer avant </root>
$newContent = $content.Replace('</root>', $keys1 + "`r`n" + $keys2 + "`r`n</root>")

# Sauvegarder
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($file, $newContent, $utf8NoBom)

Write-Host "Terminé - $(($keys1 + $keys2) -split '<data name=' | Measure-Object).Count clés ajoutées"
