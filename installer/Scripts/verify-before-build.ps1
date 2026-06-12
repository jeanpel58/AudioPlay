# Script de Vérification Pré-Compilation Installateur
# À exécuter AVANT de compiler avec Inno Setup

param(
	[string]$Configuration = "Debug"
)

Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Vérification Pré-Compilation Installateur AudioPlay        ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

$basePath = "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-01"
$publishDir = Join-Path $basePath "AudioPlay\bin\$Configuration\net8.0-windows"

Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Répertoire de publication: $publishDir`n" -ForegroundColor Gray

$allOk = $true

# ═══════════════════════════════════════════════════════════════
# 1. Vérifier l'exécutable principal
# ═══════════════════════════════════════════════════════════════
Write-Host "[1] Vérification de l'exécutable principal..." -ForegroundColor White
$exePath = Join-Path $publishDir "AudioPlay.exe"
if (Test-Path $exePath) {
	$exeInfo = Get-Item $exePath
	Write-Host "    ✓ AudioPlay.exe trouvé" -ForegroundColor Green
	Write-Host "      Taille: $([math]::Round($exeInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
	Write-Host "      Date: $($exeInfo.LastWriteTime)" -ForegroundColor Gray
} else {
	Write-Host "    ✗ AudioPlay.exe MANQUANT!" -ForegroundColor Red
	Write-Host "      → Compilez d'abord le projet AudioPlay.vbproj" -ForegroundColor Yellow
	$allOk = $false
}

# ═══════════════════════════════════════════════════════════════
# 2. Vérifier SoundTouch.dll (CRITIQUE pour Time Stretch)
# ═══════════════════════════════════════════════════════════════
Write-Host "`n[2] Vérification de SoundTouch.dll (Time Stretch)..." -ForegroundColor White
$soundTouchPath = Join-Path $publishDir "SoundTouch.dll"
if (Test-Path $soundTouchPath) {
	$dllInfo = Get-Item $soundTouchPath
	Write-Host "    ✓ SoundTouch.dll trouvé à la racine" -ForegroundColor Green
	Write-Host "      Taille: $($dllInfo.Length) bytes" -ForegroundColor Gray
	Write-Host "      Date: $($dllInfo.LastWriteTime)" -ForegroundColor Gray
} else {
	Write-Host "    ✗ SoundTouch.dll MANQUANT à la racine!" -ForegroundColor Red

	# Vérifier si la DLL existe dans runtimes/
	$runtimeDll = Join-Path $publishDir "runtimes\win-x64\native\SoundTouch.dll"
	if (Test-Path $runtimeDll) {
		Write-Host "      ⚠ La DLL existe dans runtimes\ mais pas à la racine" -ForegroundColor Yellow
		Write-Host "      → Rebuild le projet pour déclencher le Target MSBuild" -ForegroundColor Yellow
	} else {
		Write-Host "      → Package SoundTouchSharp manquant ou non restauré" -ForegroundColor Yellow
		Write-Host "      → Exécutez: dotnet restore AudioPlay/AudioPlay.vbproj" -ForegroundColor Yellow
	}
	$allOk = $false
}

# ═══════════════════════════════════════════════════════════════
# 3. Vérifier les DLL NAudio (critiques)
# ═══════════════════════════════════════════════════════════════
Write-Host "`n[3] Vérification des DLL NAudio..." -ForegroundColor White
$requiredDlls = @(
	"NAudio.dll",
	"NAudio.Core.dll",
	"NAudio.WinMM.dll"
)

$naudioOk = $true
foreach ($dll in $requiredDlls) {
	$dllPath = Join-Path $publishDir $dll
	if (Test-Path $dllPath) {
		Write-Host "    ✓ $dll" -ForegroundColor Green
	} else {
		Write-Host "    ✗ $dll MANQUANT" -ForegroundColor Red
		$naudioOk = $false
		$allOk = $false
	}
}

if (-not $naudioOk) {
	Write-Host "      → Restaurez les packages NuGet" -ForegroundColor Yellow
}

# ═══════════════════════════════════════════════════════════════
# 4. Vérifier TagLibSharp (métadonnées)
# ═══════════════════════════════════════════════════════════════
Write-Host "`n[4] Vérification de TagLibSharp..." -ForegroundColor White
$tagLibPath = Join-Path $publishDir "TagLibSharp.dll"
if (Test-Path $tagLibPath) {
	Write-Host "    ✓ TagLibSharp.dll trouvé" -ForegroundColor Green
} else {
	Write-Host "    ✗ TagLibSharp.dll MANQUANT" -ForegroundColor Red
	$allOk = $false
}

# ═══════════════════════════════════════════════════════════════
# 5. Vérifier les fichiers de ressources
# ═══════════════════════════════════════════════════════════════
Write-Host "`n[5] Vérification des fichiers de ressources..." -ForegroundColor White
$resourceFiles = @(
	"fr\AudioPlay.resources.dll",
	"en\AudioPlay.resources.dll",
	"es\AudioPlay.resources.dll",
	"de\AudioPlay.resources.dll",
	"it\AudioPlay.resources.dll"
)

$resourcesOk = $true
foreach ($resource in $resourceFiles) {
	$resourcePath = Join-Path $publishDir $resource
	if (Test-Path $resourcePath) {
		Write-Host "    ✓ $resource" -ForegroundColor Green
	} else {
		Write-Host "    ⚠ $resource manquant" -ForegroundColor Yellow
		$resourcesOk = $false
	}
}

if (-not $resourcesOk) {
	Write-Host "      → Certaines langues peuvent ne pas fonctionner" -ForegroundColor Yellow
}

# ═══════════════════════════════════════════════════════════════
# 6. Vérifier les fichiers de documentation
# ═══════════════════════════════════════════════════════════════
Write-Host "`n[6] Vérification des fichiers de documentation..." -ForegroundColor White
$docFiles = @(
	"AUDIOPLAY_GUIDE_COMPLET.fr.html",
	"AUDIOPLAY_GUIDE_COMPLET.en.html"
)

foreach ($doc in $docFiles) {
	$docPath = Join-Path $publishDir $doc
	if (Test-Path $docPath) {
		Write-Host "    ✓ $doc" -ForegroundColor Green
	} else {
		Write-Host "    ⚠ $doc manquant (non critique)" -ForegroundColor Yellow
	}
}

# ═══════════════════════════════════════════════════════════════
# 7. Vérifier Python embarqué (BPM detection)
# ═══════════════════════════════════════════════════════════════
Write-Host "`n[7] Vérification de Python embarqué (BPM)..." -ForegroundColor White
$pythonPath = Join-Path $basePath "installer\Scripts\python_embedded"
if (Test-Path $pythonPath) {
	$pythonExe = Join-Path $pythonPath "python.exe"
	if (Test-Path $pythonExe) {
		Write-Host "    ✓ Python embarqué trouvé" -ForegroundColor Green
	} else {
		Write-Host "    ⚠ python.exe manquant dans python_embedded" -ForegroundColor Yellow
	}
} else {
	Write-Host "    ⚠ Dossier python_embedded introuvable" -ForegroundColor Yellow
	Write-Host "      → BPM detection avec Librosa ne fonctionnera pas" -ForegroundColor Yellow
}

# ═══════════════════════════════════════════════════════════════
# 8. Vérifier le script Inno Setup
# ═══════════════════════════════════════════════════════════════
Write-Host "`n[8] Vérification du script Inno Setup..." -ForegroundColor White
$issPath = Join-Path $basePath "installer\Scripts\AudioPlay 2026-06-01.iss"
if (Test-Path $issPath) {
	Write-Host "    ✓ Script Inno Setup trouvé" -ForegroundColor Green

	# Vérifier que PublishDir pointe vers le bon répertoire
	$issContent = Get-Content $issPath -Raw
	if ($issContent -match "PublishDir.*$Configuration") {
		Write-Host "    ✓ PublishDir correspond à la configuration $Configuration" -ForegroundColor Green
	} else {
		Write-Host "    ⚠ PublishDir ne correspond pas à $Configuration" -ForegroundColor Yellow
		Write-Host "      → Vérifiez #define PublishDir dans le script .iss" -ForegroundColor Yellow
	}
} else {
	Write-Host "    ✗ Script Inno Setup introuvable" -ForegroundColor Red
	Write-Host "      Chemin attendu: $issPath" -ForegroundColor Gray
	$allOk = $false
}

# ═══════════════════════════════════════════════════════════════
# RÉSUMÉ FINAL
# ═══════════════════════════════════════════════════════════════
Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                        RÉSUMÉ                                 ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

if ($allOk) {
	Write-Host "✅ TOUT EST PRÊT pour compiler l'installateur!" -ForegroundColor Green
	Write-Host ""
	Write-Host "Prochaines étapes:" -ForegroundColor White
	Write-Host "  1. Ouvrir: $issPath" -ForegroundColor Gray
	Write-Host "  2. Dans Inno Setup: Build → Compile" -ForegroundColor Gray
	Write-Host "  3. Tester: installer\EXE\AudioPlay-Setup.exe" -ForegroundColor Gray
	Write-Host ""
} else {
	Write-Host "❌ ERREURS DÉTECTÉES - Corrigez avant de compiler l'installateur" -ForegroundColor Red
	Write-Host ""
	Write-Host "Actions recommandées:" -ForegroundColor White
	Write-Host "  1. Rebuild le projet: Build → Rebuild Solution" -ForegroundColor Gray
	Write-Host "  2. Restaurer packages: dotnet restore" -ForegroundColor Gray
	Write-Host "  3. Réexécuter ce script de vérification" -ForegroundColor Gray
	Write-Host ""
	exit 1
}

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
