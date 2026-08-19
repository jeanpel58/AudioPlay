# Contrôle du Volume Système Windows

## Modifications apportées

Le contrôle de volume d'AudioPlay a été modifié pour contrôler directement le **volume système de Windows** au lieu d'un volume isolé de l'application.

### Fichiers modifiés

1. **WindowsVolumeControl.vb** (nouveau fichier)
   - Classe utilitaire pour interagir avec l'API Windows Core Audio
   - Méthodes disponibles :
	 - `SetVolume(volume As Single)` : Définit le volume système (0.0 à 1.0)
	 - `GetVolume() As Single` : Obtient le volume système actuel
	 - `SetMute(mute As Boolean)` : Active/désactive le mute système
	 - `IsMuted() As Boolean` : Vérifie si le système est en mute

2. **Form1.vb**
   - `TrackBar_Volume_Scroll()` : Modifié pour appliquer le volume au système Windows via `WindowsVolumeControl.SetVolume()`
   - `Button_Mute_Click()` : Modifié pour activer/désactiver le mute système via `WindowsVolumeControl.SetMute()`
   - `ChargerAudioAjustements()` : Modifié pour charger le volume depuis le système Windows au démarrage via `WindowsVolumeControl.GetVolume()`

### Comportement

#### Avant
- Le volume d'AudioPlay était indépendant du volume Windows
- Chaque application avait son propre niveau de volume
- Le mixeur de volume Windows affichait un niveau différent d'AudioPlay

#### Après
- Le TrackBar de volume d'AudioPlay contrôle directement le volume maître de Windows
- Modifier le volume dans AudioPlay modifie le volume système
- Modifier le volume système (touches clavier, mixeur Windows) se reflète dans AudioPlay
- Le bouton Mute d'AudioPlay active/désactive le mute système

### Avantages
- ✅ Contrôle unifié du volume
- ✅ Synchronisation automatique avec le volume système
- ✅ Compatibilité avec les touches multimédias du clavier
- ✅ Pas de confusion entre deux niveaux de volume différents

### Notes techniques
- Utilise l'API **Windows Core Audio** (IAudioEndpointVolume)
- Interface COM native pour un contrôle précis du volume
- Compatible Windows Vista et supérieur
- Gestion d'erreur robuste en cas d'échec API

### Compatibilité
- Le `volumeProvider` de NAudio continue de fonctionner pour les effets audio qui en dépendent (normalisation, etc.)
- Les paramètres Basses et Aigues restent spécifiques à AudioPlay
- Le fichier `Son_Ajustement.txt` continue d'enregistrer les préférences (Basses, Aigues)
