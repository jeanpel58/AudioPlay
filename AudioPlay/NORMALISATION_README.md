# Normalisation du Volume - Option 1

## Fonctionnalité

Cette fonctionnalité permet de normaliser automatiquement le volume de tous les fichiers audio pour qu'ils aient un niveau de volume perçu similaire lors de la lecture.

## Comment ça fonctionne

### 1. Analyse automatique
- Quand un fichier audio est joué pour la première fois, le système analyse rapidement son volume (les 30 premières secondes)
- L'analyse calcule :
  - **Volume RMS (Root Mean Square)** : Le volume moyen perçu
  - **Volume Peak** : Le volume maximum
  - **Gain suggéré** : Le multiplicateur à appliquer pour normaliser

### 2. Application du gain
- Le gain calculé est appliqué en temps réel pendant la lecture
- Le gain est stocké dans la mémoire de l'application pour ne pas avoir à recalculer
- Le volume principal (trackbar) continue de fonctionner normalement

### 3. Limites de sécurité
- **Volume cible** : 80% du maximum (pour éviter la saturation)
- **Gain maximum** : 3x (pour éviter d'amplifier trop le bruit de fond)
- **Protection contre la saturation** : Si le peak * gain dépasse 95%, le gain est réduit

## Activation/Désactivation

### Dans les paramètres
1. Cliquez sur le bouton **Paramètres**
2. Dans la section "Paramètres de lecture"
3. Cochez ou décochez **"Normaliser le volume des fichiers audio (Option 1)"**
4. Cliquez sur **Sauvegarder**

### Par défaut
La normalisation est **activée par défaut**.

## Performance

### Analyse rapide
- L'analyse se fait sur les **30 premières secondes** du fichier
- Impact minimal sur le temps de démarrage de la lecture
- Le résultat est mis en cache pour les lectures suivantes

### Analyse complète (optionnel)
Une méthode `AnalyserFichier` existe pour analyser tout le fichier, mais elle n'est pas utilisée par défaut pour des raisons de performance.

## Architecture technique

### Fichiers modifiés
- **VolumeNormalizer.vb** : Nouvelle classe pour l'analyse du volume
- **Form1.vb** : Intégration de la normalisation dans le pipeline de lecture
- **FormParametres.vb** : Ajout du paramètre pour activer/désactiver
- **FormParametres.Designer.vb** : Ajout du CheckBox dans l'interface

### Pipeline audio
```
AudioFileReader 
  → ToSampleProvider
  → SimpleEqualizerProvider (Basses/Aigues)
  → VolumeSampleProvider (Volume * Gain de normalisation)
  → WaveOutEvent
```

### Stockage du gain
- Le gain calculé est stocké dans le `Tag` de chaque `ListViewItem`
- Format : `Dictionary(Of String, Object)` avec les clés :
  - `"Chemin"` : Chemin du fichier
  - `"GainNormalisation"` : Gain calculé (Single)

## Avantages de cette approche

✅ **Temps réel** : Pas besoin de modifier les fichiers MP3 originaux  
✅ **Rapide** : Analyse seulement les 30 premières secondes  
✅ **Sûr** : Limitations pour éviter la saturation et le bruit  
✅ **Persistant** : Le gain est recalculé seulement si nécessaire  
✅ **Compatible** : Fonctionne avec le volume principal et l'égaliseur  

## Notes pour le développeur

### Personnalisation
Vous pouvez ajuster les constantes dans `VolumeNormalizer.vb` :
- `VOLUME_CIBLE` : Volume cible (actuellement 0.8 = 80%)
- Limite de gain maximum (actuellement 3.0)
- Durée d'analyse rapide (actuellement 30 secondes)

### Extension future
- Ajouter une analyse en arrière-plan de tous les fichiers de la playlist
- Sauvegarder les gains dans un fichier cache pour les sessions futures
- Permettre à l'utilisateur de choisir entre analyse rapide et complète
- Afficher le gain calculé dans les métadonnées du fichier
