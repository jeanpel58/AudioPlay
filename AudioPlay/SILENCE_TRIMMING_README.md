# Suppression automatique des silences

## Vue d'ensemble
Cette fonctionnalité permet de supprimer automatiquement les silences au début et/ou à la fin de chaque chanson pour une expérience d'écoute plus fluide.

## Activation

### Via les Paramètres
1. Ouvrir **Paramètres** (bouton dans l'application)
2. Dans la section **Paramètres de lecture** :
   - ☑️ **Supprimer les silences au début de chaque chanson** : La chanson démarre immédiatement au premier son audible
   - ☑️ **Supprimer les silences à la fin de chaque chanson** : La prochaine chanson démarre immédiatement sans attendre
3. Cliquer sur **Sauvegarder**

## Fonctionnement

### Suppression du silence au début
- **Détection automatique** : analyse les échantillons audio en temps réel
- **Seuil de détection** : 1% de l'amplitude maximale (0.01)
- **Pas de manipulation de fichier** : tout se fait pendant la lecture (via `SkipSilenceSampleProvider`)
- **Compatible avec tous les formats** : WAV, MP3, FLAC, AAC, OGG, etc.

### Suppression du silence à la fin
- **Buffer circulaire** : garde en mémoire les dernières 0.5 secondes d'audio
- **Détection en fin de stream** : analyse le buffer quand le fichier se termine
- **Transition fluide** : la chanson suivante démarre immédiatement
- **Pas d'impact sur la qualité** : aucune modification du fichier original

## Interaction avec le métronome

Si le **métronome est activé** :
- La suppression du silence au début est **automatiquement activée**
- Garantit que la chanson démarre immédiatement après les beats
- Pas besoin d'activer manuellement l'option

## Avantages

### 🎵 Expérience d'écoute améliorée
- Pas d'attente entre les chansons
- Transitions naturelles dans les playlists
- Idéal pour les mix DJ ou les albums live

### ⚡ Performance
- Détection en temps réel (pas de pré-analyse)
- Faible utilisation mémoire
- Aucun impact sur la qualité audio

### 🔧 Flexibilité
- Activation/désactivation indépendante (début et/ou fin)
- Fonctionne avec ou sans métronome
- Compatible avec toutes les autres fonctionnalités (égaliseur, normalisation, etc.)

## Technique

### Architecture

```
AudioFileReader → ToSampleProvider() 
				↓
	SkipSilenceSampleProvider (si activé)
				↓
	TrimEndSilenceSampleProvider (si activé)
				↓
	MetronomeAudioSequencer (si métronome actif)
				↓
	SimpleEqualizerProvider
				↓
	VolumeSampleProvider
				↓
	WaveOutEvent
```

### Classes créées

#### `SkipSilenceSampleProvider`
- **Fonction** : ignore les échantillons silencieux au début
- **Méthode** : analyse chaque buffer lors de la première lecture
- **Décalage** : déplace les échantillons valides au début du buffer
- **Remplissage** : lit davantage si nécessaire pour remplir le buffer

#### `TrimEndSilenceSampleProvider`
- **Fonction** : supprime les silences à la fin
- **Buffer circulaire** : `Queue<Single>` de 0.5 secondes
- **Détection** : analyse inversée depuis la fin quand le stream se termine
- **Sortie** : retourne seulement les échantillons jusqu'au dernier son

## Paramètres techniques

### Seuil de silence
- **Valeur par défaut** : 0.01 (1% de l'amplitude max)
- **Modifiable** : dans le constructeur des classes (pour développeurs)
- **Équilibre** : assez sensible pour détecter le silence, assez tolérant pour le bruit de fond

### Durée minimale de silence à la fin
- **Valeur par défaut** : 0.5 secondes
- **Raison** : éviter de couper les réverbérations ou fades naturels
- **Modifiable** : paramètre `dureeSilenceMin` dans le constructeur

## Notes importantes

### ⚠️ Limitations
- Les silences **intentionnels** (pauses artistiques) sont aussi supprimés
- Les **fades out très longs** peuvent être tronqués
- Les **bruits de fond faibles** peuvent être considérés comme du silence

### 💡 Recommandations
- Tester avec votre musique avant d'activer globalement
- Idéal pour les **podcasts** et **livres audio** avec silences techniques
- Parfait pour les **playlists de fête** sans interruption
- Moins adapté pour la **musique classique** avec silences expressifs

## Persistance

Les paramètres sont sauvegardés dans `parametres.txt` :
```
SupprimerSilenceDebut=True
SupprimerSilenceFin=True
```

Chargés automatiquement au démarrage de l'application.

## Compatibilité

✅ Compatible avec :
- Tous les formats audio supportés (MP3, FLAC, WAV, AAC, OGG, WMA)
- Égaliseur et réglages basses/aigues
- Normalisation du volume
- Métronome pré-roll
- Lecture en continu et mode aléatoire
- Lecture stéréo et mono

## Futur

Améliorations possibles :
- Seuil de silence ajustable dans l'interface
- Prévisualisation des silences détectés
- Option "silence minimum" pour garder les pauses courtes
- Détection de silence intelligente (apprentissage du bruit de fond)
