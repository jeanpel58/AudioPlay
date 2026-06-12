@echo off
chcp 65001 >nul
cd /d "%~dp0"

powershell -Command "$file='Resources.en.resx'; $enc=[System.Text.Encoding]::UTF8; $content=$enc.GetString([System.IO.File]::ReadAllBytes($file)); $content=$content -replace 'â—','●'; $content=$content -replace 'â– ','■ '; $content=$content -replace 'â€¢','•'; $content=$content -replace 'â—€','◀'; $content=$content -replace 'â–¶','▶'; $bom=New-Object byte[] 3; $bom[0]=0xEF; $bom[1]=0xBB; $bom[2]=0xBF; $bytes=$enc.GetBytes($content); $all=$bom+$bytes; [System.IO.File]::WriteAllBytes($file,$all); Write-Host 'Correction terminée'"

echo.
pause
