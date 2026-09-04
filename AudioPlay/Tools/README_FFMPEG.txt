═══════════════════════════════════════════════════════════════════
  INSTALLATION DE FFMPEG POUR L'EXTRACTION FLAC ET WMA
═══════════════════════════════════════════════════════════════════

AudioPlay utilise FFMpeg pour encoder les fichiers audio en format FLAC et WMA.

Pour activer l'extraction FLAC et WMA, vous devez installer FFMpeg :

OPTION 1 : Placer ffmpeg.exe dans ce dossier (Tools)
──────────────────────────────────────────────────────
1. Téléchargez FFMpeg depuis : https://www.gyan.dev/ffmpeg/builds/
   ou depuis : https://github.com/BtbN/FFmpeg-Builds/releases

2. Extrayez l'archive et copiez le fichier "ffmpeg.exe" dans ce dossier :
   AudioPlay\Tools\ffmpeg.exe

3. Relancez AudioPlay


OPTION 2 : Installer FFMpeg dans le système (PATH)
──────────────────────────────────────────────────────
1. Téléchargez et installez FFMpeg via :
   - Windows : Chocolatey (choco install ffmpeg)
   - Winget : winget install ffmpeg
   - Ou manuellement depuis https://www.gyan.dev/ffmpeg/builds/

2. Assurez-vous que ffmpeg.exe est dans le PATH système

3. Relancez AudioPlay


VÉRIFICATION
──────────────────────────────────────────────────────
Pour vérifier que FFMpeg est correctement installé, ouvrez un terminal
et tapez :
	ffmpeg -version

Vous devriez voir les informations de version de FFMpeg.


NOTES
──────────────────────────────────────────────────────
- Sans FFMpeg, seuls les formats MP3 et WAV seront disponibles
- FLAC : Compression sans perte (qualité CD, fichier plus petit que WAV)
- WMA : Compression avec perte (qualité élevée, fichier compact)

═══════════════════════════════════════════════════════════════════
