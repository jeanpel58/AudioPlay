But: ce fichier explique comment utiliser le script de conversion des .resx et activer le hook pre-commit.

1) Conversion manuelle (PowerShell)

Ouvrir PowerShell dans la racine du dépôt puis lancer :

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\convert-resx-utf8.ps1

Le script réécrit tous les *.resx en UTF-8 sans BOM et affiche les fichiers traités.

2) Vérifier l'encodage (exemple PowerShell)

Get-ChildItem -Path . -Recurse -Filter *.resx | ForEach-Object { (Get-Content -Path $_.FullName -Raw) | Out-Null; Write-Host "OK: $($_.FullName)" }

3) Activer le hook pre-commit (Husky) - recommandation pour Windows

- Installer Node.js si nécessaire.
- Depuis la racine du repo :
  npm install husky --save-dev
  npx husky install

- Ajouter le hook (si vous voulez qu'il réexécute le script lors des commits) :
  npx husky add .husky/pre-commit "pwsh -NoProfile -ExecutionPolicy Bypass -File tools/convert-resx-utf8.ps1"

Note : le dépôt contient déjà un fichier .husky/pre-commit. L'étape ci-dessus initialisera Husky et assurera que Git exécutera le hook.

4) Alternative sans Husky : hook Git natif

Copiez le fichier .husky/pre-commit en .git/hooks/pre-commit et rendez-le exécutable (sur Windows, utilisez Git Bash) :

cp .husky/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit

5) .editorconfig et .gitattributes

Le dépôt contient .editorconfig et .gitattributes qui indiquent l'encodage et la normalisation des fins de ligne (UTF-8 + CRLF pour *.resx, *.vb, *.sln). Ces fichiers aident les éditeurs et Git à conserver l'encodage désiré.

6) Remarques

- Le script supprime le BOM (UTF-8 sans BOM). Si vous préférez conserver le BOM, modifiez tools/convert-resx-utf8.ps1.
- Le commit automatique depuis l'agent n'a pas pu être effectué ici (git absents de l'environnement). Les fichiers ont été créés dans le dépôt local ; effectuez commit/push depuis votre machine.

Si vous voulez, j’ajoute un script PowerShell pour vérifier l'absence de caractères invalides dans les .resx avant commit.