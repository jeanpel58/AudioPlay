# Fonctionnalité Loop (Boucle I-O)

## Vue d'ensemble
La fonctionnalité Loop permet de définir une section spécifique d'une chanson et de la jouer en boucle indéfiniment.

## Comment utiliser

### 1. Définir une boucle
Pendant qu'une chanson est en cours de lecture :
- Appuyez sur la touche **I** au moment où vous voulez que la boucle commence
- Appuyez sur la touche **O** au moment où vous voulez que la boucle se termine

### 2. Indicateurs visuels
Une fois les deux marqueurs définis :
- Un petit **I** apparaît au-dessus du TrackBar d'avancement pour indiquer le début de la boucle
- Un petit **O** apparaît au-dessus du TrackBar d'avancement pour indiquer la fin de la boucle
- **Positionnement précis** : Les marqueurs sont alignés avec une grande précision sur la position exacte du curseur du TrackBar
- **Couleur automatique** : Les marqueurs s'affichent en **rouge** sur fond clair, et en **jaune** sur fond rouge ou sombre (adaptation automatique selon le thème)

### 3. Activer la boucle
- Cliquez sur le bouton **Loop** dans le GroupBox4
- Le bouton devient vert clair pour indiquer que la boucle est active
- La lecture revient automatiquement au début de la boucle (marqueur I) quand la fin (marqueur O) est atteinte

### 4. Désactiver la boucle
- Cliquez à nouveau sur le bouton **Loop**
- Le bouton revient à sa couleur normale
- La lecture continue normalement sans boucler

### 5. Pause et reprise
- Si vous mettez la lecture en pause (bouton Pause/Reprise ou barre Espace), la boucle se met également en pause
- Quand vous reprenez la lecture, la boucle reprend automatiquement son fonctionnement
- Le bouton Loop reste vert pendant la pause pour indiquer que la boucle est toujours active

### 6. Mode Muet (Mute)
- Si vous activez le mode muet (bouton Mute ou Ctrl+S), la lecture et la boucle continuent normalement
- Seul le son est coupé, la position continue d'avancer et la boucle reste active
- Quand vous désactivez le mode muet, le son reprend immédiatement

### 7. Réinitialisation
- Les marqueurs I et O sont automatiquement effacés lorsque vous chargez une nouvelle chanson
- Les marqueurs sont également effacés si vous arrêtez complètement la lecture (bouton Arrêter ou Ctrl+Espace)
- La boucle est également désactivée automatiquement dans ces cas

## Différences importantes

| Action | Effet sur la boucle | Effet sur les marqueurs I/O |
|--------|---------------------|------------------------------|
| **Pause/Reprise** (Espace, Ctrl+P) | ⏸️ Pause et reprend avec la lecture | ✅ Conservés |
| **Mute** (Ctrl+S) | ✅ Continue normalement (sans son) | ✅ Conservés |
| **Arrêter** (Ctrl+Espace) | ❌ Désactivée | ❌ Effacés |
| **Nouvelle chanson** | ❌ Désactivée | ❌ Effacés |
| **Clic Button_Loop** | ✅/❌ Active/Désactive | ✅ Conservés |
| **Button_CalculBPM** | 🚫 **Bloqué** (message affiché) | ✅ Conservés |

## Restrictions

### Calcul BPM bloqué pendant une boucle
- Si vous tentez de cliquer sur le bouton **CalculBPM** pendant qu'une boucle est active, un message vous informera que le calcul n'est pas disponible
- Vous devez d'abord désactiver la boucle (cliquer sur le bouton Loop) avant de pouvoir calculer le BPM
- Cette restriction évite les conflits entre le calcul BPM et la lecture en boucle

## Raccourcis clavier
- **I** : Marquer le début de la boucle (doit être appuyé en premier)
- **O** : Marquer la fin de la boucle (doit être appuyé après I)

## Notes techniques
- Les marqueurs ne peuvent être définis que pendant la lecture (pas en pause)
- Le marqueur O doit être défini après le marqueur I (chronologiquement dans la chanson)
- La position exacte est capturée au moment où vous appuyez sur la touche
- La boucle fonctionne en vérifiant la position actuelle toutes les 200ms (intervalle du timer)
- **Adaptation automatique de la couleur** : Les marqueurs I et O changent de couleur automatiquement selon le thème
  - **Rouge** : Si le fond du formulaire est clair
  - **Jaune** : Si le fond du formulaire est rouge ou très sombre (pour assurer une bonne visibilité)
  - La couleur est recalculée automatiquement quand vous changez de thème dans les paramètres

## Variables utilisées
- `loopEnabled` : Indique si la boucle est activée
- `loopStartPosition` : Position de début de la boucle (TimeSpan)
- `loopEndPosition` : Position de fin de la boucle (TimeSpan)
- `hasLoopMarkers` : Indique si les deux marqueurs I et O ont été définis
- `labelLoopStart` : Label "I" affiché au-dessus du TrackBar
- `labelLoopEnd` : Label "O" affiché au-dessus du TrackBar
