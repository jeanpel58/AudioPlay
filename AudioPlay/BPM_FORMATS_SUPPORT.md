# Support BPM pour différents formats audio

## Vue d'ensemble

L'application peut **détecter** et **sauvegarder** le BPM pour la plupart des formats audio courants.

---

## ✅ Formats ENTIÈREMENT supportés

### **MP3 (MPEG)**
- **Détection** : ✅ Oui (NAudio + librosa/SoundTouch)
- **Sauvegarde** : ✅ Oui (ID3v2 tag - frame TBPM)
- **Lecture** : ✅ Oui
- **Note** : Support complet et testé

### **FLAC**
- **Détection** : ✅ Oui (NAudio + librosa/SoundTouch)
- **Sauvegarde** : ✅ Oui (Vorbis Comment - champ BPM)
- **Lecture** : ✅ Oui
- **Note** : Excellent support, format sans perte

### **OGG Vorbis**
- **Détection** : ✅ Oui (NAudio + librosa/SoundTouch)
- **Sauvegarde** : ✅ Oui (Vorbis Comment - champ BPM)
- **Lecture** : ✅ Oui
- **Note** : Support complet via Vorbis Comment

### **M4A / AAC / MP4**
- **Détection** : ✅ Oui (NAudio + librosa/SoundTouch)
- **Sauvegarde** : ✅ Oui (iTunes metadata - atom tmpo)
- **Lecture** : ✅ Oui
- **Note** : Format Apple, bien supporté

### **WMA (Windows Media Audio)**
- **Détection** : ✅ Oui (NAudio + librosa/SoundTouch)
- **Sauvegarde** : ✅ Oui (ASF metadata)
- **Lecture** : ✅ Oui
- **Note** : Format Microsoft, support natif

### **APE (Monkey's Audio)**
- **Détection** : ✅ Oui (NAudio + librosa/SoundTouch)
- **Sauvegarde** : ✅ Oui (APEv2 tag)
- **Lecture** : ✅ Oui
- **Note** : Format sans perte

---

## ⚠️ Formats PARTIELLEMENT supportés

### **WAV (Waveform Audio)**
- **Détection** : ✅ Oui (NAudio + librosa/SoundTouch)
- **Sauvegarde** : ⚠️ Limité (ID3v2 ou INFO chunks)
- **Lecture** : ⚠️ Limité
- **Note** : WAV n'a pas de standard de métadonnées unifié
- **Recommandation** : Convertir en FLAC pour conserver les métadonnées

### **AIFF / AIF (Audio Interchange File Format)**
- **Détection** : ✅ Oui (NAudio + librosa/SoundTouch)
- **Sauvegarde** : ⚠️ Limité (peut utiliser ID3v2)
- **Lecture** : ⚠️ Limité
- **Note** : Format Apple, métadonnées limitées
- **Recommandation** : Convertir en M4A pour métadonnées complètes

---

## ❌ Formats NON supportés

### **AIM**
- **Détection** : ❌ Non
- **Sauvegarde** : ❌ Non
- **Note** : Format rare et peu documenté, non supporté par TagLib#

---

## 🔧 Comment ça fonctionne ?

### 1. **Détection du BPM**
L'application utilise **NAudio** pour lire tous les formats audio supportés :
- **Librosa** (Python) : Analyse précise du tempo sur tout le fichier
- **SoundTouch** (C++) : Analyse rapide en fallback

### 2. **Sauvegarde des métadonnées**
L'application utilise **TagLib#** pour écrire le BPM :
- Propriété universelle : `Tag.BeatsPerMinute`
- Adaptation automatique selon le format (ID3v2, Vorbis Comment, iTunes, etc.)

### 3. **Lecture des métadonnées**
- Lecture automatique au chargement de la playlist
- Affichage dans la colonne BPM avec 2 décimales

---

## 💡 Recommandations

### Pour une **compatibilité maximale** :
1. **MP3** : Format universel, excellent support BPM
2. **FLAC** : Sans perte + métadonnées complètes
3. **M4A** : Bon compromis qualité/taille + métadonnées

### Pour éviter les problèmes :
- ❌ **Évitez WAV** pour stocker des métadonnées BPM
- ❌ **Évitez AIFF** si vous voulez des métadonnées complètes
- ✅ **Préférez FLAC** si vous voulez sans perte + métadonnées

---

## 📊 Tableau récapitulatif

| Format | Extension | Détection BPM | Sauvegarde BPM | Lecture BPM | Qualité |
|--------|-----------|---------------|----------------|-------------|---------|
| MP3    | .mp3      | ✅ Oui        | ✅ Oui         | ✅ Oui      | Avec perte |
| FLAC   | .flac     | ✅ Oui        | ✅ Oui         | ✅ Oui      | Sans perte |
| OGG    | .ogg      | ✅ Oui        | ✅ Oui         | ✅ Oui      | Avec perte |
| M4A    | .m4a, .aac, .mp4 | ✅ Oui | ✅ Oui    | ✅ Oui      | Avec perte |
| WMA    | .wma      | ✅ Oui        | ✅ Oui         | ✅ Oui      | Avec perte |
| APE    | .ape      | ✅ Oui        | ✅ Oui         | ✅ Oui      | Sans perte |
| WAV    | .wav      | ✅ Oui        | ⚠️ Limité      | ⚠️ Limité   | Sans perte |
| AIFF   | .aif, .aiff | ✅ Oui      | ⚠️ Limité      | ⚠️ Limité   | Sans perte |
| AIM    | .aim      | ❌ Non        | ❌ Non         | ❌ Non      | ? |

---

## 🛠️ Tests effectués

Pour vérifier le support, l'application écrit dans les logs :
```
BPM sauvegardé dans MP3 (ID3v2): 128.45
BPM sauvegardé dans FLAC (Vorbis Comment): 174.32
BPM lu depuis MpegFile: 128
```

Consultez la fenêtre **Output → Debug** dans Visual Studio pour voir les logs.

---

## ❓ Questions fréquentes

### Q: Pourquoi WAV a un support limité ?
**R:** WAV est un format conteneur brut sans standard de métadonnées unifié. Certains logiciels utilisent ID3v2, d'autres des chunks INFO. La compatibilité n'est pas garantie entre applications.

### Q: Puis-je convertir mes WAV en FLAC sans perte ?
**R:** Oui ! FLAC est un format sans perte avec métadonnées complètes. Utilisez un convertisseur comme **foobar2000** ou **dBpoweramp**.

### Q: Le BPM est-il compatible avec d'autres logiciels DJ ?
**R:** Oui ! Les métadonnées BPM sont standards :
- **Rekordbox** : Lit ID3v2 TBPM
- **Serato** : Lit ID3v2 TBPM
- **Traktor** : Lit ID3v2 TBPM et Vorbis Comment
- **VirtualDJ** : Lit tous les formats standards

---

**Dernière mise à jour** : Version avec support décimal (2 décimales)
