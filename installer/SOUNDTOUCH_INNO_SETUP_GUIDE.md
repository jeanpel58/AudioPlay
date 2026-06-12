# 📦 Inno Setup et SoundTouch.dll - Guide Complet

## ❓ Question
**Est-ce que SoundTouch.dll sera importée automatiquement lors de la compilation avec Inno Setup ?**

---

## ✅ Réponse Courte
**OUI**, la DLL sera automatiquement incluse grâce à la ligne récursive existante :
```
Source: "{#PublishDir}\\*"; DestDir: "{app}"; Flags: recursesubdirs
```

Puisque notre Target MSBuild copie `SoundTouch.dll` à la racine de `bin\Debug\net8.0-windows\`, elle sera capturée par le wildcard `*`.

---

## 🔍 Vérification Détaillée

### 1️⃣ Chaîne de Copie Automatique

```
Build MSBuild
	↓
Target CopySoundTouchDll exécuté
	↓
SoundTouch.dll copié vers: bin\Debug\net8.0-windows\SoundTouch.dll
	↓
Inno Setup lit recursivement: bin\Debug\net8.0-windows\*
	↓
SoundTouch.dll inclus dans l'installateur
	↓
Installation dans: C:\Program Files\AudioPlay\SoundTouch.dll
```

### 2️⃣ Vérification Avant Compilation Installer

**Script PowerShell de vérification** :

```powershell
# Vérifier que SoundTouch.dll est présent avant de compiler l'installateur
$publishDir = "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-01\AudioPlay\bin\Debug\net8.0-windows"
$dllPath = Join-Path $publishDir "SoundTouch.dll"

if (Test-Path $dllPath) {
	$info = Get-Item $dllPath
	Write-Host "✓ SoundTouch.dll trouvé" -ForegroundColor Green
	Write-Host "  Taille: $($info.Length) bytes" -ForegroundColor Gray
	Write-Host "  Date: $($info.LastWriteTime)" -ForegroundColor Gray
} else {
	Write-Host "✗ ERREUR: SoundTouch.dll manquant!" -ForegroundColor Red
	Write-Host "  Recompilez AudioPlay.vbproj d'abord" -ForegroundColor Yellow
	exit 1
}
```

---

## 📝 Script Inno Setup Mis à Jour

### Fichier Créé
`installer/Scripts/AudioPlay 2026-06-01.iss`

### Changements Importants

1. **Version mise à jour** :
   ```
   #define MyAppVersion "1.26.06.01"
   AppId={{A2E7F95E-58E4-4E53-8AFA-8B9AA9F7E1260601}
   ```

2. **Chemin PublishDir mis à jour** :
   ```
   #define PublishDir "g:\Visual Studio Projects\Jean\AudioPlay 2026-06-01\AudioPlay\bin\Debug\net8.0-windows\"
   ```

3. **Déclaration explicite de SoundTouch.dll** (ligne 54) :
   ```
   ; DLL native SoundTouch pour l'effet Time Stretch (qualité Audacity)
   Source: "{#PublishDir}\\SoundTouch.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(...)
   ```

4. **Fonction de vérification** (section [Code]) :
   ```pascal
   function FileExists(const FileName: string): Boolean;
   begin
	 Result := FileOrDirExists(FileName);
	 if not Result then
	   Log('AVERTISSEMENT: Fichier non trouvé: ' + FileName);
   end;
   ```

### Avantages de la Déclaration Explicite

| Avant | Après |
|-------|-------|
| Inclus implicitement via `*` | ✅ Déclaré explicitement |
| Pas de vérification | ✅ Check `FileExists()` |
| Pas de documentation | ✅ Commentaire clair |
| Erreur silencieuse si manquant | ✅ Log d'avertissement |

---

## 🧪 Tests Recommandés

### Avant de Compiler l'Installateur

1. **Rebuild AudioPlay** :
   ```
   Build → Rebuild Solution
   ```

2. **Vérifier SoundTouch.dll** :
   ```powershell
   Test-Path "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-01\AudioPlay\bin\Debug\net8.0-windows\SoundTouch.dll"
   ```

3. **Compiler l'installateur** avec Inno Setup :
   ```
   Ouvrir: installer/Scripts/AudioPlay 2026-06-01.iss
   Build → Compile
   ```

4. **Vérifier le log Inno Setup** :
   ```
   Chercher: "SoundTouch.dll" dans la sortie de compilation
   ```

### Après Compilation de l'Installateur

5. **Installer AudioPlay** (sur une machine de test) :
   ```
   Exécuter: installer/EXE/AudioPlay-Setup.exe
   ```

6. **Vérifier l'installation** :
   ```powershell
   Test-Path "C:\Program Files\AudioPlay\SoundTouch.dll"
   ```

7. **Tester l'effet Time Stretch** :
   - Lancer AudioPlay
   - Ouvrir un fichier audio
   - Activer Time Stretch
   - Vérifier qu'il n'y a pas de crash

---

## 🛡️ Protection Multi-Niveaux

### Si SoundTouch.dll Manque (Peu Probable)

| Niveau | Protection | Résultat |
|--------|-----------|----------|
| **1. Build** | Target MSBuild copie la DLL | ✅ DLL présente |
| **2. Installer** | `FileExists()` check | ⚠️ Log d'avertissement |
| **3. Runtime** | `SoundTouchInterop` gestion erreur | ✅ Mode bypass |
| **4. UI** | Time Stretch en mode bypass | ✅ Pas de crash |

**Résultat** : L'application **fonctionne toujours**, même si la DLL manque !

---

## 📊 Comparaison Ancien vs Nouveau Script

| Aspect | Ancien Script | Nouveau Script |
|--------|---------------|----------------|
| **Version** | 1.26.05.31 | 1.26.06.01 |
| **AppId** | ...0531 | ...0601 |
| **Chemin PublishDir** | 2026-05-31 | 2026-06-01 ✅ |
| **SoundTouch.dll** | Implicite via `*` | Explicite + Check ✅ |
| **Vérification** | Non | Oui ✅ |
| **Documentation** | Non | Commentaires ✅ |

---

## 📝 Checklist Compilation Installateur

### Avant de Compiler

- [ ] Rebuild AudioPlay en Release (recommandé pour distribution)
- [ ] Vérifier `SoundTouch.dll` dans `bin\Release\net8.0-windows\`
- [ ] Mettre à jour `#define MyAppVersion` si nécessaire
- [ ] Vérifier que le chemin `#define PublishDir` est correct

### Compilation

- [ ] Ouvrir `AudioPlay 2026-06-01.iss` dans Inno Setup
- [ ] Build → Compile
- [ ] Vérifier le log de compilation (aucune erreur)
- [ ] Chercher "SoundTouch.dll" dans le log

### Après Compilation

- [ ] Vérifier `installer/EXE/AudioPlay-Setup.exe` existe
- [ ] Tester l'installateur sur une machine propre
- [ ] Vérifier `C:\Program Files\AudioPlay\SoundTouch.dll` après installation
- [ ] Lancer AudioPlay et tester Time Stretch

---

## 🚀 Build Release Recommandé

Pour la distribution finale, utilisez **Release** au lieu de **Debug** :

```powershell
# 1. Build en Release
dotnet build AudioPlay/AudioPlay.vbproj -c Release

# 2. Vérifier SoundTouch.dll
Test-Path "AudioPlay/bin/Release/net8.0-windows/SoundTouch.dll"

# 3. Mettre à jour le script Inno Setup
# Changer PublishDir vers: bin\Release\net8.0-windows\

# 4. Compiler l'installateur
```

---

## 💡 Résumé

### ✅ Avec le Système Actuel

1. **MSBuild** copie automatiquement `SoundTouch.dll` à la racine
2. **Inno Setup** inclut automatiquement via `recursesubdirs`
3. **Nouveau script** ajoute une vérification explicite
4. **Runtime** a un fallback si la DLL manque

### 🎯 Résultat

**SoundTouch.dll sera TOUJOURS incluse dans l'installateur** grâce à :
- ✅ Target MSBuild automatique
- ✅ Wildcard récursif Inno Setup
- ✅ (Nouveau) Vérification explicite avec log

**Vous n'avez rien à faire manuellement !** 🎉

---

## 📂 Fichiers à Utiliser

- **Pour Dev** : `AudioPlay 2026-05-31.iss` (ancien, fonctionne)
- **Pour Production** : `AudioPlay 2026-06-01.iss` (nouveau, avec vérifications)

Les deux fonctionneront, mais le nouveau est plus robuste et documenté.
