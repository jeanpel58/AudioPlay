# Effets Audio - AudioPlay

## Vue d'ensemble

AudioPlay intègre désormais un système complet d'effets audio en temps réel qui permet de modifier le son pendant la lecture sans affecter les fichiers originaux.

## Effets disponibles

### 1. Réverbération (Reverb)
- **Description** : Simule l'acoustique d'un espace en ajoutant des réflexions sonores
- **Paramètres** :
  - **Actif** : Active/désactive l'effet
  - **Mix** : Contrôle le mélange entre le signal original (dry) et l'effet (wet)
	- 0% = Son original uniquement
	- 100% = Effet uniquement
	- Valeur par défaut : 30%
- **Algorithme** : Utilise 6 lignes de délai avec des temps premiers (29, 37, 41, 43, 53, 61 ms) pour éviter les résonances et créer une réverbération naturelle

### 2. Écho (Echo)
- **Description** : Crée des répétitions du signal audio avec un délai configurable
- **Paramètres** :
  - **Actif** : Active/désactive l'effet
  - **Mix** : Contrôle le mélange entre le signal original et l'écho (0-100%, défaut : 30%)
  - **Délai** : Temps entre les répétitions (50-2000 ms, défaut : 300 ms)
  - **Feedback** : Quantité de signal réinjecté pour créer des répétitions multiples (0-90%, défaut : 50%)
	- Plus élevé = plus de répétitions
	- Maximum à 90% pour éviter l'emballement

### 3. Pitch Shift (Changement de tonalité)
- **Description** : Modifie la tonalité (hauteur) du son sans affecter le tempo
- **Paramètres** :
  - **Actif** : Active/désactive l'effet
  - **Demi-tons** : Décalage en demi-tons (-12 à +12)
	- -12 = Une octave plus bas
	- 0 = Pas de changement
	- +12 = Une octave plus haut
	- Valeur par défaut : 0
- **Utilisation** : Transposer une chanson dans une autre tonalité pour l'adapter à votre voix
- **Note** : Version simplifiée avec interpolation linéaire. Pour une qualité professionnelle, une intégration complète de SoundTouch serait nécessaire.

### 4. Time Stretch (Changement de tempo)
- **Description** : Modifie la vitesse de lecture (tempo) sans affecter la tonalité
- **Paramètres** :
  - **Actif** : Active/désactive l'effet
  - **Tempo** : Ratio de vitesse (0.50x à 2.00x)
	- 0.50x = Deux fois plus lent
	- 1.00x = Vitesse normale
	- 2.00x = Deux fois plus rapide
	- Valeur par défaut : 1.00x
- **Utilisation** : Ralentir pour apprendre une chanson, ou accélérer pour gagner du temps
- **Note** : Version simplifiée avec interpolation linéaire. Pour une qualité professionnelle, une intégration complète de SoundTouch serait nécessaire.

## Architecture technique

### Chaîne de traitement audio

Les effets sont appliqués dans l'ordre suivant :

```
AudioFileReader (fichierAudio)
  ↓
AudioSampleProvider (conversion en samples)
  ↓
[Métronome/Silence optionnel]
  ↓
SimpleEqualizerProvider (égaliseur)
  ↓
PitchShiftSampleProvider (changement de tonalité) [si actif]
  ↓
TimeStretchSampleProvider (changement de tempo) [si actif]
  ↓
ReverbSampleProvider (réverbération) [si actif]
  ↓
EchoSampleProvider (écho) [si actif]
  ↓
VolumeSampleProvider (volume final + normalisation)
  ↓
WaveOutEvent (sortie audio)
```

### Classes des effets

Les effets sont implémentés dans le dossier `AudioPlay/AudioEffects/` :

1. **ReverbSampleProvider.vb** : Implémente la réverbération avec plusieurs lignes de délai
2. **EchoSampleProvider.vb** : Implémente l'écho avec délai et feedback configurables
3. **PitchShiftSampleProvider.vb** : Implémente le changement de tonalité par rééchantillonnage
4. **TimeStretchSampleProvider.vb** : Implémente le changement de tempo par rééchantillonnage

Toutes les classes implémentent l'interface `ISampleProvider` de NAudio pour s'intégrer dans la chaîne audio.

### Stockage des paramètres

Les paramètres des effets sont stockés dans :
- **Module** : `ParametresGlobaux.vb` (variables globales)
- **Fichier** : `%AppData%\AudioPlay\parametres.txt` (persistance)

Format dans le fichier de configuration :
```
EffetReverbActif=False
EffetReverbMix=0.3
EffetEchoActif=False
EffetEchoMix=0.3
EffetEchoDelai=300
EffetEchoFeedback=0.5
EffetPitchActif=False
EffetPitchSemitones=0
EffetTimeStretchActif=False
EffetTimeStretchRatio=1.0
```

## Interface utilisateur

### Accès aux paramètres
1. Cliquer sur le bouton **Paramètres** dans l'interface principale
2. Faire défiler vers le bas jusqu'à la section **"Effets Audio"**

### Contrôles disponibles
- **Cases à cocher** : Activer/désactiver chaque effet
- **TrackBars (curseurs)** : Ajuster les paramètres de chaque effet
- **Labels** : Affichage en temps réel des valeurs sélectionnées
- **Bouton "Réinitialiser les effets"** : Remet tous les effets à leurs valeurs par défaut

### Couleurs du bouton "Réinitialiser"
- **Normal** : Blanc
- **Survol (hover)** : Vert (Lime)
- **Clic (mousedown)** : Rouge

## Utilisation

### Appliquer un effet

1. Ouvrir les **Paramètres**
2. Cocher la case de l'effet désiré (ex: "Réverbération (Reverb)")
3. Ajuster les paramètres avec les curseurs
4. Cliquer sur **Sauvegarder**
5. Relancer la lecture pour entendre l'effet

### Combiner plusieurs effets

Les effets peuvent être combinés :
- Activer plusieurs cases à cocher simultanément
- Les effets seront appliqués dans l'ordre de la chaîne de traitement
- Exemple : Pitch Shift + Time Stretch + Reverb pour un effet créatif

### Désactiver les effets

Deux méthodes :
1. **Méthode rapide** : Cliquer sur **"Réinitialiser les effets"** puis sauvegarder
2. **Méthode manuelle** : Décocher les cases des effets actifs puis sauvegarder

## Conseils d'utilisation

### Réverbération
- Pour un effet subtil : Mix à 20-30%
- Pour une ambiance de cathédrale : Mix à 50-70%
- Éviter 100% sauf pour des effets spéciaux

### Écho
- Écho court (50-150 ms) : Effet de doublement vocal
- Écho moyen (200-400 ms) : Écho classique
- Écho long (500+ ms) : Effet delay créatif
- Feedback à 50% : Environ 3-4 répétitions
- Feedback à 70% : Environ 6-8 répétitions

### Pitch Shift
- +2 à +4 demi-tons : Monter la chanson pour voix plus aiguë
- -2 à -4 demi-tons : Baisser la chanson pour voix plus grave
- ±12 demi-tons : Changement d'octave complet

### Time Stretch
- 0.75x : Ralentir de 25% pour apprendre
- 0.85-0.95x : Ralentissement subtil
- 1.10-1.25x : Accélération subtile
- 1.50-2.00x : Écoute rapide de podcasts/discours

## Performance

### Impact sur le CPU
- **Reverb** : Impact moyen (6 lignes de délai)
- **Echo** : Impact faible (1 ligne de délai)
- **Pitch Shift** : Impact moyen-élevé (rééchantillonnage)
- **Time Stretch** : Impact moyen-élevé (rééchantillonnage)

### Recommandations
- Activer uniquement les effets nécessaires
- Sur les machines moins puissantes, éviter d'activer les 4 effets simultanément
- Les effets sont appliqués en temps réel sans modifier les fichiers originaux

## Améliorations futures possibles

1. **Intégration complète de SoundTouch** : Améliorer la qualité du Pitch Shift et Time Stretch
2. **Préréglages (Presets)** : Sauvegarder des combinaisons d'effets favorites
3. **Égaliseur graphique avancé** : Plus de bandes de fréquences
4. **Compresseur dynamique** : Égaliser les niveaux sonores
5. **Limiteur** : Prévenir la saturation
6. **Visualisation en temps réel** : Spectrogramme, VU-mètre, etc.
7. **Effets additionnels** : Chorus, Flanger, Phaser, Distorsion

## Notes de version

### Version 1.26.06.01
- ✅ Implémentation de la réverbération (algorithme multi-délai)
- ✅ Implémentation de l'écho (délai + feedback)
- ✅ Implémentation du pitch shifting (version simplifiée)
- ✅ Implémentation du time stretching (version simplifiée)
- ✅ Interface utilisateur complète dans FormParametres
- ✅ Persistance des paramètres dans le fichier de configuration
- ✅ Intégration dans la chaîne audio existante
- ✅ Compatibilité avec tous les autres effets (égaliseur, normalisation, métronome)

---

**AudioPlay** - Lecteur audio professionnel avec effets temps réel
