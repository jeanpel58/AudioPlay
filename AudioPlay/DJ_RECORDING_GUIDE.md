# 🎙️ Guide d'enregistrement DJ - AudioPlay

## 📋 Vue d'ensemble

Le module d'enregistrement DJ d'AudioPlay vous permet de capturer vos sessions de mixage en temps réel dans plusieurs formats audio professionnels.

---

## ✨ Fonctionnalités

### Formats supportés
- **WAV** (Lossless) - Qualité studio sans perte
- **MP3** - 128/192/256/320 kbps
- **FLAC** (Prochainement) - Compression sans perte
- **WMA** (Prochainement) - Format Windows Media
- **AAC** (Prochainement) - Format avancé

### Caractéristiques
- ✅ Enregistrement en temps réel du mix complet (crossfader inclus)
- ✅ Nommage automatique avec timestamp (`DJ_Mix_20260101_120000.mp3`)
- ✅ Indicateur visuel de durée d'enregistrement
- ✅ Choix libre du répertoire de destination
- ✅ Sauvegarde du répertoire pour les prochaines sessions

---

## 🎯 Utilisation

### 1. Préparer l'enregistrement

1. **Chargez vos pistes** sur les decks A et/ou B
2. **Choisissez le format** dans le menu déroulant (en bas à droite du mixeur)
   - Par défaut : **MP3 320 kbps** (excellent compromis qualité/taille)

### 2. Démarrer l'enregistrement

1. **Cliquez sur le bouton ⬤ REC** (rouge, en bas à gauche du mixeur)
2. **Première utilisation** : Une fenêtre s'ouvrira pour choisir le dossier de destination
   - Exemple : `C:\Users\VotreNom\Documents\AudioPlay\Recordings\`
3. **Confirmation** : Un message indique le début de l'enregistrement
4. **Indicateur actif** :
   - Bouton devient **⬛ STOP** (gris)
   - Timer de durée apparaît sous le bouton
   - **Format verrouillé** pendant l'enregistrement

### 3. Mixer normalement

- **Tous les contrôles fonctionnent** :
  - Crossfader
  - Volume de chaque deck
  - Pitch/Tempo
  - Effets (Phaser, Reverb, Echo)
  - Beat Sync
- **Le mix complet est enregistré** tel que vous l'entendez

### 4. Arrêter l'enregistrement

1. **Cliquez sur ⬛ STOP**
2. **Confirmation** :
   - Message affiche la durée totale
   - Nom du fichier créé
   - Proposition d'ouvrir le dossier
3. **Ouvrir le dossier** (optionnel) :
   - Cliquez "Oui" pour voir le fichier dans l'explorateur

---

## ⚙️ Paramètres avancés

### Changer le répertoire d'enregistrement

**Pendant qu'aucun enregistrement n'est actif** :
1. Maintenez **Shift** enfoncé
2. Cliquez sur **⬤ REC**
3. Choisissez un nouveau dossier

### Qualité MP3

| Débit | Qualité | Taille (pour 1h) | Usage |
|-------|---------|------------------|-------|
| 128 kbps | Correcte | ~57 MB | Partage web |
| 192 kbps | Bonne | ~86 MB | Écoute standard |
| 256 kbps | Très bonne | ~115 MB | Qualité élevée |
| **320 kbps** | **Excellente** | **~144 MB** | **Recommandé** |

### Format WAV vs MP3

| Format | Avantages | Inconvénients |
|--------|-----------|---------------|
| **WAV** | ✅ Qualité parfaite<br>✅ Aucune perte<br>✅ Compatible universel | ❌ Fichiers très lourds<br>❌ ~600 MB/heure |
| **MP3 320** | ✅ Qualité excellente<br>✅ Taille raisonnable<br>✅ Compatible partout | ❌ Compression avec perte<br>(imperceptible à 320 kbps) |

---

## 📂 Organisation des fichiers

### Structure par défaut
```
Documents/
└── AudioPlay/
	└── Recordings/
		├── DJ_Mix_20260602_143000.mp3
		├── DJ_Mix_20260602_150530.mp3
		└── DJ_Mix_20260602_183245.mp3
```

### Nommage automatique
Format : `DJ_Mix_AAAAMMJJ_HHMMSS.extension`
- **AAAAMMJJ** : Date (2026-06-02)
- **HHMMSS** : Heure de début (14:30:00)
- **Extension** : `.mp3`, `.wav`, `.flac`, etc.

---

## 💡 Astuces professionnelles

### 🎚️ Avant d'enregistrer
1. **Testez vos niveaux** : Vérifiez que les VU-mètres ne saturent pas (rouge)
2. **Préparez votre setlist** : Chargez vos pistes dans la playlist DJ
3. **Vérifiez l'espace disque** :
   - MP3 320 : ~144 MB/heure
   - WAV : ~600 MB/heure

### 🎧 Pendant l'enregistrement
- **Ne fermez pas AudioPlay** : L'enregistrement s'arrêterait
- **Le timer indique la durée** : Gardez un œil dessus
- **Tous les effets sont capturés** : Ce que vous entendez = ce qui est enregistré

### 💾 Après l'enregistrement
1. **Sauvegardez** vos meilleurs mixes ailleurs (cloud, disque externe)
2. **Nommez manuellement** (optionnel) : Ajoutez un titre explicite
   - Exemple : `DJ_Mix_20260602_143000_HouseSummer2026.mp3`
3. **Partagez** : Les fichiers sont prêts pour SoundCloud, Mixcloud, etc.

---

## 🔧 Dépannage

### Le bouton REC ne fait rien
- ✅ **Chargez au moins une piste** sur un deck avant

### L'enregistrement ne démarre pas
- ✅ Vérifiez les **permissions du dossier** choisi
- ✅ Vérifiez l'**espace disque disponible**

### Le fichier est vide ou corrompu
- ✅ **Arrêtez proprement** : Utilisez le bouton STOP (ne fermez pas brutalement)
- ✅ **Vérifiez la RAM** : Les enregistrements longs nécessitent de la mémoire

### Format non disponible (FLAC/WMA/AAC)
- ⚠️ Ces formats sont **en développement**
- ✅ Utilisez **WAV** (lossless) ou **MP3 320** (excellente qualité) en attendant

---

## 🚀 Prochaines améliorations

### Version future
- [ ] **FLAC** : Compression lossless (taille divisée par 2 vs WAV)
- [ ] **AAC** : Meilleure qualité que MP3 à bitrate équivalent
- [ ] **WMA** : Format Windows Media
- [ ] **Normalisation automatique** : Niveaux audio optimisés
- [ ] **Split automatique** : Découpe par pistes (détection de silence)
- [ ] **Métadonnées** : Artist, Title, Date dans les fichiers
- [ ] **Upload direct** : SoundCloud, Mixcloud intégration

---

## 📞 Support

### Besoin d'aide ?
- 📖 Consultez le **Guide complet AudioPlay**
- 🎛️ Mode DJ : Voir **DJ_MODE_GUIDE_USER**
- 💬 Questions : Contactez le support AudioPlay

---

**AudioPlay © 2026 - Enregistrez vos meilleurs mixes ! 🎵**
