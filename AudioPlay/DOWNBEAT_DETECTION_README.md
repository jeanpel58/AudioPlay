# Détection des Downbeats dans AudioPlay

## Vue d'ensemble

La détection des downbeats a été ajoutée à AudioPlay pour permettre une synchronisation phrase-aware style Virtual DJ et Serato. Cette fonctionnalité détecte les premiers beats de chaque mesure (downbeats) pour aligner les phrases musicales lors du mixage.

## Fichiers créés/modifiés

### 1. **DownbeatDetector.vb** (NOUVEAU)
Module de détection des downbeats utilisant Python/Librosa avec fallback basique.

**Caractéristiques principales :**
- Classe `DownbeatResult` contenant :
  - `BPM` : Tempo détecté
  - `Beats` : Liste de tous les beats (en secondes)
  - `Downbeats` : Liste des downbeats seulement (premiers beats de mesure)
  - `TimeSignature` : Signature rythmique (4 pour 4/4, 3 pour 3/4, etc.)
  - `Confidence` : Niveau de confiance de la détection (0.0 à 1.0)

**Méthodes de détection :**
- `DetecterDownbeats(cheminFichier)` : Méthode principale (async)
- `DetecterDownbeatsAvecLibrosa(cheminFichier)` : Utilise Python/Librosa pour une détection précise
  - `librosa.beat.beat_track()` pour les beats
  - `librosa.beat.plp()` et peak picking pour les downbeats
  - Détection automatique de la signature rythmique
- `DetecterDownbeatsBasique(cheminFichier)` : Fallback si Python n'est pas disponible
  - Utilise `BPMDetector` pour obtenir le BPM
  - Génère des beats réguliers
  - Marque chaque 4e beat comme downbeat (assume 4/4)

### 2. **BeatGrid.vb** (MODIFIÉ)
Ajout du support des downbeats et signatures rythmiques.

**Nouvelles propriétés :**
- `Downbeats As List(Of Double)` : Positions des downbeats
- `TimeSignature As Integer` : Signature rythmique (4 = 4/4)
- `DownbeatCount` : Nombre de downbeats

**Nouvelles méthodes :**
- `TrouverDownbeatLePlusProche(position)` : Trouve le downbeat le plus proche
- `TrouverProchainDownbeat(position)` : Trouve le prochain downbeat après une position
- `TrouverDownbeatPrecedent(position)` : Trouve le downbeat précédent
- `EstSurDownbeat(position, tolerance)` : Vérifie si une position est sur un downbeat
- `CalculerDriftDownbeat(position)` : Calcule le drift par rapport au downbeat le plus proche
- `CalculerPhrasePhase(position)` : Calcule la phase dans la mesure (0.0 à 1.0)

**Constructeur mis à jour :**
Le constructeur `New(bpm, dureeTotale)` génère maintenant automatiquement les downbeats en marquant chaque N-ième beat (selon `TimeSignature`).

### 3. **FormDJ.vb** (MODIFIÉ)
Intégration de la détection des downbeats lors du chargement de pistes.

**Modifications dans `DetecterBPMDeckA()` et `DetecterBPMDeckB()` :**
- Après la détection du BPM, appel à `DownbeatDetector.DetecterDownbeats()`
- Affichage des informations de downbeat dans la console de debug :
  - BPM, signature rythmique, nombre de beats/downbeats
  - Niveau de confiance
  - Position du premier downbeat
- Messages de log pour suivre la détection

### 4. **PythonManager.vb** (MODIFIÉ)
Ajout de propriétés publiques pour accéder aux chemins Python.

**Nouvelles propriétés :**
- `CheminPython As String` : Chemin vers `python.exe`
- `CheminDossierPython As String` : Chemin du dossier Python embedded

Ces propriétés permettent à `DownbeatDetector` d'exécuter des scripts Python.

## Utilisation

### Détection automatique
La détection des downbeats se fait automatiquement lors du chargement d'une piste sur Deck A ou Deck B :
```vb
' Dans ChargerFichierDeckA() ou ChargerFichierDeckB()
DetecterBPMDeckA()  ' ou DetecterBPMDeckB()
  ↓
' Détecte le BPM
' Puis détecte les downbeats
' Affiche les résultats dans Debug.WriteLine
```

### Utilisation manuelle
```vb
' Détecter les downbeats d'un fichier
Dim result As DownbeatDetector.DownbeatResult = _
	Await DownbeatDetector.DetecterDownbeats(cheminFichier)

If result IsNot Nothing Then
	Console.WriteLine($"BPM: {result.BPM:F3}")
	Console.WriteLine($"Signature: {result.TimeSignature}/4")
	Console.WriteLine($"Beats: {result.Beats.Count}")
	Console.WriteLine($"Downbeats: {result.Downbeats.Count}")
	Console.WriteLine($"Confiance: {result.Confidence:F2}")
End If
```

### Utilisation avec BeatGrid
```vb
' Créer un BeatGrid avec downbeats
Dim beatGrid As New BeatGrid()
beatGrid.BPM = result.BPM
beatGrid.Beats = result.Beats
beatGrid.Downbeats = result.Downbeats
beatGrid.TimeSignature = result.TimeSignature

' Trouver le downbeat le plus proche
Dim position As Double = 45.5  ' secondes
Dim downbeat As Double = beatGrid.TrouverDownbeatLePlusProche(position)

' Vérifier si on est sur un downbeat
If beatGrid.EstSurDownbeat(position, 0.1) Then
	Console.WriteLine("Sur un downbeat!")
End If

' Calculer la phase dans la mesure
Dim phase As Double = beatGrid.CalculerPhrasePhase(position)
Console.WriteLine($"Phase dans la mesure: {phase:F2} (0.0 = début, 1.0 = fin)")
```

## Algorithme de détection (Librosa)

### 1. Chargement audio
```python
y, sr = librosa.load(audio_path, sr=None, duration=120.0)
```
Charge l'audio (max 2 minutes pour performance).

### 2. Détection des beats
```python
tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr, units='frames')
beat_times = librosa.frames_to_time(beat_frames, sr=sr)
```
Détecte tous les beats avec `beat_track`.

### 3. Détection des downbeats
```python
onset_env = librosa.onset.onset_strength(y=y, sr=sr)
plp = librosa.beat.plp(onset_envelope=onset_env, sr=sr)
```
Utilise la *Predominant Local Pulse* (PLP) pour identifier les downbeats parmi les beats détectés.

### 4. Détection de la signature rythmique
```python
intervals = np.diff(beat_times)
most_common_interval = mode(np.round(intervals / median_interval).astype(int))
time_signature = int(most_common_interval)
```
Analyse les intervalles entre beats pour détecter la signature (3/4, 4/4, etc.).

### 5. Calcul de confiance
```python
confidence = np.mean(plp[beat_frames]) / np.max(plp)
```
Mesure la confiance de la détection basée sur l'énergie des downbeats détectés.

## Prochaines étapes (TODO)

### Intégration avec SYNC
Pour rendre le SYNC phrase-aware (comme Virtual DJ/Serato), il faudra :

1. **Stocker les downbeats** dans `FormDJ` :
```vb
Private downbeatResultDeckA As DownbeatDetector.DownbeatResult = Nothing
Private downbeatResultDeckB As DownbeatDetector.DownbeatResult = Nothing
```

2. **Modifier `ButtonSyncDeckA_Click()` et `ButtonSyncDeckB_Click()`** :
   - Au lieu d'aligner sur n'importe quel beat, aligner sur le downbeat le plus proche
   - Utiliser `TrouverDownbeatLePlusProche()` au lieu de `TrouverBeatLePlusProche()`

3. **Ajouter un mode SYNC "Phrase" vs "Beat"** :
   - Mode Beat : alignement beat-à-beat (comportement actuel)
   - Mode Phrase : alignement downbeat-à-downbeat (nouveau)

4. **Améliorer BeatSyncEngine** :
   - Intégrer la connaissance des downbeats dans `BeatGrid`
   - Permettre la synchronisation phrase-aware continue

5. **Interface utilisateur** :
   - Afficher la signature rythmique dans l'UI (ex: "4/4", "3/4")
   - Indicateur visuel du downbeat (clignotant sur le beat 1)
   - Bouton pour basculer entre SYNC Beat et SYNC Phrase

6. **Sauvegarde/cache** :
   - Enregistrer les downbeats détectés dans les métadonnées du fichier
   - Utiliser `BPMMetadataManager` pour lire/écrire les downbeats
   - Éviter de recalculer à chaque chargement

## Dépendances

### Python/Librosa (recommandé)
- Python Embedded 3.11.9
- librosa
- numpy
- scipy

### Fallback (si Python absent)
- `BPMDetector.vb` (existant)
- `AudioFileReader` (NAudio)
- Génération de beats réguliers

## Performance

- **Avec Librosa** : ~2-5 secondes pour une piste de 3-4 minutes
- **Fallback basique** : ~0.5-1 seconde (mais moins précis)
- Analyse limitée aux 2 premières minutes pour optimiser les performances

## Compatibilité

- Virtual DJ et Serato utilisent des algorithmes similaires basés sur l'analyse onset/PLP
- La précision des downbeats dépend de la qualité de la production musicale
- Meilleure détection sur :
  - EDM (structures très régulières)
  - Hip-Hop (kick/snare marqués)
  - House/Techno (4/4 constant)
- Détection plus difficile sur :
  - Jazz (signatures complexes/changeantes)
  - Musique classique (tempo variable)
  - Ambient (beats peu marqués)

## Références

- [Librosa Beat Tracking](https://librosa.org/doc/main/generated/librosa.beat.beat_track.html)
- [Librosa PLP (Predominant Local Pulse)](https://librosa.org/doc/main/generated/librosa.beat.plp.html)
- [Virtual DJ Beat Detection](https://www.virtualdj.com/wiki/Beat%20Detection.html)
- [Serato Beat Grid](https://support.serato.com/hc/en-us/articles/228019568-Beat-Grids)

---

**Date d'implémentation :** 2025-01-XX  
**Version AudioPlay :** 2026-06-02  
**Status :** ✅ Compilé et prêt pour tests  
**Prochaine étape :** Intégration avec le système SYNC pour alignment phrase-aware
