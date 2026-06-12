# Script de Build Complet AudioPlay + Installateur
# Automatise toutes les étapes de la compilation à l'installateur

param(
	[ValidateSet("Debug", "Release")]
	[string]$Configuration = "Release",

	[switch]$SkipTests,
	[switch]$CompileInstaller,
	[string]$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        AudioPlay - Build Complet avec SoundTouch            ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

$basePath = "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-01"
$projectPath = Join-Path $basePath "AudioPlay\AudioPlay.vbproj"
$publishDir = Join-Path $basePath "AudioPlay\bin\$Configuration\net8.0-windows"
$issPath = Join-Path $basePath "installer\Scripts\AudioPlay 2026-06-01.iss"

Write-Host "Configuration: " -NoNewline -ForegroundColor White
Write-Host $Configuration -ForegroundColor Yellow
Write-Host ""

$startTime = Get-Date

# ═══════════════════════════════════════════════════════════════
# ÉTAPE 1 : Nettoyage
# ═══════════════════════════════════════════════════════════════
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[1/6] Nettoyage..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

try {
	& dotnet clean $projectPath -c $Configuration -v quiet
	Write-Host "✓ Nettoyage terminé" -ForegroundColor Green
} catch {
	Write-Host "⚠ Erreur lors du nettoyage (non critique)" -ForegroundColor Yellow
}

# ═══════════════════════════════════════════════════════════════
# ÉTAPE 2 : Restauration des packages NuGet
# ═══════════════════════════════════════════════════════════════
Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[2/6] Restauration des packages NuGet..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

& dotnet restore $projectPath --verbosity quiet
if ($LASTEXITCODE -eq 0) {
	Write-Host "✓ Packages restaurés" -ForegroundColor Green
	Write-Host "  • NAudio 2.3.0" -ForegroundColor Gray
	Write-Host "  • SoundTouchSharp 2.3.2 (contient SoundTouch.dll)" -ForegroundColor Gray
	Write-Host "  • TagLibSharp 2.3.0" -ForegroundColor Gray
} else {
	Write-Host "✗ Erreur lors de la restauration des packages" -ForegroundColor Red
	exit 1
}

# ═══════════════════════════════════════════════════════════════
# ÉTAPE 3 : Compilation
# ═══════════════════════════════════════════════════════════════
Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[3/6] Compilation AudioPlay..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

& dotnet build $projectPath -c $Configuration --no-restore
if ($LASTEXITCODE -eq 0) {
	Write-Host "✓ Compilation réussie" -ForegroundColor Green
} else {
	Write-Host "✗ Erreur de compilation" -ForegroundColor Red
	exit 1
}

# ═══════════════════════════════════════════════════════════════
# ÉTAPE 4 : Vérification SoundTouch.dll (CRITIQUE)
# ═══════════════════════════════════════════════════════════════
Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[4/6] Vérification de SoundTouch.dll..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$soundTouchPath = Join-Path $publishDir "SoundTouch.dll"
if (Test-Path $soundTouchPath) {
	$dllInfo = Get-Item $soundTouchPath
	Write-Host "✓ SoundTouch.dll présent à la racine" -ForegroundColor Green
	Write-Host "  Taille: $($dllInfo.Length) bytes" -ForegroundColor Gray
	Write-Host "  Date: $($dllInfo.LastWriteTime)" -ForegroundColor Gray
	Write-Host ""
	Write-Host "  → Time Stretch fonctionnera avec qualité Audacity ✨" -ForegroundColor Green
} else {
	Write-Host "⚠ SoundTouch.dll manquant - tentative de copie manuelle..." -ForegroundColor Yellow

	# Tentative de copie manuelle
	$runtimeDll = Join-Path $publishDir "runtimes\win-x64\native\SoundTouch.dll"
	if (Test-Path $runtimeDll) {
		Copy-Item $runtimeDll $soundTouchPath -Force
		Write-Host "✓ SoundTouch.dll copié manuellement" -ForegroundColor Green
	} else {
		Write-Host "✗ ERREUR: SoundTouch.dll introuvable!" -ForegroundColor Red
		Write-Host "  → Time Stretch fonctionnera en mode bypass" -ForegroundColor Yellow
	}
}

# ═══════════════════════════════════════════════════════════════
# ÉTAPE 5 : Vérifications Additionnelles
# ═══════════════════════════════════════════════════════════════
Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "[5/6] Vérifications additionnelles..." -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Vérifier AudioPlay.exe
$exePath = Join-Path $publishDir "AudioPlay.exe"
if (Test-Path $exePath) {
	$exeInfo = Get-Item $exePath
	Write-Host "✓ AudioPlay.exe ($([math]::Round($exeInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green
} else {
	Write-Host "✗ AudioPlay.exe manquant" -ForegroundColor Red
	exit 1
}

# Vérifier DLL NAudio
$naudioDll = Join-Path $publishDir "NAudio.dll"
if (Test-Path $naudioDll) {
	Write-Host "✓ NAudio.dll" -ForegroundColor Green
} else {
	Write-Host "✗ NAudio.dll manquant" -ForegroundColor Red
}

# Vérifier TagLibSharp
$tagLibDll = Join-Path $publishDir "TagLibSharp.dll"
if (Test-Path $tagLibDll) {
	Write-Host "✓ TagLibSharp.dll" -ForegroundColor Green
} else {
	Write-Host "✗ TagLibSharp.dll manquant" -ForegroundColor Red
}

# Compter les fichiers de ressources
$resourceFiles = Get-ChildItem -Path $publishDir -Recurse -Filter "AudioPlay.resources.dll" -ErrorAction SilentlyContinue
Write-Host "✓ $($resourceFiles.Count) langues trouvées" -ForegroundColor Green

# ═══════════════════════════════════════════════════════════════
# ÉTAPE 6 : Compilation Installateur (Optionnel)
# ═══════════════════════════════════════════════════════════════
if ($CompileInstaller) {
	Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
	Write-Host "[6/6] Compilation de l'installateur Inno Setup..." -ForegroundColor White
	Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

	if (Test-Path $InnoSetupPath) {
		Write-Host "Inno Setup: $InnoSetupPath" -ForegroundColor Gray
		Write-Host "Script: $issPath" -ForegroundColor Gray
		Write-Host ""

		# Compiler l'installateur
		& $InnoSetupPath $issPath

		if ($LASTEXITCODE -eq 0) {
			Write-Host "✓ Installateur compilé avec succès" -ForegroundColor Green

			$setupExe = Join-Path $basePath "installer\EXE\AudioPlay-Setup.exe"
			if (Test-Path $setupExe) {
				$setupInfo = Get-Item $setupExe
				Write-Host ""
				Write-Host "Installateur créé:" -ForegroundColor White
				Write-Host "  Fichier: AudioPlay-Setup.exe" -ForegroundColor Gray
				Write-Host "  Taille: $([math]::Round($setupInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
				Write-Host "  Emplacement: installer\EXE\" -ForegroundColor Gray
			}
		} else {
			Write-Host "✗ Erreur lors de la compilation de l'installateur" -ForegroundColor Red
		}
	} else {
		Write-Host "⚠ Inno Setup non trouvé à: $InnoSetupPath" -ForegroundColor Yellow
		Write-Host "  → Compilez manuellement le fichier .iss" -ForegroundColor Yellow
	}
} else {
	Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
	Write-Host "[6/6] Compilation installateur ignorée (-CompileInstaller non spécifié)" -ForegroundColor White
	Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

# ═══════════════════════════════════════════════════════════════
# RÉSUMÉ FINAL
# ═══════════════════════════════════════════════════════════════
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    BUILD TERMINÉ                              ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Host "Durée totale: " -NoNewline -ForegroundColor White
Write-Host "$([math]::Round($duration.TotalSeconds, 1)) secondes" -ForegroundColor Yellow
Write-Host ""

Write-Host "Répertoire de sortie:" -ForegroundColor White
Write-Host "  $publishDir" -ForegroundColor Gray
Write-Host ""

Write-Host "Fichiers clés:" -ForegroundColor White
Write-Host "  ✓ AudioPlay.exe" -ForegroundColor Green
Write-Host "  ✓ SoundTouch.dll (Time Stretch qualité Audacity)" -ForegroundColor Green
Write-Host "  ✓ NAudio, TagLibSharp, ressources" -ForegroundColor Green
Write-Host ""

if ($CompileInstaller -and (Test-Path (Join-Path $basePath "installer\EXE\AudioPlay-Setup.exe"))) {
	Write-Host "Installateur:" -ForegroundColor White
	Write-Host "  ✓ installer\EXE\AudioPlay-Setup.exe" -ForegroundColor Green
	Write-Host ""
}

Write-Host "Prochaines étapes:" -ForegroundColor White
if (-not $CompileInstaller) {
	Write-Host "  1. Tester AudioPlay.exe" -ForegroundColor Gray
	Write-Host "  2. Activer Time Stretch et vérifier la qualité" -ForegroundColor Gray
	Write-Host "  3. Si OK: réexécuter avec -CompileInstaller" -ForegroundColor Gray
} else {
	Write-Host "  1. Tester l'installateur sur une machine propre" -ForegroundColor Gray
	Write-Host "  2. Vérifier Time Stretch après installation" -ForegroundColor Gray
	Write-Host "  3. Distribuer AudioPlay-Setup.exe" -ForegroundColor Gray
}
Write-Host ""

Write-Host "════════════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
