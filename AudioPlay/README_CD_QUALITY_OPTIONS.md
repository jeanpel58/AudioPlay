# 📀 Options de qualité d'extraction CD - AudioPlay

Ce document décrit les options de qualité disponibles pour chaque format d'extraction CD dans AudioPlay.

## 🎵 MP3 (MPEG-1 Audio Layer 3)

Format compressé avec perte, universel et compatible avec tous les lecteurs.

| Qualité | Bitrate | Taille approximative (par minute) | Usage recommandé |
|---------|---------|-----------------------------------|------------------|
| **Très haute** ⭐ | 320 kbps | ~2.4 MB | Qualité maximale, audiophiles |
| Haute | 256 kbps | ~1.9 MB | Excellente qualité, usage général |
| Moyenne | 192 kbps | ~1.4 MB | Bonne qualité, économie d'espace |
| Basse | 128 kbps | ~0.96 MB | Qualité acceptable, espace limité |

**Par défaut** : Très haute (320 kbps)

**Encodeur** : LAME MP3 (via NAudio.Lame)

---

## 💎 FLAC (Free Lossless Audio Codec)

Format compressé **sans perte**. Qualité identique au CD original, fichiers plus petits que WAV.

| Qualité | Niveau de compression | Taille approximative | Vitesse d'encodage |
|---------|----------------------|----------------------|--------------------|
| **Niveau 8 (meilleur)** ⭐ | Compression maximale | ~50% du WAV | Lent |
| Niveau 5 (équilibré) | Compression moyenne | ~55% du WAV | Moyen |
| Niveau 0 (rapide) | Compression minimale | ~60% du WAV | Rapide |

**Par défaut** : Niveau 8 (meilleur)

**Encodeur** : FFMpeg (téléchargement automatique si absent)

**Note** : Plus le niveau est élevé, plus la compression est efficace (fichier plus petit) mais l'encodage est plus lent. La qualité audio reste **identique** quel que soit le niveau.

---

## 🎼 WAV (Waveform Audio File Format)

Format non compressé, qualité maximale. Supporte plusieurs résolutions.

| Qualité | Sample Rate | Bit Depth | Taille approximative (par minute) | Usage recommandé |
|---------|-------------|-----------|-----------------------------------|------------------|
| PCM 32-bit 192 kHz | 192,000 Hz | 32-bit | ~92 MB | Audio professionnel, mastering |
| **PCM 24-bit 96 kHz** ⭐ | 96,000 Hz | 24-bit | ~34 MB | Studio, archivage haute qualité |
| PCM 16-bit 44.1 kHz | 44,100 Hz | 16-bit | ~10 MB | Qualité CD standard |

**Par défaut** : PCM 24-bit 96 kHz

**Conversion** : MediaFoundationResampler (haute qualité, ResamplerQuality = 60)

**Note** : 
- Les CD audio sont en **16-bit 44.1 kHz** natif
- Les options 24-bit et 32-bit impliquent un **upsampling** (amélioration de la résolution)
- L'upsampling ne crée pas de nouvelles informations audio, mais offre une marge pour le traitement ultérieur

---

## 🎧 WMA (Windows Media Audio)

Format compressé propriétaire Microsoft, bonne qualité/compression.

| Qualité | Bitrate | Taille approximative (par minute) | Usage recommandé |
|---------|---------|-----------------------------------|------------------|
| **256 kbps** ⭐ | 256 kbps | ~1.9 MB | Qualité maximale |
| 192 kbps | 192 kbps | ~1.4 MB | Très bonne qualité |
| 128 kbps | 128 kbps | ~0.96 MB | Bonne qualité, économie d'espace |

**Par défaut** : 256 kbps

**Encodeur** : FFMpeg avec codec wmav2 (téléchargement automatique si absent)

---

## 🔧 Installation automatique de FFMpeg

AudioPlay nécessite **FFMpeg** pour les formats **FLAC** et **WMA**.

### Processus automatique :
1. 🔍 Détection automatique de l'absence de FFMpeg
2. 💬 Proposition de téléchargement à l'utilisateur
3. 📥 Téléchargement automatique (~120 MB)
4. 📂 Installation dans `Tools\ffmpeg.exe`
5. ✅ Extraction qui démarre automatiquement

### Emplacements de recherche :
- `Tools\ffmpeg.exe` (dossier de l'application)
- Répertoire de base de l'application
- Variable d'environnement PATH

---

## 📊 Comparaison rapide

| Format | Qualité | Taille (5 min) | Compatibilité | Vitesse |
|--------|---------|----------------|---------------|---------|
| **MP3 320 kbps** | Excellente | ~12 MB | ⭐⭐⭐⭐⭐ Universelle | ⭐⭐⭐⭐ Rapide |
| **FLAC Niveau 8** | Parfaite (lossless) | ~25 MB | ⭐⭐⭐ Bonne | ⭐⭐⭐ Moyen |
| **WAV 24-bit 96k** | Parfaite (non compressé) | ~170 MB | ⭐⭐⭐⭐ Très bonne | ⭐⭐⭐⭐⭐ Très rapide |
| **WMA 256 kbps** | Excellente | ~9.5 MB | ⭐⭐⭐ Moyenne (Windows) | ⭐⭐⭐ Moyen |

---

## 🎯 Recommandations d'usage

### 🎵 Pour l'écoute quotidienne
→ **MP3 320 kbps** ou **WMA 256 kbps**
- Excellente qualité audible
- Compatible partout
- Taille raisonnable

### 💿 Pour l'archivage
→ **FLAC Niveau 8**
- Qualité parfaite (identique au CD)
- Compression efficace (~50% de WAV)
- Aucune perte

### 🎚️ Pour le traitement audio / mastering
→ **WAV 24-bit 96 kHz** ou **WAV 32-bit 192 kHz**
- Résolution maximale
- Headroom pour le traitement
- Aucune compression

### 📱 Pour les appareils portables
→ **MP3 256 kbps** ou **MP3 192 kbps**
- Bon compromis qualité/taille
- Économie d'espace
- Batterie préservée

---

## 🔍 Notes techniques

### Calcul de la taille des fichiers

#### MP3 / WMA :
```
Taille (MB) = (Bitrate_kbps × Durée_secondes) / 8 / 1024
```

#### FLAC :
```
Taille (MB) ≈ Taille_WAV × (0.50 à 0.60)
```
*La compression varie selon la complexité du signal audio*

#### WAV :
```
Taille (MB) = (SampleRate × BitDepth × Channels × Durée_secondes) / 8 / 1024 / 1024
```

### CD Audio standard
- **Format natif** : PCM 16-bit stéréo 44.1 kHz
- **Bitrate non compressé** : 1,411.2 kbps
- **Taille par minute** : ~10 MB

---

## 📝 Métadonnées

Tous les formats supportent l'écriture de métadonnées via **TagLib** :
- ✅ Titre, Artiste, Album
- ✅ Année, Genre, Commentaire
- ✅ Numéro de piste
- ✅ Pochette d'album (cover art)

---

## 🆘 Dépannage

### ❌ FFMpeg ne s'installe pas
1. Vérifiez votre connexion Internet
2. Désactivez temporairement l'antivirus
3. Téléchargez manuellement depuis https://ffmpeg.org/
4. Placez `ffmpeg.exe` dans le dossier `Tools\`

### ⚠️ Extraction lente
- **FLAC/WMA** : Normal, l'encodage prend du temps
- **WAV upsampling** : Normal, le resampling est gourmand
- **Solution** : Choisir une qualité inférieure ou WAV 16-bit 44.1 kHz

### 🔊 Qualité audio insuffisante
- Augmentez le bitrate (MP3/WMA)
- Passez à FLAC pour une qualité parfaite
- Vérifiez l'état physique du CD (rayures)

---

## 📚 Ressources

- **NAudio** : https://github.com/naudio/NAudio
- **NAudio.Lame** : https://github.com/Corey-M/NAudio.Lame
- **FFMpeg** : https://ffmpeg.org/
- **TagLib** : https://github.com/mono/taglib-sharp

---

**AudioPlay** © 2024 - Extraction CD avec qualité professionnelle 🎵
