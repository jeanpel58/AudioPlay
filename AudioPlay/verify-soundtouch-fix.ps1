# Script de Vérification Post-Correction Time Stretch

Write-Host "`n=== Vérification Configuration SoundTouch ===" -ForegroundColor Cyan

$outputPath = "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-01\AudioPlay\bin\Debug\net8.0-windows"

# 1. Vérifier que la DLL est à la racine
Write-Host "`n[1] Vérification SoundTouch.dll à la racine..." -ForegroundColor Yellow
$dllRootPath = Join-Path $outputPath "SoundTouch.dll"
if (Test-Path $dllRootPath) {
	$dllInfo = Get-Item $dllRootPath
	Write-Host "  ✓ SoundTouch.dll trouvé" -ForegroundColor Green
	Write-Host "    Taille: $($dllInfo.Length) bytes" -ForegroundColor Gray
	Write-Host "    Date: $($dllInfo.LastWriteTime)" -ForegroundColor Gray
} else {
	Write-Host "  ✗ SoundTouch.dll NON trouvé à la racine" -ForegroundColor Red
	Write-Host "    Attendu: $dllRootPath" -ForegroundColor Gray
}

# 2. Vérifier que la DLL native existe dans runtimes/
Write-Host "`n[2] Vérification DLL native dans runtimes/..." -ForegroundColor Yellow
$dllNativePath = Join-Path $outputPath "runtimes\win-x64\native\SoundTouch.dll"
if (Test-Path $dllNativePath) {
	$dllNativeInfo = Get-Item $dllNativePath
	Write-Host "  ✓ DLL native win-x64 trouvée" -ForegroundColor Green
	Write-Host "    Taille: $($dllNativeInfo.Length) bytes" -ForegroundColor Gray
} else {
	Write-Host "  ✗ DLL native win-x64 NON trouvée" -ForegroundColor Red
}

# 3. Vérifier les fichiers de code source
Write-Host "`n[3] Vérification fichiers sources..." -ForegroundColor Yellow
$basePath = "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-01\AudioPlay"

$sourceFiles = @(
	"AudioEffects\SoundTouchInterop.vb",
	"AudioEffects\TimeStretchSampleProvider.vb",
	"copy-soundtouch.ps1",
	"CRASH_FIX_SOUNDTOUCH.md"
)

foreach ($file in $sourceFiles) {
	$fullPath = Join-Path $basePath $file
	if (Test-Path $fullPath) {
		Write-Host "  ✓ $file" -ForegroundColor Green
	} else {
		Write-Host "  ✗ $file MANQUANT" -ForegroundColor Red
	}
}

# 4. Vérifier le projet (.vbproj)
Write-Host "`n[4] Vérification configuration projet..." -ForegroundColor Yellow
$projPath = Join-Path $basePath "AudioPlay.vbproj"
if (Test-Path $projPath) {
	$projContent = Get-Content $projPath -Raw
	if ($projContent -match "CopySoundTouchDll") {
		Write-Host "  ✓ Target MSBuild 'CopySoundTouchDll' présent" -ForegroundColor Green
	} else {
		Write-Host "  ✗ Target MSBuild 'CopySoundTouchDll' MANQUANT" -ForegroundColor Red
	}

	if ($projContent -match "SoundTouchSharp") {
		Write-Host "  ✓ Package SoundTouchSharp référencé" -ForegroundColor Green
	} else {
		Write-Host "  ✗ Package SoundTouchSharp NON référencé" -ForegroundColor Red
	}
}

# 5. Résumé
Write-Host "`n=== RÉSUMÉ ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration:" -ForegroundColor White
Write-Host "  • DLL copiée automatiquement après build" -ForegroundColor Gray
Write-Host "  • Fallback automatique si DLL non trouvée" -ForegroundColor Gray
Write-Host "  • Protection contre les crashes" -ForegroundColor Gray
Write-Host ""
Write-Host "Test suivant:" -ForegroundColor White
Write-Host "  1. Lancer AudioPlay" -ForegroundColor Gray
Write-Host "  2. Ouvrir un fichier audio" -ForegroundColor Gray
Write-Host "  3. Paramètres → Time Stretch" -ForegroundColor Gray
Write-Host "  4. Vérifier la fenêtre Output dans Visual Studio" -ForegroundColor Gray
Write-Host ""
Write-Host "Logs attendus (si succès):" -ForegroundColor White
Write-Host "  • 'SoundTouch chargé depuis: ...'" -ForegroundColor Green
Write-Host "  • 'SoundTouch natif initialisé avec succès'" -ForegroundColor Green
Write-Host ""
Write-Host "Logs attendus (si échec):" -ForegroundColor White
Write-Host "  • 'SoundTouch DLL non trouvée: ...'" -ForegroundColor Yellow
Write-Host "  • 'Time Stretch désactivé - fallback en mode bypass'" -ForegroundColor Yellow
Write-Host "  • L'application continue de fonctionner normalement" -ForegroundColor Yellow
Write-Host ""
Write-Host "=== Vérification Terminée ===" -ForegroundColor Cyan
Write-Host ""
