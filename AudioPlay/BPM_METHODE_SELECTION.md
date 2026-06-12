# Sélection de la méthode de calcul BPM

## Vue d'ensemble

AudioPlay vous permet maintenant de choisir la méthode utilisée pour calculer le BPM (Battements Par Minute) de vos fichiers audio. Vous avez le choix entre trois options :

1. **Auto (recommandé)** - Utilise Librosa si disponible, sinon SoundTouch
2. **Librosa uniquement** - Analyse plus précise (nécessite Python)
3. **SoundTouch uniquement** - Analyse plus rapide mais moins précise

## Configuration

### Accéder aux paramètres

1. Cliquez sur le bouton **Paramètres** dans l'interface principale
2. Dans la section **Paramètres de lecture**, trouvez le menu déroulant **Méthode de calcul BPM**
3. Sélectionnez la méthode de votre choix
4. Cliquez sur **Sauvegarder**

### Options disponibles

#### Auto (Librosa si disponible, sinon SoundTouch)

**Avantages :**
- Meilleure précision possible automatiquement
- Toujours fonctionnel (fallback sur SoundTouch si Librosa n'est pas disponible)
- Recommandé pour la plupart des utilisateurs

**Inconvénients :**
- Nécessite l'installation de Python Embedded (téléchargement automatique au premier usage)

**Utilisation :**
Cette option est le choix par défaut. L'application essaiera d'utiliser Librosa pour une meilleure précision, et basculera automatiquement sur SoundTouch si Librosa n'est pas disponible.

#### Librosa uniquement (plus précis)

**Avantages :**
- Analyse BPM la plus précise disponible
- Utilise des algorithmes de traitement du signal avancés
- Meilleure détection pour les morceaux avec tempo variable

**Inconvénients :**
- Nécessite Python Embedded (installation automatique au premier usage)
- Analyse plus lente que SoundTouch
- Ne fonctionnera pas si Python n'est pas disponible

**Utilisation :**
Choisissez cette option si vous recherchez la précision maximale et que vous acceptez un temps de calcul légèrement plus long. Si Python n'est pas installé, l'application vous proposera de l'installer automatiquement.

**Note importante :** Si Python n'est pas disponible et que vous forcez Librosa, le calcul du BPM échouera (retournera 0).

#### SoundTouch uniquement (moins précis)

**Avantages :**
- Aucune dépendance externe requise
- Analyse très rapide
- Fonctionne toujours, même sans Python

**Inconvénients :**
- Moins précis que Librosa
- Peut donner des résultats moins fiables pour certains types de musique

**Utilisation :**
Choisissez cette option si :
- Vous ne pouvez pas ou ne voulez pas installer Python
- Vous privilégiez la vitesse à la précision
- Vous travaillez avec des morceaux au tempo simple et constant

## Comparaison des méthodes

| Critère | Auto | Librosa uniquement | SoundTouch uniquement |
|---------|------|-------------------|---------------------|
| Précision | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| Vitesse | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Fiabilité | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Dépendances | Python (optionnel) | Python (requis) | Aucune |

## Installation de Python (pour Librosa)

Si vous choisissez **Auto** ou **Librosa uniquement** et que Python n'est pas encore installé :

1. Au premier calcul de BPM, l'application détectera l'absence de Python
2. Une boîte de dialogue vous proposera d'installer Python Embedded automatiquement
3. Cliquez sur **Oui** pour lancer l'installation
4. L'application téléchargera Python Embedded 3.11.9 et installera les bibliothèques nécessaires (librosa, numpy, scipy)
5. L'installation est portable et n'affecte pas votre système Windows

**Taille du téléchargement :** Environ 25-30 MB pour Python Embedded + bibliothèques

## Persistance des paramètres

Votre choix de méthode BPM est sauvegardé dans le fichier de configuration de l'application et sera automatiquement restauré au prochain démarrage.

## Recommandations

- **Pour la plupart des utilisateurs :** Utilisez **Auto**
- **Pour une précision maximale :** Utilisez **Librosa uniquement**
- **Pour une utilisation sans Python :** Utilisez **SoundTouch uniquement**
- **Pour l'analyse en lot :** **Auto** ou **Librosa uniquement** offriront de meilleurs résultats

## Notes techniques

- Les deux méthodes analysent maintenant **le fichier audio complet** (pas seulement un échantillon)
- Les valeurs BPM calculées sont automatiquement sauvegardées dans les métadonnées du fichier audio
- Lors de l'ajout de fichiers à la playlist, les valeurs BPM existantes dans les métadonnées sont automatiquement chargées
- Le changement de méthode ne recalcule pas automatiquement les BPM existants ; utilisez le menu contextuel pour recalculer si nécessaire

## Dépannage

**Problème :** Le BPM retourne 0 avec "Librosa uniquement"
**Solution :** Python n'est pas installé. Choisissez "Auto" pour permettre l'installation automatique, ou basculez sur "SoundTouch uniquement".

**Problème :** L'analyse Librosa semble lente
**Solution :** C'est normal, Librosa est plus précis mais plus lent. Si la vitesse est importante, utilisez "SoundTouch uniquement".

**Problème :** Les résultats SoundTouch semblent incorrects
**Solution :** Pour certains types de musique (EDM, jazz, musique classique), Librosa donnera de meilleurs résultats. Installez Python et utilisez "Auto" ou "Librosa uniquement".
