# Script pour copier SoundTouch.dll native vers le répertoire de sortie
# À exécuter automatiquement après la compilation

param(
	[string]$OutputPath = "bin\Debug\net8.0-windows"
)

Write-Host "=== Copie de SoundTouch.dll ===" -ForegroundColor Cyan

# Détecter l'architecture
$architecture = if ([Environment]::Is64BitProcess) { "win-x64" } else { "win-x86" }
Write-Host "Architecture détectée: $architecture" -ForegroundColor Yellow

# Chemins source et destination
$sourcePath = Join-Path $OutputPath "runtimes\$architecture\native\SoundTouch.dll"
$destPath = Join-Path $OutputPath "SoundTouch.dll"

# Vérifier que le fichier source existe
if (Test-Path $sourcePath) {
	Write-Host "Source trouvée: $sourcePath" -ForegroundColor Green

	# Copier la DLL
	try {
		Copy-Item -Path $sourcePath -Destination $destPath -Force
		Write-Host "✓ SoundTouch.dll copié vers: $destPath" -ForegroundColor Green
	}
	catch {
		Write-Host "✗ Erreur lors de la copie: $_" -ForegroundColor Red
		exit 1
	}
}
else {
	Write-Host "✗ Source introuvable: $sourcePath" -ForegroundColor Red
	Write-Host "La DLL native ne sera pas disponible pour P/Invoke" -ForegroundColor Yellow
	Write-Host "Time Stretch fonctionnera en mode bypass (désactivé)" -ForegroundColor Yellow
}

Write-Host "=== Copie terminée ===" -ForegroundColor Cyan
