@echo off
chcp 65001 >nul
echo Correction de l'encodage français...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$content = [System.IO.File]::ReadAllText('Resources.resx', [System.Text.Encoding]::UTF8); $content = $content -replace 'Ã©', 'é' -replace 'Ã¨', 'è' -replace 'Ãª', 'ê' -replace 'Ã ', 'à' -replace 'Ã´', 'ô' -replace 'Ã®', 'î' -replace 'Ã»', 'û' -replace 'Ã§', 'ç' -replace 'Ã¢', 'â' -replace 'Ã‰', 'É' -replace 'Ãˆ', 'È' -replace 'Ãª', 'Ê' -replace 'Ã€', 'À' -replace 'Ã"', 'Ô' -replace 'Ã®', 'Î' -replace 'Ã›', 'Û' -replace 'Ã‡', 'Ç'; $utf8 = New-Object System.Text.UTF8Encoding($false); [System.IO.File]::WriteAllText((Resolve-Path 'Resources.resx').Path, $content, $utf8); Write-Host '✅ Resources.resx corrigé'"
echo ✅ Correction terminée !
