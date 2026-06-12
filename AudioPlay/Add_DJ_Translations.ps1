# Script pour ajouter les traductions du mode DJ dans les fichiers .resx
# À exécuter après avoir fermé Visual Studio

Write-Host "=== Ajout des traductions du mode DJ ===" -ForegroundColor Cyan

# Fonction pour ajouter les traductions avant </root>
function Add-Translations {
	param(
		[string]$FilePath,
		[string]$TranslationsFile
	)

	if (-not (Test-Path $FilePath)) {
		Write-Host "Fichier non trouvé: $FilePath" -ForegroundColor Red
		return
	}

	if (-not (Test-Path $TranslationsFile)) {
		Write-Host "Fichier de traductions non trouvé: $TranslationsFile" -ForegroundColor Red
		return
	}

	Write-Host "Traitement de $FilePath..." -ForegroundColor Yellow

	try {
		$content = Get-Content $FilePath -Raw -Encoding UTF8
		$translations = Get-Content $TranslationsFile -Raw -Encoding UTF8

		# Vérifier si les traductions sont déjà présentes
		if ($content -match "DJ_SyncToA") {
			Write-Host "  ✓ Les traductions sont déjà présentes" -ForegroundColor Green
			return
		}

		# Ajouter les traductions avant </root>
		$content = $content -replace '</root>', "$translations`n</root>"

		# Sauvegarder avec BOM UTF-8
		[System.IO.File]::WriteAllText($FilePath, $content, (New-Object System.Text.UTF8Encoding $true))

		Write-Host "  ✓ Traductions ajoutées avec succès" -ForegroundColor Green
	}
	catch {
		Write-Host "  ✗ Erreur: $_" -ForegroundColor Red
	}
}

# Traiter chaque fichier
Add-Translations -FilePath "AudioPlay\Resources.es.resx" -TranslationsFile "AudioPlay\DJ_TRANSLATIONS_ES.txt"
Add-Translations -FilePath "AudioPlay\Resources.de.resx" -TranslationsFile "AudioPlay\DJ_TRANSLATIONS_DE.txt"
Add-Translations -FilePath "AudioPlay\Resources.it.resx" -TranslationsFile "AudioPlay\DJ_TRANSLATIONS_IT.txt"

Write-Host "`n=== Terminé ===" -ForegroundColor Cyan
Write-Host "Les traductions ES, DE et IT ont été ajoutées."
Write-Host "Les traductions FR et EN ont déjà été ajoutées directement." -ForegroundColor Green
Write-Host "`nVous pouvez maintenant:"
Write-Host "1. Supprimer les fichiers DJ_TRANSLATIONS_*.txt"
Write-Host "2. Rouvrir le projet dans Visual Studio"
Write-Host "3. Recompiler le projet" -ForegroundColor Yellow
