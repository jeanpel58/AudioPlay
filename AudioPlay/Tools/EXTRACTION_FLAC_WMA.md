# 🎵 Extraction FLAC et WMA - Installation Automatique de FFMpeg

AudioPlay supporte maintenant le **téléchargement automatique de FFMpeg** pour l'extraction FLAC et WMA !

---

## ✨ Fonctionnement Automatique

### Première extraction FLAC ou WMA

Lorsque vous sélectionnez **FLAC** ou **WMA** pour la première fois et cliquez sur **Extraire** :

1. ✅ AudioPlay détecte automatiquement que FFMpeg n'est pas installé
2. ❓ Une boîte de dialogue vous demande si vous souhaitez le télécharger
3. ⬇️ Si vous acceptez, le téléchargement démarre (environ 120 MB)
4. 📊 Une barre de progression affiche l'avancement
5. ✅ FFMpeg est installé automatiquement dans le dossier `Tools/`
6. 🎉 L'extraction démarre immédiatement

### Extractions suivantes

Une fois FFMpeg installé, toutes les extractions FLAC et WMA fonctionnent directement, sans nouvelle installation !

---

## 🔧 Installation Manuelle (Optionnel)

Si vous préférez installer FFMpeg manuellement :

### Option 1 : Dossier Tools

1. Téléchargez FFMpeg : https://www.gyan.dev/ffmpeg/builds/
2. Extrayez le fichier `ffmpeg.exe` 
3. Copiez-le dans : `AudioPlay\Tools\ffmpeg.exe`

### Option 2 : Installation Système

```powershell
# Chocolatey
choco install ffmpeg

# Winget
winget install ffmpeg

# Scoop
scoop install ffmpeg
```

---

## 📋 Formats Supportés

| Format | Qualité | Compression | Taille | FFMpeg requis |
|--------|---------|-------------|--------|---------------|
| **MP3** | Bonne | Avec perte | ~10 MB/album | ❌ Non |
| **WAV** | Maximale | Aucune | ~500 MB/album | ❌ Non |
| **FLAC** | Maximale | Sans perte | ~250 MB/album | ✅ Oui (auto) |
| **WMA** | Bonne | Avec perte | ~50 MB/album | ✅ Oui (auto) |

---

## ❓ FAQ

**Q : Le téléchargement est-il sûr ?**  
R : Oui ! FFMpeg est téléchargé depuis les sources officielles (gyan.dev ou GitHub).

**Q : Où est stocké FFMpeg ?**  
R : Dans le dossier `Tools/` de l'application AudioPlay.

**Q : Puis-je annuler le téléchargement ?**  
R : Oui, cliquez sur "Annuler" pendant le téléchargement.

**Q : Que se passe-t-il si le téléchargement échoue ?**  
R : Vous pouvez réessayer ou installer manuellement (voir ci-dessus).

**Q : FFMpeg ralentit-il AudioPlay ?**  
R : Non, FFMpeg n'est utilisé que pendant l'extraction FLAC/WMA.

**Q : Puis-je désinstaller FFMpeg ?**  
R : Oui, supprimez simplement `Tools\ffmpeg.exe`.

---

## 🌟 Avantages du Système Automatique

✅ **Zéro configuration** - Tout fonctionne automatiquement  
✅ **Téléchargement unique** - FFMpeg est installé une seule fois  
✅ **Pas d'installation système** - Portable avec l'application  
✅ **Mise à jour facile** - Supprimez ffmpeg.exe pour forcer une nouvelle installation  
✅ **Transparent** - Fonctionne en arrière-plan sans ralentir l'application

---

**AudioPlay** © 2026 | Extraction CD de qualité professionnelle
