# Script de vérification de la configuration des effets audio
# Usage: .\Check-AudioEffects.ps1

$configPath = Join-Path $env:APPDATA "AudioPlay\parametres.txt"

Write-Host "`n=================================================" -ForegroundColor Cyan
Write-Host "  VÉRIFICATION CONFIGURATION EFFETS AUDIO" -ForegroundColor Cyan
Write-Host "=================================================`n" -ForegroundColor Cyan

if (Test-Path $configPath) {
	Write-Host "✅ Fichier de configuration trouvé" -ForegroundColor Green
	Write-Host "   📂 $configPath`n" -ForegroundColor Gray

	$content = Get-Content $configPath

	# Paramètres généraux
	Write-Host "=== PARAMÈTRES GÉNÉRAUX ===" -ForegroundColor Yellow
	$content | Where-Object { $_ -notlike "*Effet*" } | ForEach-Object {
		Write-Host "  $_" -ForegroundColor White
	}

	Write-Host "`n=== EFFETS AUDIO ===" -ForegroundColor Yellow
	$effetsLignes = $content | Where-Object { $_ -like "*Effet*" }

	if ($effetsLignes) {
		# Reverb
		Write-Host "`n🎵 REVERB:" -ForegroundColor Magenta
		$reverbActif = $content | Where-Object { $_ -like "EffetReverbActif=*" }
		$reverbMix = $content | Where-Object { $_ -like "EffetReverbMix=*" }
		if ($reverbActif) {
			$actif = $reverbActif.Split('=')[1]
			if ($actif -eq "True") {
				Write-Host "   ✅ Actif" -ForegroundColor Green
			} else {
				Write-Host "   ❌ Inactif" -ForegroundColor Red
			}
		} else {
			Write-Host "   ⚠️  Non configuré" -ForegroundColor DarkYellow
		}
		if ($reverbMix) {
			$mix = [math]::Round([double]$reverbMix.Split('=')[1] * 100)
			Write-Host "   Mix: $mix%" -ForegroundColor White
		}

		# Echo
		Write-Host "`n🔊 ECHO:" -ForegroundColor Magenta
		$echoActif = $content | Where-Object { $_ -like "EffetEchoActif=*" }
		$echoMix = $content | Where-Object { $_ -like "EffetEchoMix=*" }
		$echoDelai = $content | Where-Object { $_ -like "EffetEchoDelai=*" }
		$echoFeedback = $content | Where-Object { $_ -like "EffetEchoFeedback=*" }

		if ($echoActif) {
			$actif = $echoActif.Split('=')[1]
			if ($actif -eq "True") {
				Write-Host "   ✅ Actif" -ForegroundColor Green
			} else {
				Write-Host "   ❌ Inactif" -ForegroundColor Red
			}
		} else {
			Write-Host "   ⚠️  Non configuré" -ForegroundColor DarkYellow
		}
		if ($echoMix) {
			$mix = [math]::Round([double]$echoMix.Split('=')[1] * 100)
			Write-Host "   Mix: $mix%" -ForegroundColor White
		}
		if ($echoDelai) {
			$delai = $echoDelai.Split('=')[1]
			Write-Host "   Délai: $delai ms" -ForegroundColor White
		}
		if ($echoFeedback) {
			$fb = [math]::Round([double]$echoFeedback.Split('=')[1] * 100)
			Write-Host "   Feedback: $fb%" -ForegroundColor White
		}

		# Pitch Shift
		Write-Host "`n🎹 PITCH SHIFT:" -ForegroundColor Magenta
		$pitchActif = $content | Where-Object { $_ -like "EffetPitchActif=*" }
		$pitchSemitones = $content | Where-Object { $_ -like "EffetPitchSemitones=*" }

		if ($pitchActif) {
			$actif = $pitchActif.Split('=')[1]
			if ($actif -eq "True") {
				Write-Host "   ✅ Actif" -ForegroundColor Green
			} else {
				Write-Host "   ❌ Inactif" -ForegroundColor Red
			}
		} else {
			Write-Host "   ⚠️  Non configuré" -ForegroundColor DarkYellow
		}
		if ($pitchSemitones) {
			$st = $pitchSemitones.Split('=')[1]
			if ([double]$st -gt 0) {
				Write-Host "   Semitones: +$st" -ForegroundColor White
			} else {
				Write-Host "   Semitones: $st" -ForegroundColor White
			}
		}

		# Time Stretch
		Write-Host "`n⏱️  TIME STRETCH:" -ForegroundColor Magenta
		$timeActif = $content | Where-Object { $_ -like "EffetTimeStretchActif=*" }
		$timeRatio = $content | Where-Object { $_ -like "EffetTimeStretchRatio=*" }

		if ($timeActif) {
			$actif = $timeActif.Split('=')[1]
			if ($actif -eq "True") {
				Write-Host "   ✅ Actif" -ForegroundColor Green
			} else {
				Write-Host "   ❌ Inactif" -ForegroundColor Red
			}
		} else {
			Write-Host "   ⚠️  Non configuré" -ForegroundColor DarkYellow
		}
		if ($timeRatio) {
			$ratio = $timeRatio.Split('=')[1]
			Write-Host "   Ratio: ${ratio}x" -ForegroundColor White
		}

		Write-Host "`n=== LIGNES BRUTES ===" -ForegroundColor Yellow
		$effetsLignes | ForEach-Object {
			Write-Host "  $_" -ForegroundColor Cyan
		}

	} else {
		Write-Host "  ⚠️  AUCUN EFFET CONFIGURÉ" -ForegroundColor Red
		Write-Host "  Les lignes 'EffetXXX=' sont manquantes dans le fichier." -ForegroundColor Yellow
		Write-Host "  Veuillez ouvrir FormParametres et sauvegarder pour initialiser les effets." -ForegroundColor Yellow
	}

} else {
	Write-Host "❌ Fichier de configuration NON TROUVÉ" -ForegroundColor Red
	Write-Host "   📂 Chemin attendu: $configPath" -ForegroundColor Gray
	Write-Host "   Veuillez lancer AudioPlay au moins une fois." -ForegroundColor Yellow
}

Write-Host "`n=================================================`n" -ForegroundColor Cyan
