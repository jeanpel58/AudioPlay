@echo off
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -Command "$content = [System.IO.File]::ReadAllText('Resources.de.resx', [System.Text.Encoding]::UTF8); $content = $content -replace 'Ã¤', 'ä' -replace 'Ã¶', 'ö' -replace 'Ã¼', 'ü' -replace 'ÃŸ', 'ß' -replace 'Ã„', 'Ä' -replace 'Ã–', 'Ö' -replace 'Ãœ', 'Ü'; $utf8 = New-Object System.Text.UTF8Encoding($false); [System.IO.File]::WriteAllText((Resolve-Path 'Resources.de.resx').Path, $content, $utf8); Write-Host '✅ DE corrigé'"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$content = [System.IO.File]::ReadAllText('Resources.it.resx', [System.Text.Encoding]::UTF8); $content = $content -replace 'Ã ', 'à' -replace 'Ã¨', 'è' -replace 'Ã©', 'é' -replace 'Ã¬', 'ì' -replace 'Ã²', 'ò' -replace 'Ã¹', 'ù'; $utf8 = New-Object System.Text.UTF8Encoding($false); [System.IO.File]::WriteAllText((Resolve-Path 'Resources.it.resx').Path, $content, $utf8); Write-Host '✅ IT corrigé'"
echo.
echo ✅ Correction terminée !
pause
