# Installation de FFMpeg pour AudioPlay

AudioPlay utilise FFMpeg pour encoder les fichiers audio en format **FLAC** et **WMA**.

## 🎯 Méthode simple (Recommandée)

### Option 1 : Téléchargement direct

1. **Téléchargez FFMpeg** depuis :
   - 🔗 https://www.gyan.dev/ffmpeg/builds/
   - Choisissez **"ffmpeg-release-essentials.zip"**

2. **Extrayez l'archive** téléchargée

3. **Copiez** le fichier `ffmpeg.exe` depuis le dossier `bin/` vers :
   ```
   AudioPlay\Tools\ffmpeg.exe
   ```

4. **Relancez AudioPlay** - L'extraction FLAC et WMA sera disponible !

---

### Option 2 : Installation système (PATH)

#### Via Chocolatey :
```powershell
choco install ffmpeg
```

#### Via Winget :
```powershell
winget install ffmpeg
```

#### Via Scoop :
```powershell
scoop install ffmpeg
```

---

## ✅ Vérification

Pour vérifier que FFMpeg est correctement installé, ouvrez PowerShell/CMD et tapez :

```cmd
ffmpeg -version
```

Vous devriez voir les informations de version de FFMpeg.

---

## 📋 Formats supportés

| Format | Type | Qualité | Taille fichier | FFMpeg requis ? |
|--------|------|---------|----------------|-----------------|
| **MP3** | Avec perte | Bonne | Petite | ❌ Non |
| **WAV** | Sans perte | Maximale | Grande | ❌ Non |
| **FLAC** | Sans perte | Maximale | Moyenne | ✅ Oui |
| **WMA** | Avec perte | Bonne | Petite | ✅ Oui |

---

## ❓ Questions fréquentes

**Q : Pourquoi FFMpeg est-il nécessaire ?**  
R : FFMpeg est un outil open-source très puissant pour encoder/décoder de nombreux formats audio et vidéo. AudioPlay l'utilise pour les formats FLAC et WMA.

**Q : FFMpeg est-il gratuit ?**  
R : Oui, FFMpeg est totalement gratuit et open-source (licence LGPL/GPL).

**Q : Quelle est la taille de FFMpeg ?**  
R : Le fichier `ffmpeg.exe` fait environ 100-130 MB.

**Q : Est-ce que mes données sont envoyées quelque part ?**  
R : Non, tout le processus d'encodage est effectué localement sur votre ordinateur. Aucune donnée n'est envoyée sur Internet.

---

## 🔗 Liens utiles

- Site officiel FFMpeg : https://ffmpeg.org/
- Documentation FFMpeg : https://ffmpeg.org/documentation.html
- Builds Windows (Gyan) : https://www.gyan.dev/ffmpeg/builds/

---

**AudioPlay** © 2026
