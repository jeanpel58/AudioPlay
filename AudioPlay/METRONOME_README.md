# Fonctionnalité Métronome Pre-Roll

## Vue d'ensemble
Cette fonctionnalité permet d'ajouter un décompte métronome avant chaque chanson, synchronisé au BPM de la piste audio.

## Caractéristiques principales

### 1. Activation du métronome
- Accessible via **Paramètres** > **Checkbox "Métronome avant lecture"**
- Permet d'activer/désactiver le métronome pré-roll

### 2. Configuration du nombre de beats
- **Champ texte "Nombre de beats (1-16)"** dans les paramètres
- Valeur par défaut : 4 beats
- Plage autorisée : 1 à 16 beats

### 3. Fonctionnement

#### Détection du BPM
- Le métronome utilise le BPM stocké dans les métadonnées de la chanson
- Si aucun BPM n'est détecté, utilise 120 BPM par défaut
- Le BPM peut être calculé via le menu BPM de l'application

#### Génération du son
- Premier beat : fréquence plus aiguë (1000 Hz) pour marquer le départ
- Beats suivants : fréquence standard (800 Hz)
- Durée de chaque click : 50ms avec envelope pour éviter les clics parasites
- Volume : 30% de l'amplitude maximale

#### Suppression du silence
- Détecte automatiquement le silence au début de chaque chanson
- Seuil de détection : 0.01 (1% de l'amplitude maximale)
- Supprime automatiquement plus de 100ms de silence
- La chanson démarre immédiatement après le dernier beat du métronome

### 4. Séquence de lecture

1. **Métronome activé** :
   - Démarrage du métronome (X beats au BPM de la chanson)
   - Détection et suppression du silence initial
   - Démarrage immédiat de la chanson

2. **Métronome désactivé** :
   - Lecture directe de la chanson (comportement normal)

## Architecture technique

### Classes créées

#### `MetronomeProvider`
- Implémente `ISampleProvider` de NAudio
- Génère les clicks de métronome en fonction du BPM et du nombre de beats
- Format : mono, IEEE Float
- Signale automatiquement la fin de la séquence via `EstTermine`

#### `SilenceDetector`
- Analyse les fichiers audio pour détecter le premier échantillon non-silencieux
- Méthode `TrouverDebutAudio()` : retourne le TimeSpan du début du son
- Méthode `AppliquerOffsetSilence()` : repositionne le lecteur après le silence

#### `MetronomeAudioSequencer`
- Combine le métronome et l'audio principal en séquence
- Gère automatiquement la conversion stéréo → mono si nécessaire
- Passe du métronome à l'audio quand `MetronomeProvider.EstTermine` est vrai

### Intégration dans Form1

#### Variables persistantes
```vb
Private metronomeActif As Boolean = False
Private nombreBeatsMetronome As Integer = 4
```

#### Modification de la lecture
- `JouerItemSelectionne()` modifié pour :
  - Appliquer la suppression du silence si le métronome est actif
  - Créer le séquenceur métronome + audio si activé
  - Utiliser le BPM stocké dans le Tag de l'item ListView

#### Persistance
- Sauvegardé dans `parametres.txt`
- Chargé au démarrage de l'application

## Utilisation

1. Ouvrir **Paramètres** via le bouton correspondant
2. Cocher **"Métronome avant lecture"**
3. Entrer le nombre de beats souhaité (1-16)
4. Cliquer sur **Sauvegarder**
5. Lancer une chanson : le métronome joue avant le début

## Notes techniques

### Compatibilité audio
- Fonctionne avec tous les formats supportés par NAudio
- Conversion automatique stéréo → mono pour le métronome
- L'égaliseur et la normalisation de volume sont appliqués après le séquenceur

### Performance
- Détection du silence : analyse rapide en mémoire tampon
- Génération du métronome : calcul en temps réel, faible charge CPU
- Pas d'impact sur la qualité audio de la chanson

### Limitations actuelles
- Le BPM doit être présent dans les métadonnées ou calculé préalablement
- Si BPM = 0 ou absent, utilise 120 BPM par défaut
- Le métronome est toujours en mono (l'audio reste stéréo)

## Améliorations futures possibles

- Choix du type de son (click, woodblock, rim shot, etc.)
- Volume du métronome ajustable
- Accent configurable (premier beat, tous les X beats)
- Signature rythmique (4/4, 3/4, 6/8, etc.)
- Pré-écoute du métronome dans les paramètres
