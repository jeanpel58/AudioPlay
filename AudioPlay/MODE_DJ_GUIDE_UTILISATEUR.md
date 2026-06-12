# 🎧 MODE MIXEUR DJ - GUIDE UTILISATEUR

## 📖 Comment activer le Mode Mixeur DJ

### Étape 1 : Ouvrir les Paramètres
1. Lancez **AudioPlay**
2. Cliquez sur le bouton **⚙️ Paramètres** (en bas à droite)

### Étape 2 : Activer le Mode DJ
1. Dans la section **"Paramètres de lecture"**
2. Cochez la case **"Mode Mixeur DJ (2 platines avec crossfader et contrôles DJ)"**
3. Un message d'information apparaît expliquant que le mode sera activé après redémarrage
4. Cliquez sur **"Sauvegarder"**

### Étape 3 : Redémarrer AudioPlay
1. Fermez AudioPlay
2. Relancez l'application
3. **FormDJ** (Mode Mixeur DJ) s'affiche automatiquement à la place du lecteur simple

---

## 🎛️ Utilisation du Mode DJ

### Interface
```
┌─────────────────────────────────────────────────────┐
│            PLATINE A          PLATINE B             │
│         ┌─────────────┐    ┌─────────────┐         │
│         │  DECK A 🎧  │    │  DECK B 🎧  │         │
│         └─────────────┘    └─────────────┘         │
│                                                     │
│         ┌─────────────┐    ┌─────────────┐         │
│         │  MIXEUR 🎚️  │                            │
│         └─────────────┘                             │
└─────────────────────────────────────────────────────┘
```

### Charger des fichiers
- **Platine A** : Cliquez sur **"📁 Charger fichier"** (gauche)
- **Platine B** : Cliquez sur **"📁 Charger fichier"** (droite)
- Formats supportés : MP3, WAV, FLAC, AAC, WMA

### Contrôles de lecture

#### Boutons principaux
- **▶** : Lecture / Pause (toggle)
- **CUE** :
  - **Pendant la lecture** : Définit un point Cue à la position actuelle
  - **Si arrêté** : Retourne au point Cue
- **⏹** : Arrête la lecture et retourne au début

#### Contrôles de mix
- **Volume Deck A / B** : Ajuste le volume individuel de chaque platine (0-100%)
- **Crossfader** : Mixe entre les deux platines
  - **← Gauche (0%)** : 100% Platine A
  - **■ Centre (50%)** : Mix équilibré
  - **→ Droite (100%)** : 100% Platine B
  - Courbe DJ : Coupe agressive au centre pour transitions professionnelles

#### Pitch Control
- **Pitch Deck A / B** : Ajuste la vitesse/hauteur (±8%)
  - **92-100** : Ralentit
  - **100** : Vitesse normale
  - **100-108** : Accélère
  - ⚠️ *Note* : Affichage uniquement pour l'instant (application audio à venir)

### Affichages
- **Nom de piste** : Affiche le fichier chargé
- **Durée** : Position / Durée totale (mm:ss)
- **BPM** : Beats par minute (à venir)
- **Pitch** : Pourcentage de modification tempo

---

## 🔄 Retour au Mode Simple

### Méthode 1 : Depuis FormDJ (rapide)
1. Cliquez sur **"◀ Mode Simple"** (en bas à gauche)
2. Confirmez le redémarrage
3. AudioPlay redémarre en mode lecteur simple

### Méthode 2 : Via les Paramètres (manuel)
1. *(Non recommandé car FormDJ doit être ouvert)*
2. Fermez AudioPlay
3. Ouvrez le fichier de configuration manuellement :
   - Chemin : `C:\Users\[VotreNom]\AppData\Roaming\AudioPlay\parametres.txt`
   - Changez `ModeMixeurDJ=True` en `ModeMixeurDJ=False`
4. Relancez AudioPlay

---

## 🎯 Cas d'usage

### Mixing de base
1. Chargez une piste sur **Deck A**
2. Lancez la lecture
3. Pendant que A joue, chargez une piste sur **Deck B**
4. Démarrez **Deck B**
5. Utilisez le **crossfader** pour mixer progressivement de A vers B

### Utilisation du Cue
1. Lancez une piste sur **Deck A**
2. Pendant la lecture, trouvez le point de départ souhaité (intro, drop, etc.)
3. Cliquez sur **CUE** → Le point est sauvegardé
4. Arrêtez la lecture
5. Cliquez à nouveau sur **CUE** → Retour instantané au point sauvegardé
6. Relancez avec **▶** pour jouer depuis ce point

### Mix avec volumes
1. Deux pistes en lecture simultanée
2. **Crossfader au centre** (50%)
3. Ajustez **Volume A** et **Volume B** pour équilibrer
4. Diminuez progressivement **Volume A** tout en augmentant **Volume B**
5. Résultat : Transition douce sans utiliser le crossfader

---

## ⌨️ Raccourcis clavier (à venir)

*Phase 2 :*
- **Espace** : Play/Pause Deck actif
- **C** : Cue
- **S** : Stop
- **1-2** : Sélectionner Deck A/B
- **←→** : Crossfader gauche/droite
- **↑↓** : Volume Deck actif

---

## 🔧 Dépannage

### Le Mode DJ ne s'active pas
- ✅ Vérifiez que vous avez cliqué sur **"Sauvegarder"** dans les paramètres
- ✅ Vérifiez que vous avez **redémarré** AudioPlay après activation
- ✅ Vérifiez le fichier `parametres.txt` : `ModeMixeurDJ=True` doit être présent

### Pas de son
- ✅ Vérifiez que les **volumes individuels** ne sont pas à 0
- ✅ Vérifiez la position du **crossfader** (doit être proche du deck actif)
- ✅ Vérifiez le **volume système** Windows

### Crossfader ne fonctionne pas
- ✅ Les deux decks doivent avoir un fichier chargé
- ✅ Les volumes individuels doivent être > 0
- ✅ Testez en déplaçant le crossfader de 0% à 100%

### Le bouton "◀ Mode Simple" ne fonctionne pas
- ✅ Confirmez bien le message de redémarrage
- ✅ Si l'application ne redémarre pas automatiquement, fermez et relancez manuellement

---

## 🚀 Fonctionnalités à venir (Phase 2)

### Sync automatique
- Détection BPM des pistes
- Bouton **SYNC** pour aligner automatiquement les tempos
- Beat-matching automatique

### Effets par platine
- Reverb, Echo, Phaser individuels
- Contrôles FX compacts par deck

### Visualisation
- VU-mètres en temps réel
- Waveform (forme d'onde)
- Indicateur de phase (alignement beats)

### Amélioration UX
- Drag & Drop fichiers sur les platines
- Hotcues multiples (points Cue nommés)
- Timer de position automatique
- Playlists DJ par deck

---

## 📝 Notes importantes

⚠️ **Pitch control** : L'affichage du pitch fonctionne, mais la modification audio n'est pas encore implémentée (Phase 2)

⚠️ **Position TrackBar** : Le TrackBar de position ne se met pas encore à jour automatiquement pendant la lecture (Phase 2)

⚠️ **BPM** : L'affichage affiche "BPM: --" pour l'instant (détection à venir Phase 2)

✅ **Crossfader** : Utilise une courbe DJ professionnelle pour des transitions naturelles

✅ **Volumes** : Le volume de chaque deck est le produit du volume individuel × crossfader

✅ **Cue** : Fonctionne parfaitement pour marquer et retourner aux points clés

---

## 🎓 Conseils de DJ

### Transition réussie
1. Écoutez bien la piste A qui joue
2. Définissez un point Cue sur B au bon moment (début du beat, intro)
3. Lancez B en même temps que A (utilisez Cue pour synchroniser)
4. Mixez progressivement avec le crossfader
5. Ajustez les volumes si nécessaire

### Mix harmonique
1. Choisissez des pistes avec des BPM similaires (±5%)
2. Utilisez le pitch pour ajuster finement
3. Mixez pendant les parties instrumentales (pas pendant les voix)

### Utilisation du Cue comme Hotstart
1. Définissez le Cue au moment exact du drop
2. Arrêtez la lecture
3. Au bon moment, retour Cue + Play instantané
4. Effet "drop surprise" garanti !

---

**Amusez-vous bien avec le Mode Mixeur DJ ! 🎉🎧**

*Feedback et suggestions : N'hésitez pas à proposer des améliorations !*
