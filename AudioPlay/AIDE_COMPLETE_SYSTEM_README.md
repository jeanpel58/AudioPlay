# AudioPlay - Système d'Aide Complet Multilingue

## 📋 Vue d'ensemble

Un système d'aide HTML complet a été créé pour AudioPlay en 5 langues (Français, English, Español, Deutsch, Italiano).

## ✅ Fichiers créés

### 1. Guides HTML complets

| Langue | Fichier | Statut |
|--------|---------|--------|
| 🇫🇷 Français | `AUDIOPLAY_GUIDE_COMPLET.fr.html` | ✅ Créé |
| 🇬🇧 English | `AUDIOPLAY_GUIDE_COMPLET.en.html` | ✅ Créé |
| 🇪🇸 Español | `AUDIOPLAY_GUIDE_COMPLET.es.html` | ⏳ À créer |
| 🇩🇪 Deutsch | `AUDIOPLAY_GUIDE_COMPLET.de.html` | ⏳ À créer |
| 🇮🇹 Italiano | `AUDIOPLAY_GUIDE_COMPLET.it.html` | ⏳ À créer |

### 2. Intégration au code

**Fichier modifié** : `AudioPlay/Form1.vb`

**Handler ajouté** :
```vb
Private Sub Button_AudioPlay_Aide_Click(sender As Object, e As EventArgs) Handles Button_AudioPlay_Aide.Click
	' Ouvre le guide complet dans la langue de l'utilisateur
	' Fichier : AUDIOPLAY_GUIDE_COMPLET.[fr|en|es|de|it].html
End Sub
```

## 📖 Contenu du guide

### Section 1 : ⌨️ Raccourcis Clavier

#### 🎮 Contrôles de Lecture
- **Espace** : Lecture / Pause
- **Ctrl+Espace** : Arrêter TOUT (lecture, BPM, karaoke)
- **Ctrl+P** : Pause / Reprise
- **Ctrl+S** : Sourdine (Mute)
- **Ctrl+A** : Mode Aléatoire

#### 🔄 Boucle (Loop)
- **I** : Marquer début de boucle
- **O** : Marquer fin de boucle

#### 📋 Navigation Playlist
- **↑** / **↓** : Sélection précédente/suivante
- **Ctrl+↑** / **Ctrl+↓** : Déplacer chanson haut/bas
- **Home** / **End** : Première/dernière chanson
- **Suppr** : Supprimer de la playlist

### Section 2 : 🎛️ Boutons de Contrôle

#### 🎵 Lecture Audio
- **▶️ Jouer** : Démarre la lecture
- **⏸️ Pause / Reprise** : Pause ou reprend la lecture
- **⏹️ Arrêter** : Arrêt complet (position à 0:00)
- **⏮️ Précédent** : Chanson précédente
- **⏭️ Suivant** : Chanson suivante
- **🔇 Mute** : Active/désactive la sourdine

#### 📋 Gestion Playlist
- **➕ Ajout** : Ajouter des fichiers audio
- **📝 Métadonnées** : Éditer les tags ID3
- **📚 Playlist** : Menu gestion (Nouvelle, Ouvrir, Sauvegarder, Vider)

#### 🎚️ Contrôles Audio
- **🔊 Volume** : Curseur 0-100%
- **🎵 Aigues** : Égaliseur hautes fréquences (-15 à +15 dB)
- **🔉 Basses** : Égaliseur basses fréquences (-15 à +15 dB)

#### 🎯 Fonctionnalités Avancées
- **🎼 BPM** : Calcul automatique des battements par minute
- **🔄 Loop** : Répétition d'une section (marqueurs I-O)
- **🎲 Aléatoire** : Lecture en ordre aléatoire
- **⚙️ Paramètres** : Ouvre la fenêtre des paramètres
- **ℹ️ À Propos** : Informations application
- **🔌 Power** : Fermer l'application

### Section 3 : ⚙️ Paramètres de l'Application

#### 🌍 Langue
- Sélection parmi 5 langues
- Redémarrage requis pour application

#### 📁 Répertoire par Défaut
- Dossier de musique par défaut
- Bouton Parcourir pour sélection

#### 🎵 Lecture
- ☑️ Lecture en continu
- ☑️ Afficher le BPM
- ☑️ Normalisation du volume
- ☑️ Supprimer silence début
- ☑️ Supprimer silence fin
- ☑️ Confirmer suppression chansons

#### 🎼 Calcul BPM
- Méthode Auto
- Méthode Librosa (Python)
- Méthode SoundTouch

#### 🥁 Métronome
- ☑️ Activer le métronome
- ☑️ Son du métronome
- ☑️ Lumière du métronome
- Nombre de battements par mesure (1-16)

#### 🎨 Thèmes
Liste complète des 15 éléments personnalisables :
1. Fond formulaire
2. Fond contrôles
3. Texte contrôles
4. Fond boutons
5. Texte boutons
6. Fond ListView
7. Texte ListView
8. Fond en-têtes ListView
9. Texte en-têtes ListView
10. Fond sélection ListView
11. Texte sélection ListView
12. Fond TextBox
13. Texte TextBox
14. Texte GroupBox
15. Bordure GroupBox
16. Fond TrackBar

Thèmes préinstallés :
- Par défaut (bleu clair)
- Sombre (noir)
- Océan (bleu/cyan)
- Automne (orange/marron)
- Soleil (jaune/doré)

#### 🔗 Associations de Fichiers
Formats supportés :
- .mp3 (MPEG Audio)
- .wav (Wave Audio)
- .flac (Free Lossless Audio Codec)
- .ogg (Ogg Vorbis)
- .m4a (MPEG-4 Audio)
- .wma (Windows Media Audio)
- .aac (Advanced Audio Coding)

### Section 4 : 📝 Éditeur de Métadonnées

#### 📋 Informations Fichier (Lecture seule)
- Nom du fichier
- Chemin complet
- Taille
- Date de modification

#### 🎵 Informations Audio (Lecture seule)
- Format audio
- Durée
- Fréquence d'échantillonnage
- Bitrate
- Bits par échantillon
- Canaux
- Octets par seconde

#### 🏷️ Tags ID3 / Métadonnées (Éditables)
- Titre
- Artiste
- Artiste Album
- Album
- Année
- Genre
- Piste
- Compositeur
- BPM
- Commentaire

#### 💾 Sauvegarde
- Écriture permanente dans le fichier
- Compatible tous lecteurs audio
- Gestion des fichiers en cours de lecture

### Section 5 : ✨ Fonctionnalités Avancées

#### 🎤 Karaoke CDG
- Support natif CD+G
- Affichage paroles synchronisées
- Graphismes et animations CDG

#### 🔄 Normalisation de Volume
- Calcul niveau RMS
- Détection des pics audio
- Gain correctif automatique
- Pas de modification permanente

#### 🎵 Métronome Synchronisé
- Son audible (tick-tock)
- Indicateur visuel LED
- Mesures configurables (1-16 temps)
- Accentuation premier temps

#### ✂️ Suppression des Silences
- Détection début de piste
- Détection fin de piste
- Seuil réglable

#### 🎲 Lecture Aléatoire Intelligente
- Mélange sans répétitions
- Compatible lecture continue
- État sauvegardé

#### 🖱️ Glisser-Déposer
- Fichiers individuels
- Dossiers entiers (récursif)
- Sélections multiples
- Playlists .m3u

#### 💾 Gestion des Playlists
- Format M3U standard
- Chemins relatifs/absolus
- Métadonnées préservées

#### 🌐 Multilingue
- Interface en 5 langues
- Tous messages localisés
- Documentation HTML multilingue

#### ⚡ Performance
- Chargement asynchrone
- Streaming audio efficace
- Libération auto ressources
- Cache métadonnées
- Multi-thread BPM

#### 🔒 Sécurité
- Pas de modifications non sollicitées
- Confirmation sauvegarde métadonnées
- Détection fichiers en cours d'utilisation
- Gestion propre des erreurs

### Section 6 : 💡 Astuces et Conseils

#### 🎯 Utilisation Optimale
- Raccourcis essentiels à mémoriser
- Calcul BPM rapide pour toute la playlist
- Création de thèmes personnalisés
- Organisation des playlists

#### ⚠️ Points d'Attention
- Modification métadonnées permanente
- BPM et Boucle incompatibles
- Fichiers protégés

#### 🚀 Raccourcis Avancés
- Menu contextuel (clic droit)
- Double-clic lecture immédiate
- Sélection multiple (Ctrl/Maj)

### Section 7 : 🔧 Dépannage

#### ❓ Problèmes Courants
- 🔇 Pas de son
- ❌ Fichier ne s'ouvre pas
- 🐌 Calcul BPM lent
- 🎨 Thème ne s'applique pas
- 🌍 Changement de langue ne fonctionne pas

#### 📞 Support
- Logiciel libre et open-source
- Documentation disponible

## 🎨 Style et Design

### Caractéristiques visuelles
- Design moderne avec dégradés purple/violet
- Navigation sticky avec liens rapides
- Tableaux stylisés avec ombres
- Badges colorés pour les touches clavier
- Blocs informatifs (tip, warning, success)
- Responsive design
- Print-friendly

### Couleurs principales
- Primaire : `#667eea` (bleu-violet)
- Secondaire : `#764ba2` (violet)
- Succès : `#28a745` (vert)
- Avertissement : `#ffc107` (jaune)
- Erreur : `#dc3545` (rouge)

## 🔧 Utilisation

### Pour l'utilisateur
1. Cliquez sur le bouton **?** (Button_AudioPlay_Aide) dans la Form1
2. Le guide complet s'ouvre dans le navigateur par défaut
3. La langue du guide correspond automatiquement à la langue de l'application

### Pour le développeur
```vb
' Handler automatique basé sur la langue
Private Sub Button_AudioPlay_Aide_Click(sender As Object, e As EventArgs)
	' Détecte la langue actuelle (fr, en, es, de, it)
	' Ouvre le fichier AUDIOPLAY_GUIDE_COMPLET.[langue].html
	' Affiche un message d'erreur si le fichier est manquant
End Sub
```

## 📦 Déploiement

### Fichiers à inclure dans la distribution
```
AudioPlay.exe
AUDIOPLAY_GUIDE_COMPLET.fr.html
AUDIOPLAY_GUIDE_COMPLET.en.html
AUDIOPLAY_GUIDE_COMPLET.es.html (à créer)
AUDIOPLAY_GUIDE_COMPLET.de.html (à créer)
AUDIOPLAY_GUIDE_COMPLET.it.html (à créer)
```

### Structure recommandée
```
AudioPlay/
├── AudioPlay.exe
├── NAudio.dll
├── ... (autres DLLs)
├── AUDIOPLAY_GUIDE_COMPLET.fr.html
├── AUDIOPLAY_GUIDE_COMPLET.en.html
├── AUDIOPLAY_GUIDE_COMPLET.es.html
├── AUDIOPLAY_GUIDE_COMPLET.de.html
├── AUDIOPLAY_GUIDE_COMPLET.it.html
├── LOOP_GUIDE_USER.fr.html
├── LOOP_GUIDE_USER.en.html
├── ... (autres fichiers d'aide)
└── Themes/
```

## ⏭️ Prochaines étapes

### À créer
- [ ] `AUDIOPLAY_GUIDE_COMPLET.es.html` (Version espagnole)
- [ ] `AUDIOPLAY_GUIDE_COMPLET.de.html` (Version allemande)
- [ ] `AUDIOPLAY_GUIDE_COMPLET.it.html` (Version italienne)

### Méthode suggérée
1. Dupliquer le fichier `.en.html`
2. Traduire tous les textes
3. Vérifier les liens et la navigation
4. Tester l'ouverture depuis l'application

## 🎓 Référence technique

### Navigation HTML
- `#raccourcis` : Section raccourcis clavier
- `#boutons` : Section boutons de contrôle
- `#parametres` : Section paramètres
- `#metadonnees` : Section métadonnées
- `#fonctionnalites` : Section fonctionnalités avancées

### Clés de ressources requises
- `Help_FilesNotFound` : Message fichier d'aide manquant
- `Help_ExpectedFiles` : Message fichiers attendus
- `Help_ErrorOpenFile` : Erreur ouverture fichier
- `Help_Title` : Titre fenêtre aide
- `Error_Title` : Titre erreur générique

## 📝 Notes de version

**Version** : 1.26.05.31  
**Date** : 2024-12-XX  
**Auteur** : Jean  
**Statut** : ✅ FR et EN complets, ES/DE/IT à créer

---

**© 2024 AudioPlay - Lecteur Audio Multilingue avec Analyse BPM et Karaoke**
