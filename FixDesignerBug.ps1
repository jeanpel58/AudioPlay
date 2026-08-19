# Script pour corriger le bug du designer Visual Studio qui cree des variables locales
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .\FixDesignerBug.ps1

$designerFile = "AudioPlay\Form1.Designer.vb"

Write-Host "Correction du bug du designer dans $designerFile..." -ForegroundColor Yellow

if (-not (Test-Path $designerFile)) {
	Write-Host "Fichier non trouve: $designerFile" -ForegroundColor Red
	exit 1
}

$content = Get-Content $designerFile -Raw
$modified = $false

# Chercher et supprimer toutes les lignes "Dim Button* As Button" dans InitializeComponent
$pattern = '(\s*Private Sub InitializeComponent\(\)\r?\n)(\s*Dim\s+\w+\s+As\s+Button\r?\n)+'
if ($content -match $pattern) {
	# Garder seulement la declaration de la methode, supprimer les Dim Button
	$content = $content -replace '(\s*Private Sub InitializeComponent\(\)\r?\n)(\s*Dim\s+\w+\s+As\s+Button\r?\n)+', '$1'
	$modified = $true
	Write-Host "Lignes 'Dim Button*' supprimees" -ForegroundColor Green
}

if ($modified) {
	Set-Content $designerFile -Value $content -NoNewline
	Write-Host "Fichier corrige et sauvegarde" -ForegroundColor Green
} else {
	Write-Host "Aucune correction necessaire" -ForegroundColor Green
}

Write-Host "Termine!" -ForegroundColor Cyan
