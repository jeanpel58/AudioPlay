# 📦 Guide Compilation et Distribution AudioPlay

## 🎯 Résumé Rapide

**SoundTouch.dll est automatiquement incluse dans l'installateur !**

Aucune manipulation manuelle nécessaire grâce à :
1. ✅ Target MSBuild automatique dans `AudioPlay.vbproj`
2. ✅ Inclusion récursive dans Inno Setup
3. ✅ Scripts de vérification et build automatique

---

## 🚀 Compilation Rapide

### Option 1 : Script Automatique (Recommandé)

```powershell
# Build complet + Installateur
.\build-audioplay-complete.ps1 -Configuration Release -CompileInstaller

# Juste le build (sans installateur)
.\build-audioplay-complete.ps1 -Configuration Release
```

### Option 2 : Visual Studio

1. **Build → Rebuild Solution**
2. Configuration : **Release** (recommandé pour distribution)
3. Vérifier la présence de `SoundTouch.dll` dans `bin\Release\net8.0-windows\`

### Option 3 : Ligne de Commande

```powershell
# Restaurer packages
dotnet restore AudioPlay/AudioPlay.vbproj

# Compiler
dotnet build AudioPlay/AudioPlay.vbproj -c Release

# Vérifier SoundTouch.dll
Test-Path "AudioPlay/bin/Release/net8.0-windows/SoundTouch.dll"
```

---

## 📋 Checklist Avant Distribution

### ☑️ Étape 1 : Build
- [ ] Configuration **Release** utilisée
- [ ] Build réussie sans erreurs
- [ ] `SoundTouch.dll` présent dans `bin\Release\net8.0-windows\`

### ☑️ Étape 2 : Vérification

```powershell
# Script de vérification
.\installer\Scripts\verify-before-build.ps1 -Configuration Release
```

**Résultat attendu** :
```
✅ TOUT EST PRÊT pour compiler l'installateur!
✓ AudioPlay.exe trouvé
✓ SoundTouch.dll trouvé à la racine
✓ NAudio.dll
✓ TagLibSharp.dll
...
```

### ☑️ Étape 3 : Compilation Installateur

**Méthode A : Script Automatique**
```powershell
.\build-audioplay-complete.ps1 -Configuration Release -CompileInstaller
```

**Méthode B : Inno Setup Manuel**
1. Ouvrir **Inno Setup Compiler**
2. Fichier → Ouvrir : `installer\Scripts\AudioPlay 2026-06-01.iss`
3. Build → Compile
4. Vérifier le log (chercher "SoundTouch.dll")

### ☑️ Étape 4 : Tests

1. **Installer** sur une machine de test
2. **Vérifier** la présence de `C:\Program Files\AudioPlay\SoundTouch.dll`
3. **Lancer** AudioPlay
4. **Tester** Time Stretch dans Paramètres → Effets Audio
5. **Vérifier** la qualité audio (doit être comme Audacity)

---

## 🛠️ Scripts Disponibles

| Script | Description | Usage |
|--------|-------------|-------|
| `build-audioplay-complete.ps1` | Build complet + installateur | Recommandé |
| `installer/Scripts/verify-before-build.ps1` | Vérification pré-build | Avant Inno Setup |
| `AudioPlay/copy-soundtouch.ps1` | Copie manuelle DLL | Dépannage |
| `AudioPlay/verify-soundtouch-fix.ps1` | Vérification post-fix | Diagnostic |

### Exemples d'Utilisation

```powershell
# Build Release avec installateur
.\build-audioplay-complete.ps1 -Configuration Release -CompileInstaller

# Build Debug pour tests rapides
.\build-audioplay-complete.ps1 -Configuration Debug

# Vérification avant compilation installateur
.\installer\Scripts\verify-before-build.ps1 -Configuration Release

# Copie manuelle SoundTouch.dll (si problème)
.\AudioPlay\copy-soundtouch.ps1
```

---

## 🔍 Vérification SoundTouch.dll

### Dans le Build

```powershell
# Vérifier présence après build
$path = "AudioPlay\bin\Release\net8.0-windows\SoundTouch.dll"
if (Test-Path $path) {
	Write-Host "✓ SoundTouch.dll présent" -ForegroundColor Green
	Get-Item $path | Select-Object Name, Length, LastWriteTime
} else {
	Write-Host "✗ SoundTouch.dll manquant!" -ForegroundColor Red
}
```

### Dans l'Installateur

```powershell
# Extraire et vérifier (après compilation Inno Setup)
# Note: Nécessite 7-Zip ou outil similaire
7z l "installer\EXE\AudioPlay-Setup.exe" | Select-String "SoundTouch.dll"
```

### Après Installation

```powershell
# Sur la machine cible
Test-Path "C:\Program Files\AudioPlay\SoundTouch.dll"
# → Doit retourner True
```

---

## 🐛 Dépannage

### Problème : SoundTouch.dll Manquant Après Build

**Solution 1** : Rebuild complet
```powershell
dotnet clean AudioPlay/AudioPlay.vbproj
dotnet restore AudioPlay/AudioPlay.vbproj
dotnet build AudioPlay/AudioPlay.vbproj -c Release
```

**Solution 2** : Copie manuelle
```powershell
.\AudioPlay\copy-soundtouch.ps1
```

**Solution 3** : Vérifier le Target MSBuild
```powershell
# Exécuter le target spécifique
dotnet build AudioPlay/AudioPlay.vbproj -t:CopySoundTouchDll -v:detailed
```

### Problème : DLL Non Incluse dans Installateur

**Diagnostic** :
1. Vérifier le log de compilation Inno Setup
2. Chercher "SoundTouch.dll" dans le log
3. Vérifier que `PublishDir` pointe vers le bon répertoire

**Solution** :
```powershell
# Recompiler AudioPlay d'abord
dotnet build AudioPlay/AudioPlay.vbproj -c Release

# Puis recompiler l'installateur
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "installer\Scripts\AudioPlay 2026-06-01.iss"
```

### Problème : Time Stretch Ne Fonctionne Pas Après Installation

**Vérifications** :
1. La DLL est-elle présente ?
   ```powershell
   Test-Path "C:\Program Files\AudioPlay\SoundTouch.dll"
   ```

2. Architecture correcte ?
   - AudioPlay compilé en x64 → Besoin de `SoundTouch.dll` x64
   - AudioPlay compilé en x86 → Besoin de `SoundTouch.dll` x86

3. Logs de débogage :
   - Lancer AudioPlay depuis Visual Studio
   - Activer Time Stretch
   - Vérifier la fenêtre **Output** pour les messages

**Mode Fallback** :
Si SoundTouch ne charge pas, l'effet passe en mode bypass :
- ✅ L'application **ne crashe pas**
- ⚠️ L'audio joue **sans modification de tempo**

---

## 📊 Architecture Complète

```
┌─────────────────────────────────────────────────────────────┐
│ DÉVELOPPEMENT                                               │
├─────────────────────────────────────────────────────────────┤
│ AudioPlay.vbproj                                            │
│   │                                                         │
│   ├─ SoundTouchInterop.vb (P/Invoke wrapper)               │
│   ├─ TimeStretchSampleProvider.vb (utilise SoundTouch)     │
│   └─ Target: CopySoundTouchDll (MSBuild)                   │
│                                                             │
│ Packages NuGet:                                             │
│   ├─ NAudio 2.3.0                                           │
│   ├─ SoundTouchSharp 2.3.2 (contient SoundTouch.dll native)│
│   └─ TagLibSharp 2.3.0                                      │
└─────────────────────────────────────────────────────────────┘
						   ↓
┌─────────────────────────────────────────────────────────────┐
│ BUILD                                                       │
├─────────────────────────────────────────────────────────────┤
│ dotnet build -c Release                                     │
│   │                                                         │
│   ├─ Compilation du code                                   │
│   ├─ Restauration des packages NuGet                       │
│   └─ Exécution Target CopySoundTouchDll                    │
│      → SoundTouch.dll copié de:                            │
│        runtimes/win-x64/native/SoundTouch.dll              │
│      → vers:                                                │
│        bin/Release/net8.0-windows/SoundTouch.dll           │
└─────────────────────────────────────────────────────────────┘
						   ↓
┌─────────────────────────────────────────────────────────────┐
│ PACKAGING (Inno Setup)                                      │
├─────────────────────────────────────────────────────────────┤
│ AudioPlay 2026-06-01.iss                                    │
│   │                                                         │
│   ├─ [Files]                                                │
│   │   ├─ Source: {PublishDir}\* (récursif)                 │
│   │   └─ Source: {PublishDir}\SoundTouch.dll (explicite)   │
│   │                                                         │
│   └─ [Code]                                                 │
│       └─ FileExists() check avec log                       │
│                                                             │
│ Résultat: AudioPlay-Setup.exe                              │
│   └─ Contient TOUT (exe, dll, ressources, SoundTouch)     │
└─────────────────────────────────────────────────────────────┘
						   ↓
┌─────────────────────────────────────────────────────────────┐
│ INSTALLATION                                                │
├─────────────────────────────────────────────────────────────┤
│ AudioPlay-Setup.exe                                         │
│   │                                                         │
│   └─ Installe dans: C:\Program Files\AudioPlay\            │
│       ├─ AudioPlay.exe                                      │
│       ├─ SoundTouch.dll ✅                                  │
│       ├─ NAudio.dll, TagLibSharp.dll                        │
│       └─ Ressources multilingues                            │
└─────────────────────────────────────────────────────────────┘
						   ↓
┌─────────────────────────────────────────────────────────────┐
│ RUNTIME                                                     │
├─────────────────────────────────────────────────────────────┤
│ AudioPlay.exe lancé                                         │
│   │                                                         │
│   ├─ SoundTouchInterop charge SoundTouch.dll (P/Invoke)    │
│   │   └─ Essaie runtimes/ puis racine                      │
│   │                                                         │
│   ├─ TimeStretchSampleProvider utilise SoundTouch          │
│   │   ├─ Si succès: Qualité Audacity ✨                    │
│   │   └─ Si échec: Mode bypass (pas de crash)             │
│   │                                                         │
│   └─ Utilisateur active Time Stretch                       │
│       → Tempo change SANS changer le pitch                 │
│       → Algorithme WSOLA (comme Audacity)                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 📚 Documentation Complète

| Fichier | Contenu |
|---------|---------|
| `REPONSE_FINALE_INNO_SETUP.md` | **Réponse à votre question** |
| `SOUNDTOUCH_INNO_SETUP_GUIDE.md` | Guide détaillé Inno Setup |
| `CORRECTION_TERMINEE.md` | Résumé correction crash |
| `CRASH_FIX_SOUNDTOUCH.md` | Détails techniques correction |
| `TIME_STRETCH_SOUNDTOUCH_NATIF.md` | Algorithme SoundTouch/WSOLA |
| `build-audioplay-complete.ps1` | Script de build automatique |
| `verify-before-build.ps1` | Vérification pré-installateur |

---

## ✅ Conclusion

### Question Initiale
**"Est-ce que la DLL de SoundTouch sera importée automatiquement à la compilation du code avec Inno Setup?"**

### Réponse Définitive
**OUI, absolument !** ✅

Le système est **100% automatique** grâce à :
1. Target MSBuild qui copie la DLL
2. Inclusion récursive dans Inno Setup
3. Protection multi-niveaux (fallback si problème)

### Vous N'avez Rien à Faire Manuellement
Compilez normalement → SoundTouch.dll sera automatiquement :
- ✅ Copié dans le build
- ✅ Inclus dans l'installateur
- ✅ Déployé lors de l'installation
- ✅ Chargé au runtime

**Le Time Stretch fonctionnera avec la qualité d'Audacity !** 🎉

---

**Pour toute question ou problème, consultez les fichiers de documentation ci-dessus.**
