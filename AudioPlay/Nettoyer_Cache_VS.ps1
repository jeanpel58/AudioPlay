# Script pour nettoyer les caches Visual Studio
# Exécutez ce script quand Visual Studio est FERMÉ

Write-Host "🧹 Nettoyage des caches Visual Studio..." -ForegroundColor Cyan
Write-Host ""

$workspaceRoot = "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-01"

# Dossier .vs (caché) - contient les paramètres et caches de Visual Studio
$vsFolder = Join-Path $workspaceRoot ".vs"
if (Test-Path $vsFolder) {
	Write-Host "❌ Suppression de .vs" -ForegroundColor Yellow
	Remove-Item -Recurse -Force $vsFolder -ErrorAction SilentlyContinue
	Write-Host "   ✅ .vs supprimé" -ForegroundColor Green
} else {
	Write-Host "ℹ️  .vs n'existe pas (déjà supprimé)" -ForegroundColor Gray
}

# Dossier bin - contient les fichiers compilés
$binFolder = Join-Path $workspaceRoot "AudioPlay\bin"
if (Test-Path $binFolder) {
	Write-Host "❌ Suppression de AudioPlay\bin" -ForegroundColor Yellow
	Remove-Item -Recurse -Force $binFolder -ErrorAction SilentlyContinue
	Write-Host "   ✅ bin supprimé" -ForegroundColor Green
} else {
	Write-Host "ℹ️  AudioPlay\bin n'existe pas (déjà supprimé)" -ForegroundColor Gray
}

# Dossier obj - contient les fichiers intermédiaires de compilation
$objFolder = Join-Path $workspaceRoot "AudioPlay\obj"
if (Test-Path $objFolder) {
	Write-Host "❌ Suppression de AudioPlay\obj" -ForegroundColor Yellow
	Remove-Item -Recurse -Force $objFolder -ErrorAction SilentlyContinue
	Write-Host "   ✅ obj supprimé" -ForegroundColor Green
} else {
	Write-Host "ℹ️  AudioPlay\obj n'existe pas (déjà supprimé)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✅ Nettoyage terminé!" -ForegroundColor Green
Write-Host ""
Write-Host "📌 Vous pouvez maintenant:" -ForegroundColor Cyan
Write-Host "   1. Rouvrir Visual Studio" -ForegroundColor White
Write-Host "   2. Ouvrir la solution AudioPlay" -ForegroundColor White
Write-Host "   3. Faire un Rebuild (Ctrl+Shift+B)" -ForegroundColor White
Write-Host "   4. Ouvrir FormParametres.vb en mode Design" -ForegroundColor White
Write-Host ""
