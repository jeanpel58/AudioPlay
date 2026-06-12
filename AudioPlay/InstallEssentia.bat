@echo off
echo ========================================
echo Installation Essentia pour AudioPlay
echo ========================================
echo.

REM Chemin Python embedded
set PYTHON_DIR=%APPDATA%\AudioPlay\python_embedded
set PYTHON_EXE=%PYTHON_DIR%\python.exe

echo Verification de Python...
if not exist "%PYTHON_EXE%" (
	echo [ERREUR] Python embedded non trouve!
	echo Veuillez d'abord installer Python via AudioPlay.
	pause
	exit /b 1
)

echo Python trouve: %PYTHON_EXE%
echo.

echo Installation d'Essentia...
echo Cela peut prendre 5-10 minutes (download ~150 MB)...
echo.

REM Installer essentia-tensorflow
"%PYTHON_EXE%" -m pip install --upgrade pip
"%PYTHON_EXE%" -m pip install essentia-tensorflow

if %ERRORLEVEL% EQU 0 (
	echo.
	echo ========================================
	echo Installation reussie!
	echo ========================================
	echo.
	echo Essentia est maintenant installe.
	echo AudioPlay peut utiliser le Machine Learning.
	echo.
) else (
	echo.
	echo ========================================
	echo ERREUR d'installation
	echo ========================================
	echo.
	echo Verifiez votre connexion Internet.
	echo.
)

pause
