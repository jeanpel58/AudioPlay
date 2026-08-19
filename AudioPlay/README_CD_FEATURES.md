# AudioPlay - Lecteur CD Audio avec métadonnées intelligentes

## 🎵 Fonctionnalités principales

### Lecture de CD Audio
- ✅ Détection automatique des lecteurs CD/DVD/Blu-ray
- ✅ Affichage dynamique dans le menu (lecteurs vides en gris)
- ✅ Lecture native via API Windows (pas de pilotes tiers requis)
- ✅ Sélection individuelle des pistes avec checkboxes
- ✅ Support multi-lecteurs (DVD + Blu-ray simultanés)

### Métadonnées CD enrichies
- 🌐 **MusicBrainz** : Lookup automatique par DiscID
- 🌐 **GnuDB (CDDB)** : Base de données CDDB (comme Exact Audio Copy)
- 🌐 **Discogs** : Recherche manuelle (artiste + album)
- ✏️ **Saisie manuelle** : Formulaire complet pour métadonnées personnalisées
- 💾 **Cache local automatique** : Les métadonnées sont sauvegardées et rechargées automatiquement

### Gestion de playlist
- 📝 Sauvegarde automatique dans `%APPDATA%\AudioPlay\playlist.txt`
- 🔒 Backup automatique (`playlist.txt.bak`) pour prévenir les pertes
- 🎵 Support MP3, FLAC, WAV, OGG, et autres formats audio
- ⚠️ Les pistes CD ne sont **pas** sauvegardées (rechargement manuel requis)

---

## 📁 Fichiers de données

### Playlist
- **Fichier principal** : `%APPDATA%\AudioPlay\playlist.txt`
- **Backup** : `%APPDATA%\AudioPlay\playlist.txt.bak`
- **Format** : `chemin|nom|bpm|duree` (un fichier par ligne)
- **Exclusions** : Les entrées `CDDA://` ne sont pas sauvegardées

### Cache de métadonnées CD
- **Fichier** : `%APPDATA%\AudioPlay\cd_metadata_cache.json`
- **Format** : JSON structuré
- **Indexation** : Par DiscID (SHA-1 des offsets TOC du CD)
- **Contenu** :
  - Artiste, Album, Année
  - Titres des pistes
  - Artiste par piste (si différent)
  - Source (MusicBrainz / Discogs / Manual)
  - Date d'ajout

---

## 🚀 Workflow d'utilisation

### Ajouter un CD audio à la playlist

1. **Insérer le CD** dans le lecteur
2. **Menu Ajout** → Sélectionner le lecteur CD
3. **Formulaire de sélection** s'ouvre :
   - Si métadonnées en cache : **affichage automatique** (label bleu)
   - Sinon : pistes affichées comme "Piste 01", "Piste 02", etc.

4. **Charger les métadonnées (si nécessaire)** :
   - Choisir une source dans le ComboBox :
	 - `MusicBrainz` → Clic "Charger" (automatique)
	 - `Discogs` → Clic "Charger" → Saisir artiste + album
	 - `Saisie manuelle` → Remplir le formulaire complet
   - Les métadonnées sont **automatiquement sauvegardées** dans le cache

5. **Cocher les pistes** à ajouter (ou utiliser "Tout sélectionner")
6. **Cliquer OK** → Les pistes sont ajoutées à la ListView avec leurs titres

### Mettre à jour des métadonnées erronées

1. **Ouvrir le sélecteur** de pistes CD
2. **Cliquer "Effacer cache"** (confirmer)
3. **Choisir une nouvelle source** et recharger
4. Les nouvelles métadonnées écrasent les anciennes dans le cache

---

## ⚙️ Configuration

### Clé API Discogs (optionnelle)

Pour utiliser Discogs, vous devez obtenir une clé API gratuite :

1. Créer un compte sur [discogs.com](https://www.discogs.com)
2. Aller dans **Settings** → **Developers**
3. Créer une **nouvelle application**
4. Copier **Consumer Key** et **Consumer Secret**
5. Modifier `AudioPlay\DiscogsMetadataProvider.vb` :

```vb
Private Const DISCOGS_API_KEY As String = "VOTRE_CLE_ICI"
Private Const DISCOGS_API_SECRET As String = "VOTRE_SECRET_ICI"
```

6. Recompiler le projet

---

## 🗂️ Architecture du code

### Modules principaux

| Fichier | Rôle |
|---------|------|
| `Form1.vb` | Interface principale, gestion playlist, orchestration |
| `CDAudioManager.vb` | Détection lecteurs, lecture TOC, streaming CD audio |
| `CDMetadataProvider.vb` | MusicBrainz, calcul DiscID |
| `GnuDBMetadataProvider.vb` | GnuDB (protocole CDDB) |
| `DiscogsMetadataProvider.vb` | Recherche Discogs API |
| `CDMetadataCache.vb` | Cache local JSON (lecture/écriture) |
| `FormSelecteurPistesCD.vb` | UI de sélection des pistes + métadonnées |

### Classes de données

```vb
' CD Track (CDAudioManager)
Public Class CDTrack
	Public Property TrackNumber As Integer
	Public Property Duration As TimeSpan
	Public Property DriveLetter As String
	Public Property StartFrame As Integer
	Public Property EndFrame As Integer
	Public Property Title As String
End Class

' Metadata (CDMetadataProvider)
Public Class CDInfo
	Public Property Artist As String
	Public Property Album As String
	Public Property Year As Integer
	Public Property Tracks As List(Of TrackInfo)
End Class

Public Class TrackInfo
	Public Property TrackNumber As Integer
	Public Property Title As String
	Public Property Artist As String
	Public Property Duration As TimeSpan
End Class
```

---

## 🔧 Dépannage

### Le CD n'est pas détecté
- Vérifier que le CD est bien inséré
- Attendre quelques secondes pour la reconnaissance
- Fermer/rouvrir le menu "Ajout"

### MusicBrainz retourne "404 NotFound"
- Le CD n'est pas dans leur base de données
- **Solution** : Utiliser Discogs ou saisie manuelle

### Les métadonnées ne se chargent pas depuis le cache
- Vérifier que le fichier existe : `%APPDATA%\AudioPlay\cd_metadata_cache.json`
- Le DiscID doit correspondre exactement (même CD, même pressage)
- En cas de doute : Effacer le cache et recharger

### Discogs ne trouve rien
- Vérifier l'orthographe de l'artiste et de l'album
- Essayer avec moins de mots (ex: "Beatles" au lieu de "The Beatles")
- Vérifier que la clé API est configurée

---

## 📚 Documentation complémentaire

- **Guide des sources de métadonnées** : `METADATA_SOURCES.md`
- **Instructions de compilation** : `.github\copilot-instructions.md`

---

## 🎯 Améliorations futures

- [ ] Synchronisation cloud du cache (Google Drive, OneDrive)
- [ ] Téléchargement automatique des pochettes d'album
- [ ] Édition inline des métadonnées
- [ ] Import/export du cache en CSV
- [ ] Support CDDB/Gracenote
- [ ] Recherche fuzzy pour Discogs
- [ ] Détection automatique de la langue de l'utilisateur

---

## 📄 Licence

AudioPlay - Projet open source
Repository : https://github.com/jeanpel58/AudioPlay
