@echo off
chcp 65001 >nul
echo Correction de l'encodage italien...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$content = [System.IO.File]::ReadAllText('Resources.it.resx', [System.Text.Encoding]::UTF8); $content = $content -replace 'Ã ', 'à' -replace 'Ã¨', 'è' -replace 'Ã©', 'é' -replace 'Ã¬', 'ì' -replace 'Ã²', 'ò' -replace 'Ã¹', 'ù' -replace 'Ã  ', 'à ' -replace 'Ã¨ ', 'è ' -replace 'Ã© ', 'é ' -replace 'Ã¬ ', 'ì ' -replace 'Ã² ', 'ò ' -replace 'Ã¹ ', 'ù '; $utf8 = New-Object System.Text.UTF8Encoding($false); [System.IO.File]::WriteAllText((Resolve-Path 'Resources.it.resx').Path, $content, $utf8); Write-Host '✅ Resources.it.resx corrigé'"
echo ✅ Correction terminée !
