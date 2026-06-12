# Script de Test Time Stretch - Validation Post-Correction

Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        Test Time Stretch - Vérification Bug Corrigé         ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Chemins
$basePath = "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-01"
$exePath = Join-Path $basePath "AudioPlay\bin\Debug\net8.0-windows\AudioPlay.exe"
$soundTouchDll = Join-Path $basePath "AudioPlay\bin\Debug\net8.0-windows\SoundTouch.dll"

# ═══════════════════════════════════════════════════════════════
# 1. Vérifications Préliminaires
# ═══════════════════════════════════════════════════════════════
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[1] Vérifications préliminaires..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

if (Test-Path $exePath) {
	Write-Host "✓ AudioPlay.exe trouvé" -ForegroundColor Green
} else {
	Write-Host "✗ AudioPlay.exe manquant - Compilez d'abord!" -ForegroundColor Red
	exit 1
}

if (Test-Path $soundTouchDll) {
	$dllInfo = Get-Item $soundTouchDll
	Write-Host "✓ SoundTouch.dll trouvé ($($dllInfo.Length) bytes)" -ForegroundColor Green
} else {
	Write-Host "⚠ SoundTouch.dll manquant - Time Stretch fonctionnera en mode bypass" -ForegroundColor Yellow
}

# ═══════════════════════════════════════════════════════════════
# 2. Vérification du Code Corrigé
# ═══════════════════════════════════════════════════════════════
Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[2] Vérification des corrections dans le code source..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$timeStretchPath = Join-Path $basePath "AudioPlay\AudioEffects\TimeStretchSampleProvider.vb"
if (Test-Path $timeStretchPath) {
	$content = Get-Content $timeStretchPath -Raw

	# Vérifier division entière
	if ($content -match 'samplesRead \\ WaveFormat\.Channels') {
		Write-Host "✓ Division entière (\) utilisée correctement" -ForegroundColor Green
	} else {
		Write-Host "⚠ Division entière non trouvée (possible régression)" -ForegroundColor Yellow
	}

	# Vérifier buffer de sortie agrandi
	if ($content -match 'New Single\(16383\)') {
		Write-Host "✓ Buffer de sortie agrandi (16384 samples)" -ForegroundColor Green
	} else {
		Write-Host "⚠ Buffer de sortie peut être trop petit" -ForegroundColor Yellow
	}

	# Vérifier vérification des limites
	if ($content -match 'outputBufferOffset \+ samplesToCopy <= outputBuffer\.Length') {
		Write-Host "✓ Vérification des limites de buffer présente" -ForegroundColor Green
	} else {
		Write-Host "⚠ Vérification des limites non trouvée" -ForegroundColor Yellow
	}

	# Vérifier Try/Catch
	if ($content -match 'Try\s+While.*Catch ex As Exception') {
		Write-Host "✓ Gestion d'exception Try/Catch présente" -ForegroundColor Green
	} else {
		Write-Host "⚠ Try/Catch non trouvé" -ForegroundColor Yellow
	}
} else {
	Write-Host "✗ TimeStretchSampleProvider.vb non trouvé" -ForegroundColor Red
}

# ═══════════════════════════════════════════════════════════════
# 3. Instructions de Test Manuel
# ═══════════════════════════════════════════════════════════════
Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[3] Instructions de test manuel..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host ""
Write-Host "Tests à effectuer dans AudioPlay:" -ForegroundColor White
Write-Host ""

Write-Host "  Test 1: Tempo Normal" -ForegroundColor Yellow
Write-Host "    1. Lancer AudioPlay" -ForegroundColor Gray
Write-Host "    2. Ouvrir un fichier audio" -ForegroundColor Gray
Write-Host "    3. Paramètres → Effets Audio → Time Stretch" -ForegroundColor Gray
Write-Host "    4. Slider à 1.0 (milieu)" -ForegroundColor Gray
Write-Host "    5. Jouer la musique" -ForegroundColor Gray
Write-Host "    ✓ Résultat: Audio normal, aucun effet" -ForegroundColor Green
Write-Host ""

Write-Host "  Test 2: Tempo Ralenti (BUG ÉTAIT ICI)" -ForegroundColor Yellow
Write-Host "    1. Slider Time Stretch à 0.5" -ForegroundColor Gray
Write-Host "    2. Jouer la musique" -ForegroundColor Gray
Write-Host "    ✓ Résultat: Audio 2x plus lent, SANS crash !" -ForegroundColor Green
Write-Host "    ✓ Pitch de la voix préservé" -ForegroundColor Green
Write-Host ""

Write-Host "  Test 3: Tempo Accéléré" -ForegroundColor Yellow
Write-Host "    1. Slider Time Stretch à 2.0" -ForegroundColor Gray
Write-Host "    2. Jouer la musique" -ForegroundColor Gray
Write-Host "    ✓ Résultat: Audio 2x plus rapide, SANS crash !" -ForegroundColor Green
Write-Host "    ✓ Pitch de la voix préservé" -ForegroundColor Green
Write-Host ""

Write-Host "  Test 4: Valeurs Intermédiaires" -ForegroundColor Yellow
Write-Host "    1. Tester: 0.75, 1.25, 1.5" -ForegroundColor Gray
Write-Host "    ✓ Résultat: Toutes les valeurs fonctionnent" -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 4. Vérification des Logs (si lancé depuis VS)
# ═══════════════════════════════════════════════════════════════
Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[4] Logs de débogage (fenêtre Output dans Visual Studio)..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host ""
Write-Host "Logs attendus SI succès:" -ForegroundColor White
Write-Host "  • 'SoundTouch chargé depuis: ...'" -ForegroundColor Green
Write-Host "  • 'SoundTouch: instance créée avec handle ...'" -ForegroundColor Green
Write-Host "  • 'SoundTouch natif initialisé avec succès'" -ForegroundColor Green
Write-Host ""

Write-Host "Logs attendus SI fallback (DLL non trouvée):" -ForegroundColor White
Write-Host "  • 'SoundTouch DLL non trouvée: ...'" -ForegroundColor Yellow
Write-Host "  • 'Time Stretch désactivé - fallback en mode bypass'" -ForegroundColor Yellow
Write-Host "  • L'audio joue normalement sans effet" -ForegroundColor Yellow
Write-Host ""

Write-Host "Logs attendus en cas d'erreur (ne devrait PAS apparaître):" -ForegroundColor White
Write-Host "  • 'Erreur TimeStretch Read: ...'" -ForegroundColor Red
Write-Host "  • 'Dépassement buffer détecté'" -ForegroundColor Red
Write-Host "  • 'Erreur ProcessMoreSamples: ...'" -ForegroundColor Red
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 5. Checklist Finale
# ═══════════════════════════════════════════════════════════════
Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    CHECKLIST DE TEST                          ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Host "Avant de valider la correction, vérifiez:" -ForegroundColor White
Write-Host ""
Write-Host "  [ ] Build réussie sans erreurs" -ForegroundColor Gray
Write-Host "  [ ] SoundTouch.dll présent dans bin\" -ForegroundColor Gray
Write-Host "  [ ] AudioPlay se lance sans crash" -ForegroundColor Gray
Write-Host "  [ ] Time Stretch activable dans Paramètres" -ForegroundColor Gray
Write-Host "  [ ] Tempo 0.5x fonctionne (ancien crash)" -ForegroundColor Gray
Write-Host "  [ ] Tempo 2.0x fonctionne" -ForegroundColor Gray
Write-Host "  [ ] Qualité audio acceptable (comme Audacity)" -ForegroundColor Gray
Write-Host "  [ ] Aucun message d'erreur dans Output" -ForegroundColor Gray
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 6. Lancement Optionnel
# ═══════════════════════════════════════════════════════════════
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Voulez-vous lancer AudioPlay maintenant pour tester?" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tapez 'O' pour Oui, 'N' pour Non: " -NoNewline -ForegroundColor Yellow

$response = Read-Host

if ($response -eq 'O' -or $response -eq 'o') {
	Write-Host ""
	Write-Host "Lancement d'AudioPlay..." -ForegroundColor Green
	Write-Host "Note: Lancez depuis Visual Studio (F5) pour voir les logs dans Output!" -ForegroundColor Yellow
	Write-Host ""

	if (Test-Path $exePath) {
		Start-Process $exePath
		Write-Host "✓ AudioPlay lancé" -ForegroundColor Green
		Write-Host ""
		Write-Host "Suivez les instructions de test ci-dessus ↑" -ForegroundColor Cyan
	} else {
		Write-Host "✗ Impossible de lancer AudioPlay.exe" -ForegroundColor Red
	}
} else {
	Write-Host ""
	Write-Host "Pour tester, lancez AudioPlay depuis Visual Studio (F5)" -ForegroundColor Cyan
	Write-Host "Cela vous permettra de voir les logs dans la fenêtre Output" -ForegroundColor Gray
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
